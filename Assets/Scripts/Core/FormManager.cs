
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

public class FormManager : MonoBehaviour, IFormQueueSource
{
    [Header("References")]
    public GameStateManager gameStateManager;
    public FormFactory formFactory;
    [SerializeField] private InventoryCatalog inventoryCatalog;
    [SerializeField] private AlphaScoreService alphaScoreService;
    [SerializeField] private FormWorkspaceTable workspaceTable;
    [FormerlySerializedAs("formWindowMenu")]
    [SerializeField] private MonoBehaviour formDockBehaviour;
    [FormerlySerializedAs("useFormWindowMenu")]
    [SerializeField] private bool useFormDock = true;
    [SerializeField] private FormSpawnSource formSpawnSource;
    [SerializeField] private bool useFormSpawnSource;

    [Header("Form Groups")]
    public List<FormGroup> formGroups = new List<FormGroup>();
    public bool listenForFlagChanges = true;
    // public bool createFormsOnUnlock = true;

    [Header("Submission")]
    [SerializeField] private SubmitZone submitZone;

    [Header("Timing")]
    [SerializeField] private bool enableTimedArrivals = true;
    [SerializeField] private float idleCheckSeconds = 1f;

    [Header("Alpha Fail Condition")]
    [SerializeField] private bool enableLoseOnFailureCount = true;
    [SerializeField] private int maxFailedForms = 5;
    [SerializeField] private string loseSceneName = "LoseScene";
    [SerializeField] private bool enableSubmissionDebugLogs = true;

    // Form data
    private List<FormDefinition> incomingForms = new List<FormDefinition>();
    private List<FormDefinition> submittedForms = new List<FormDefinition>();
    // TODO: add failed forms
    private HashSet<FormGroup> unlockedGroups = new HashSet<FormGroup>();
    private Dictionary<FormDefinition, Form> activeForms = new Dictionary<FormDefinition, Form>();
    private Dictionary<Form, FormDefinition> activeFormDefs = new Dictionary<Form, FormDefinition>();
    private Dictionary<FormGroup, HashSet<FormDefinition>> groupRemainingForms = new Dictionary<FormGroup, HashSet<FormDefinition>>();
    private Dictionary<FormDefinition, FormGroup> groupByForm = new Dictionary<FormDefinition, FormGroup>();
    private Queue<FormDefinition> arrivalQueue = new Queue<FormDefinition>();
    private HashSet<FormDefinition> arrivalSet = new HashSet<FormDefinition>();
    private readonly List<Form> inboxForms = new List<Form>();
    private Coroutine arrivalRoutine;
    private bool hasBootstrappedSpawnSource;
    private bool loseSceneTriggered;
    private IFormDock formDock;

    // Actions
    public event Action<FormGroup> FormGroupUnlocked;
    public event Action<FormDefinition, Form> FormCreated;
    public event Action<FormDefinition> FormSubmitted;
    public event Action<FormDefinition> FormSubmissionFailed;
    public event Action<FormDefinition> FormDiscarded;
    public event Action LoseTriggered;
    public event Action<int> PendingSpawnQueueChanged;


    public List<FormDefinition> IncomingForms => incomingForms;
    public IReadOnlyList<FormDefinition> SubmittedForms => submittedForms;
    public int PendingSpawnCount => inboxForms.Count;

    // do we need this?
    public IReadOnlyList<WordBlock> OwnedWordBlocks => gameStateManager != null
        ? gameStateManager.OwnedWordBlocks
        : Array.Empty<WordBlock>();

    private void Awake()
    {
        if (gameStateManager == null)
            gameStateManager = GameStateManager.Instance;
        if (inventoryCatalog == null)
            inventoryCatalog = FindFirstObjectByType<InventoryCatalog>();
        if (alphaScoreService == null)
            alphaScoreService = FindFirstObjectByType<AlphaScoreService>();
        if (workspaceTable == null)
            workspaceTable = FindFirstObjectByType<FormWorkspaceTable>();
        ResolveFormDock();
    }

