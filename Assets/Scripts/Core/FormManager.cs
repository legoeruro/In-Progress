
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

    // Form data
    private List<FormDefinition> incomingForms = new List<FormDefinition>();
    private List<FormDefinition> submittedForms = new List<FormDefinition>();
    // TODO: add failed forms
    private HashSet<FormGroup> unlockedGroups = new HashSet<FormGroup>();
    private Dictionary<FormDefinition, Form> activeForms = new Dictionary<FormDefinition, Form>();
    private Dictionary<Form, FormDefinition> activeFormDefs = new Dictionary<Form, FormDefinition>();

    // Actions
    public event Action<FormGroup> FormGroupUnlocked;
    public event Action<FormDefinition, Form> FormCreated;
    public event Action<FormDefinition> FormSubmitted;


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

        UpdateFormAvailability();
    }

    private void OnDisable()
    {
        if (listenForFlagChanges && gameStateManager != null)
            gameStateManager.FlagStateChanged -= OnFlagStateChanged;
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

        if (group.forms == null) return;

        foreach (var def in group.forms)
        {
            if (def == null) continue;

            QueueIncomingForm(def);

            // TODO: if we are thinking of forms/mails that are sent when you unlock something, add it here 
        }
    }

    private void DisableGroup(FormGroup group)
    {
        if (group == null) return;

        if (group.forms != null)
        {
            foreach (var def in group.forms)
            {
                if (def == null) continue;

                if (!submittedForms.Contains(def))
                    incomingForms.Remove(def);

                // if (activeForms.TryGetValue(def, out var formInstance))
                //     formInstance.gameObject.SetActive(false);
            }
        }
    }

    public void QueueIncomingForm(FormDefinition formDefinition)
    {
        if (formDefinition == null) return;
        if (submittedForms.Contains(formDefinition)) return;
        if (!incomingForms.Contains(formDefinition))
            incomingForms.Add(formDefinition);
    }

    public bool SubmitForm(Form form)
    {
        if (form == null) return false;
        if (!form.VerifyForm()) return false;

        if (!activeFormDefs.TryGetValue(form, out var def))
        {
            Debug.LogWarning("FormManager: Tried to submit a form that was not created by this manager.");
            return false;
        }

        incomingForms.Remove(def);
        if (!submittedForms.Contains(def))
            submittedForms.Add(def);

        form.gameObject.SetActive(false);
        FormSubmitted?.Invoke(def);

        return true;
    }

}
