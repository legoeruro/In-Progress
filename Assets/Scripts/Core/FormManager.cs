
using System;
using System.Collections.Generic;
using UnityEngine;

public class FormManager : MonoBehaviour
{
    [Header("References")]
    public GameStateManager gameStateManager;
    public FormFactory formFactory;

    [Header("Form Groups")]
    public List<FormGroup> formGroups = new List<FormGroup>();
    public bool listenForFlagChanges = true;
    // public bool createFormsOnUnlock = true;

    [Header("Submission")]
    [SerializeField] private SubmitZone submitZone;

    [Header("Timing")]
    [SerializeField] private bool enableTimedArrivals = true;
    [SerializeField] private float idleCheckSeconds = 1f;

    // Form data
    private List<FormDefinition> incomingForms = new List<FormDefinition>();
    private List<FormDefinition> submittedForms = new List<FormDefinition>();
    // TODO: add failed forms
    private HashSet<FormGroup> unlockedGroups = new HashSet<FormGroup>();
    private Dictionary<FormDefinition, Form> activeForms = new Dictionary<FormDefinition, Form>();
    private Dictionary<Form, FormDefinition> activeFormDefs = new Dictionary<Form, FormDefinition>();
    private Dictionary<FormGroup, HashSet<FormDefinition>> groupRemainingForms = new Dictionary<FormGroup, HashSet<FormDefinition>>();
    private Dictionary<FormDefinition, FormGroup> groupByForm = new Dictionary<FormDefinition, FormGroup>();
    private Queue<FormDefinition> pendingSpawnQueue = new Queue<FormDefinition>();
    private HashSet<FormDefinition> pendingSpawnSet = new HashSet<FormDefinition>();
    private Coroutine arrivalRoutine;

    // Actions
    public event Action<FormGroup> FormGroupUnlocked;
    public event Action<FormDefinition, Form> FormCreated;
    public event Action<FormDefinition> FormSubmitted;
    public event Action<FormDefinition> FormSubmissionFailed;
    public event Action<FormDefinition> FormDiscarded;


    public List<FormDefinition> IncomingForms => incomingForms;
    public IReadOnlyList<FormDefinition> SubmittedForms => submittedForms;

    // do we need this?
    public IReadOnlyList<WordBlock> OwnedWordBlocks => gameStateManager != null
        ? gameStateManager.OwnedWordBlocks
        : Array.Empty<WordBlock>();

    private void Awake()
    {
        if (gameStateManager == null)
            gameStateManager = GameStateManager.Instance;
    }

    private void OnEnable()
    {
        if (listenForFlagChanges && gameStateManager != null)
            gameStateManager.FlagStateChanged += OnFlagStateChanged;

        if (submitZone != null)
            submitZone.SubmitRequested += OnSubmitRequested;

        if (enableTimedArrivals)
            arrivalRoutine = StartCoroutine(FormArrivalLoop());

        UpdateFormAvailability();
    }

    private void OnDisable()
    {
        if (listenForFlagChanges && gameStateManager != null)
            gameStateManager.FlagStateChanged -= OnFlagStateChanged;

        if (submitZone != null)
            submitZone.SubmitRequested -= OnSubmitRequested;

        if (arrivalRoutine != null)
        {
            StopCoroutine(arrivalRoutine);
            arrivalRoutine = null;
        }
    }

    private void OnFlagStateChanged(GameFlags flag, bool state)
    {
        UpdateFormAvailability();
    }

    public void UpdateFormAvailability()
    {
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
        }
    }

    public void QueueIncomingForm(FormDefinition formDefinition)
    {
        if (!AddIncomingForm(formDefinition)) return;

        EnqueueForSpawn(formDefinition);
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
        formInstance.PlaySpawnAnimation();
        FormCreated?.Invoke(def, formInstance);
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

        bool isValidSubmission = form.VerifyForm() && !def.shouldBeDiscarded;
        ResolveFormOutcome(def, form, isSuccess: isValidSubmission, wasDiscarded: false);
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
        ResolveFormOutcome(def, form, isSuccess: false, wasDiscarded: true);
        return true;
    }

    private void ResolveFormOutcome(FormDefinition def, Form form, bool isSuccess, bool wasDiscarded)
    {
        incomingForms.Remove(def);
        if (!submittedForms.Contains(def))
            submittedForms.Add(def);

        form.gameObject.SetActive(false);

        if (isSuccess)
        {
            if (gameStateManager != null)
                gameStateManager.RegisterSubmissionSuccess();
            FormSubmitted?.Invoke(def);
        }
        else
        {
            if (gameStateManager != null)
                gameStateManager.RegisterSubmissionFailure();
            FormSubmissionFailed?.Invoke(def);
            if (wasDiscarded)
                FormDiscarded?.Invoke(def);
        }

        CheckForGroupCompletion(def);
        form.Expired -= OnFormExpired;
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

        if (def != null)
        {
            incomingForms.Remove(def);
            if (gameStateManager != null)
                gameStateManager.RegisterExpiredForm(def);
            CheckForGroupCompletion(def);
        }

        form.Expired -= OnFormExpired;
        activeFormDefs.Remove(form);
        if (def != null)
            activeForms.Remove(def);
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

    private void EnqueueForSpawn(FormDefinition def)
    {
        if (def == null) return;
        if (activeForms.ContainsKey(def)) return;
        if (pendingSpawnSet.Contains(def)) return;

        pendingSpawnQueue.Enqueue(def);
        pendingSpawnSet.Add(def);
    }

    private FormDefinition DequeueNextSpawn()
    {
        while (pendingSpawnQueue.Count > 0)
        {
            var next = pendingSpawnQueue.Dequeue();
            pendingSpawnSet.Remove(next);

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

            var next = DequeueNextSpawn();
            if (next == null)
            {
                yield return new WaitForSeconds(idleCheckSeconds);
                continue;
            }

            float delay = gameStateManager.GetCurrentFormArrivalDelay();
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            SpawnForm(next);
        }
    }
}