    private void OnEnable()
    {
        if (!useFormSpawnSource && listenForFlagChanges && gameStateManager != null)
            gameStateManager.FlagStateChanged += OnFlagStateChanged;

        if (submitZone != null)
            submitZone.SubmitRequested += OnSubmitRequested;

        if (enableTimedArrivals)
            arrivalRoutine = StartCoroutine(FormArrivalLoop());

        if (useFormSpawnSource)
            BootstrapSpawnSourceIfNeeded();
        else
            UpdateFormAvailability();
    }

    private void OnDisable()
    {
        if (!useFormSpawnSource && listenForFlagChanges && gameStateManager != null)
            gameStateManager.FlagStateChanged -= OnFlagStateChanged;

        if (submitZone != null)
            submitZone.SubmitRequested -= OnSubmitRequested;

        if (arrivalRoutine != null)
        {
            StopCoroutine(arrivalRoutine);
            arrivalRoutine = null;
        }

        hasBootstrappedSpawnSource = false;
    }

    private void OnFlagStateChanged(GameFlags flag, bool state)
    {
        UpdateFormAvailability();
    }

    public void UpdateFormAvailability()
    {
        if (useFormSpawnSource)
            return;

        if (gameStateManager == null)
        {
            Debug.LogWarning("FormManager: GameStateManager reference is missing.");
            return;
        }

        foreach (var group in formGroups)
        {
            if (group == null) continue;

            bool unlocked = group.IsUnlocked(gameStateManager);
            if (unlocked)
            {
                EnsureGroupUnlocked(group);
            }
            else
            {
                DisableGroup(group);
            }
        }
    }

    private void EnsureGroupUnlocked(FormGroup group)
    {
        if (group == null) return;

        bool firstUnlock = unlockedGroups.Add(group);
        if (firstUnlock)
            FormGroupUnlocked?.Invoke(group);

        if (!firstUnlock) return;

        if (!groupRemainingForms.ContainsKey(group))
            groupRemainingForms[group] = new HashSet<FormDefinition>();

        RegisterGroupForms(group, group.initialForms, spawnImmediately: true);
        RegisterGroupForms(group, group.forms, spawnImmediately: false);
    }

    private void DisableGroup(FormGroup group)
    {
        if (group == null) return;

        RemoveGroupForms(group.initialForms);
        RemoveGroupForms(group.forms);
        RemoveGroupForms(group.endingForms);
    }

    private void RegisterGroupForms(FormGroup group, List<FormDefinition> defs, bool spawnImmediately)
    {
        if (group == null || defs == null) return;

        foreach (var def in defs)
        {
            if (def == null) continue;

            groupByForm[def] = group;
            groupRemainingForms[group].Add(def);

            if (spawnImmediately)
            {
                AddIncomingForm(def);
                if (UseInboxFlow())
                    QueueIncomingForm(def);
                else
                    SpawnForm(def);
            }
            else
            {
                QueueIncomingForm(def);
            }
        }
    }

    private void RemoveGroupForms(List<FormDefinition> defs)
    {
        if (defs == null) return;

        foreach (var def in defs)
        {
            if (def == null) continue;

            if (!submittedForms.Contains(def))
                incomingForms.Remove(def);

            RemoveFromArrivalQueue(def);
            RemoveInboxForm(def);
        }
    }

    public void QueueIncomingForm(FormDefinition formDefinition)
    {
        if (!AddIncomingForm(formDefinition)) return;

        if (enableTimedArrivals)
            EnqueueForArrival(formDefinition);
        else if (UseInboxFlow())
            SpawnHiddenInboxForm(formDefinition);
        else
            SpawnForm(formDefinition);
    }

    private bool AddIncomingForm(FormDefinition formDefinition)
    {
        if (formDefinition == null) return false;
        if (submittedForms.Contains(formDefinition)) return false;
        if (!incomingForms.Contains(formDefinition))
            incomingForms.Add(formDefinition);
        return true;
    }

    public void SpawnForm(FormDefinition def)
    {
        if (activeForms.TryGetValue(def, out var existingForm))
        {
            if (!existingForm.gameObject.activeSelf)
                existingForm.gameObject.SetActive(true);
            return;
        }

        var formInstance = formFactory.Create(def);
        activeForms[def] = formInstance;
        activeFormDefs[formInstance] = def;
        formInstance.Expired += OnFormExpired;
        ApplyRewards(def.rewardsOnReceive, def, formInstance);
        bool parkedInWindow = useFormDock && formDock != null && formDock.RegisterSpawnedForm(formInstance);
        if (!parkedInWindow)
        {
            EnsureWorkspaceTracker(formInstance);
            formInstance.PlaySpawnAnimation();
        }
        FormCreated?.Invoke(def, formInstance);
        CheckLoseCondition();
    }

    public bool TryOpenNextIncomingForm()
    {
        var next = GetInboxFormClosestToExpiry();
        if (next == null)
            return false;

        RemoveInboxForm(next);
        next.SetInboxHidden(false);
        SpawnFormAtCenter(next.Definition);
        return true;
    }

    private void SpawnFormAtCenter(FormDefinition def)
    {
        if (activeForms.TryGetValue(def, out var existingForm))
        {
            if (!existingForm.gameObject.activeSelf)
                existingForm.gameObject.SetActive(true);
            existingForm.SetInboxHidden(false);
            var existingDrag = existingForm.GetComponent<DraggableUI>();
            if (existingDrag != null)
                existingDrag.enabled = true;
            if (workspaceTable != null)
                workspaceTable.PlaceAtCenter(existingForm);
            else if (formFactory != null)
                formFactory.PlaceAtCenter(existingForm);
            EnsureWorkspaceTracker(existingForm);
            existingForm.transform.localScale = Vector3.one;
            existingForm.PlaySpawnAnimation();
            return;
        }

        var formInstance = formFactory.Create(def);
        activeForms[def] = formInstance;
        activeFormDefs[formInstance] = def;
        formInstance.Expired += OnFormExpired;
        ApplyRewards(def.rewardsOnReceive, def, formInstance);

        var drag = formInstance.GetComponent<DraggableUI>();
        if (drag != null)
            drag.enabled = true;

        formInstance.SetInboxHidden(false);
        if (workspaceTable != null)
            workspaceTable.PlaceAtCenter(formInstance);
        else if (formFactory != null)
            formFactory.PlaceAtCenter(formInstance);
        EnsureWorkspaceTracker(formInstance);
        formInstance.transform.localScale = Vector3.one;
        formInstance.PlaySpawnAnimation();

        FormCreated?.Invoke(def, formInstance);
        CheckLoseCondition();
    }

    public bool SubmitForm(Form form)
    {
        if (form == null) return false;

        if (!activeFormDefs.TryGetValue(form, out var def))
        {
            def = form.Definition;
            if (def == null)
            {
                Debug.LogWarning("FormManager: Tried to submit a form with no definition.");
                return false;
            }
        }

        var validation = form.EvaluateValidation();
        bool isValidSubmission = validation.IsValid && !def.shouldBeDiscarded;
        if (enableSubmissionDebugLogs)
        {
            Debug.Log($"[FormManager] Submit: '{def.name}' => {(isValidSubmission ? "SUCCESS" : "FAIL")} " +
                      $"(validFields={validation.IsValid}, shouldBeDiscarded={def.shouldBeDiscarded}, " +
                      $"correct={validation.CorrectFields}, incorrectOrEmpty={validation.IncorrectOrEmptyFields})");
        }
        ResolveFormOutcome(def, form, isSuccess: isValidSubmission, wasDiscarded: false, validation);
        return isValidSubmission;
    }

    public bool DiscardForm(Form form)
    {
        if (form == null) return false;

        if (!activeFormDefs.TryGetValue(form, out var def))
        {
            def = form.Definition;
            if (def == null)
            {
                Debug.LogWarning("FormManager: Tried to discard a form with no definition.");
                return false;
            }
        }

        // Current design: discarding always counts as submission failure.
        var validation = form.EvaluateValidation();
        ResolveFormOutcome(def, form, isSuccess: false, wasDiscarded: true, validation);
        return true;
    }

    private void ResolveFormOutcome(FormDefinition def, Form form, bool isSuccess, bool wasDiscarded, FormValidationResult validation)
    {
        incomingForms.Remove(def);
        if (!submittedForms.Contains(def))
            submittedForms.Add(def);
        RemoveInboxForm(form);

        if (isSuccess)
        {
            if (gameStateManager != null)
                gameStateManager.RegisterSubmissionSuccess();
            ApplyRewards(def.rewardsOnSubmitSuccess, def, form);
            FormSubmitted?.Invoke(def);
        }
        else
        {
            if (gameStateManager != null)
                gameStateManager.RegisterSubmissionFailure();
            ApplyRewards(def.rewardsOnSubmitFailure, def, form);
            FormSubmissionFailed?.Invoke(def);
            if (wasDiscarded)
                FormDiscarded?.Invoke(def);
        }

        if (alphaScoreService != null)
        {
            float spawnPressure = gameStateManager != null ? gameStateManager.GetSpawnPressure01() : 0f;
            alphaScoreService.ApplySubmission(isSuccess, form, validation, spawnPressure);
        }

        CheckForGroupCompletion(def);
        formDock?.UnregisterForm(form);
        form.Expired -= OnFormExpired;
        activeFormDefs.Remove(form);
        if (def != null)
            activeForms.Remove(def);

        LogSubmissionTotals(def, isSuccess);
        CheckLoseCondition();

        if (form != null)
        {
            form.gameObject.SetActive(false);
            Destroy(form.gameObject);
        }
    }

    private void ApplyRewards(IReadOnlyList<FormReward> rewards, FormDefinition def, Form form)
    {
        if (rewards == null || rewards.Count == 0) return;

        if (gameStateManager == null)
            gameStateManager = GameStateManager.Instance;

        var context = new FormRewardContext(
            gameStateManager,
            this,
            inventoryCatalog,
            def,
            form);

        for (int i = 0; i < rewards.Count; i++)
        {
            var reward = rewards[i];
            if (reward == null) continue;

            try
            {
                reward.Apply(context);
            }
            catch (Exception ex)
            {
                Debug.LogError($"FormManager: Reward '{reward.name}' failed on form '{(def != null ? def.name : "null")}'. Exception: {ex}");
            }
        }
    }

    private void OnSubmitRequested(List<Form> forms)
    {
        if (forms == null || forms.Count == 0) return;

        foreach (var form in forms)
        {
            if (form == null) continue;
            if (SubmitForm(form))
                submitZone.Deregister(form);
        }
    }

    private void OnFormExpired(Form form)
    {
        if (form == null) return;

        if (!activeFormDefs.TryGetValue(form, out var def))
            def = form.Definition;

        if (def == null)
        {
            if (enableSubmissionDebugLogs)
                Debug.LogWarning("[FormManager] Timeout: form expired but has no FormDefinition.");
            form.Expired -= OnFormExpired;
            formDock?.UnregisterForm(form);
            activeFormDefs.Remove(form);
            RemoveInboxForm(form);
            Destroy(form.gameObject);
            return;
        }

        if (gameStateManager != null)
            gameStateManager.RegisterExpiredForm(def);

        var validation = form.EvaluateValidation();
        if (enableSubmissionDebugLogs)
        {
            Debug.Log($"[FormManager] Timeout: '{def.name}' => FAIL " +
                      $"(correct={validation.CorrectFields}, incorrectOrEmpty={validation.IncorrectOrEmptyFields})");
        }

        // Timeout now counts as failure and applies failure penalties/rewards.
        ResolveFormOutcome(def, form, isSuccess: false, wasDiscarded: false, validation);
    }

    private void LogSubmissionTotals(FormDefinition def, bool wasSuccess)
    {
        if (!enableSubmissionDebugLogs)
            return;

        int successCount = gameStateManager != null ? gameStateManager.SubmissionCount : 0;
        int failureCount = gameStateManager != null ? gameStateManager.SubmissionFailCount : 0;
        int unresolvedCount = activeFormDefs.Count;

        Debug.Log($"[FormManager] Totals after '{(def != null ? def.name : "UnknownForm")}' " +
                  $"({(wasSuccess ? "SUCCESS" : "FAIL")}): success={successCount}, fail={failureCount}, unresolved={unresolvedCount}");
    }

    private void CheckForGroupCompletion(FormDefinition def)
    {
        if (def == null) return;
        if (!groupByForm.TryGetValue(def, out var group)) return;
        if (!groupRemainingForms.TryGetValue(group, out var remaining)) return;

        remaining.Remove(def);

        if (remaining.Count == 0 && group.endingForms != null)
        {
            foreach (var endingDef in group.endingForms)
            {
                if (endingDef == null) continue;
                QueueIncomingForm(endingDef);
            }
        }
    }

    private void EnqueueForArrival(FormDefinition def)
    {
        if (def == null) return;
        if (activeForms.ContainsKey(def)) return;
        if (arrivalSet.Contains(def)) return;

        arrivalQueue.Enqueue(def);
        arrivalSet.Add(def);
    }

    private FormDefinition DequeueNextArrival()
    {
        while (arrivalQueue.Count > 0)
        {
            var next = arrivalQueue.Dequeue();
            arrivalSet.Remove(next);

            if (next == null) continue;
            if (submittedForms.Contains(next)) continue;
            if (activeForms.ContainsKey(next)) continue;
            if (!incomingForms.Contains(next)) continue;

            return next;
        }

        return null;
    }

    /// <summary>
    /// Spawns a new form - a new form arrives every `delay` amount calculated in GameStateManager
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator FormArrivalLoop()
    {
        while (true)
        {
            if (gameStateManager == null)
            {
                yield return null;
                continue;
            }

            var next = DequeueNextArrival();
            if (next == null)
            {
                yield return new WaitForSeconds(idleCheckSeconds);
                continue;
            }

            float delay = gameStateManager.GetCurrentFormArrivalDelay();
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (UseInboxFlow())
                SpawnHiddenInboxForm(next);
            else
                SpawnForm(next);
            if (useFormSpawnSource)
                QueueNextFromSpawnSource();
        }
    }

    private void BootstrapSpawnSourceIfNeeded()
    {
        if (hasBootstrappedSpawnSource || formSpawnSource == null)
            return;

        formSpawnSource.InitializeSource(this, gameStateManager);

        foreach (var def in formSpawnSource.GetInitialForms())
        {
            if (def == null) continue;
            AddIncomingForm(def);
            if (UseInboxFlow())
            {
                if (enableTimedArrivals)
                    EnqueueForArrival(def);
                else
                    SpawnHiddenInboxForm(def);
            }
            else
            {
                SpawnForm(def);
            }
        }

        QueueNextFromSpawnSource();
        hasBootstrappedSpawnSource = true;
    }

    private void QueueNextFromSpawnSource()
    {
        if (!useFormSpawnSource || formSpawnSource == null)
            return;

        var next = formSpawnSource.GetNextForm();
        if (next == null)
            return;

        QueueIncomingForm(next);
    }

    private void RemoveFromArrivalQueue(FormDefinition def)
    {
        if (def == null || !arrivalSet.Remove(def))
            return;

        var rebuilt = new Queue<FormDefinition>(arrivalQueue.Count);
        while (arrivalQueue.Count > 0)
        {
            var current = arrivalQueue.Dequeue();
            if (current == def)
                continue;
            rebuilt.Enqueue(current);
        }

        arrivalQueue = rebuilt;
    }

    private void NotifyPendingSpawnQueueChanged()
    {
        PendingSpawnQueueChanged?.Invoke(PendingSpawnCount);
    }

    private void SpawnHiddenInboxForm(FormDefinition def)
    {
        if (def == null)
            return;

        if (activeForms.TryGetValue(def, out var existingForm))
        {
            if (!inboxForms.Contains(existingForm))
                inboxForms.Add(existingForm);

            existingForm.SetInboxHidden(true);
            var existingDrag = existingForm.GetComponent<DraggableUI>();
            if (existingDrag != null)
                existingDrag.enabled = false;

            if (formFactory != null)
                formFactory.PlaceInInboxHidden(existingForm);

            GameSoundController.Instance?.PlayMailIncoming();
            NotifyPendingSpawnQueueChanged();
            return;
        }

        var formInstance = formFactory.Create(def);
        activeForms[def] = formInstance;
        activeFormDefs[formInstance] = def;
        formInstance.Expired += OnFormExpired;
        ApplyRewards(def.rewardsOnReceive, def, formInstance);

        var drag = formInstance.GetComponent<DraggableUI>();
        if (drag != null)
            drag.enabled = false;

        formInstance.SetInboxHidden(true);
        if (formFactory != null)
            formFactory.PlaceInInboxHidden(formInstance);

        inboxForms.Add(formInstance);
        GameSoundController.Instance?.PlayMailIncoming();
        NotifyPendingSpawnQueueChanged();
        FormCreated?.Invoke(def, formInstance);
        CheckLoseCondition();
    }

    private Form GetInboxFormClosestToExpiry()
    {
        Form bestForm = null;
        float bestRemainingSeconds = float.PositiveInfinity;

        for (int i = inboxForms.Count - 1; i >= 0; i--)
        {
            var form = inboxForms[i];
            if (form == null)
            {
                inboxForms.RemoveAt(i);
                continue;
            }

            float remainingSeconds = form.GetRemainingSeconds();
            if (bestForm == null || remainingSeconds < bestRemainingSeconds)
            {
                bestForm = form;
                bestRemainingSeconds = remainingSeconds;
            }
        }

        return bestForm;
    }

    private void RemoveInboxForm(Form form)
    {
        if (form == null)
            return;

        if (inboxForms.Remove(form))
            NotifyPendingSpawnQueueChanged();
    }

    private void RemoveInboxForm(FormDefinition def)
    {
        if (def == null)
            return;

        for (int i = inboxForms.Count - 1; i >= 0; i--)
        {
            var form = inboxForms[i];
            if (form == null)
            {
                inboxForms.RemoveAt(i);
                continue;
            }

            if (form.Definition != def)
                continue;

            inboxForms.RemoveAt(i);
            NotifyPendingSpawnQueueChanged();
            return;
        }
    }

    private void ResolveFormDock()
    {
        formDock = formDockBehaviour as IFormDock;
        if (formDock != null)
            return;

        var candidates = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Length; i++)
        {
            formDock = candidates[i] as IFormDock;
            if (formDock == null)
                continue;

            formDockBehaviour = candidates[i];
            return;
        }
    }

    private bool UseInboxFlow()
    {
        return !useFormDock;
    }

    private void EnsureWorkspaceTracker(Form form)
    {
        if (form == null || workspaceTable == null)
            return;

        var tracker = form.GetComponent<FormWorkspaceScaleTracker>();
        if (tracker == null)
            tracker = form.gameObject.AddComponent<FormWorkspaceScaleTracker>();

        tracker.Initialize(workspaceTable, onlyWhileDragging: true);
    }

    private void CheckLoseCondition()
    {
        if (loseSceneTriggered)
            return;

        bool shouldLoseFromFailureCount = false;
        if (enableLoseOnFailureCount && gameStateManager != null)
        {
            int failThreshold = Mathf.Max(0, maxFailedForms);
            shouldLoseFromFailureCount = gameStateManager.SubmissionFailCount >= failThreshold;
        }

        if (!shouldLoseFromFailureCount)
            return;

        loseSceneTriggered = true;
        if (enableSubmissionDebugLogs)
        {
            int failCount = gameStateManager != null ? gameStateManager.SubmissionFailCount : 0;
            Debug.Log($"[FormManager] Lose triggered: failCount={failCount}, fromFailureCount={shouldLoseFromFailureCount}");
        }
        LoseTriggered?.Invoke();

        if (!string.IsNullOrWhiteSpace(loseSceneName))
            SceneManager.LoadScene(loseSceneName);
    }
}
