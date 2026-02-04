using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Serializable]
    public struct FlagStateEntry
    {
        public GameFlags flag;
        public bool initialState;
    }

    [Header("Flags")]
    [SerializeField] List<FlagStateEntry> initialFlagStates = new List<FlagStateEntry>();
    private List<GameFlags> flags = new List<GameFlags>();

    [Header("Word Blocks")]
    [SerializeField] private List<WordBlock> ownedWordBlocks = new List<WordBlock>();

    [Header("Form Timing")]
    [SerializeField] private float baseFormArrivalSeconds = 10f;
    [SerializeField] private float perSubmissionDecreaseSeconds = 0.2f;
    [SerializeField] private float minFormArrivalSeconds = 3f;

    [Header("Failures")]
    [SerializeField] private List<FailFlagRule> failFlagRules = new List<FailFlagRule>();

    [Header("Expired Forms")]
    [SerializeField] private List<FormDefinition> expiredForms = new List<FormDefinition>();

    private readonly Dictionary<GameFlags, bool> flagStates = new Dictionary<GameFlags, bool>();
    private readonly Dictionary<string, GameFlags> flagByName = new Dictionary<string, GameFlags>(StringComparer.OrdinalIgnoreCase);

    public event Action<GameFlags, bool> FlagStateChanged;

    public IReadOnlyDictionary<GameFlags, bool> FlagStates => flagStates;
    public IReadOnlyList<WordBlock> OwnedWordBlocks => ownedWordBlocks;
    public IReadOnlyList<FormDefinition> ExpiredForms => expiredForms;

    public int SubmissionCount { get; private set; }
    public int SubmissionFailCount { get; private set; }

    [Serializable]
    public struct FailFlagRule
    {
        public int failCount;
        public GameFlags flagToSetTrue;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeFlags();
    }

    private void InitializeFlags()
    {
        flagStates.Clear();
        flagByName.Clear();

        if (initialFlagStates != null)
        {
            foreach (var entry in initialFlagStates)
            {
                if (entry.flag == null) continue;
                RegisterFlag(entry.flag, entry.initialState, silent: true);
            }
        }
    }

    public void RegisterFlag(GameFlags flag, bool initialState = false, bool silent = false)
    {
        if (flag == null) return;

        if (!flags.Contains(flag))
            flags.Add(flag);

        if (!flagStates.ContainsKey(flag))
            flagStates[flag] = initialState;
        else
            flagStates[flag] = initialState;

        if (!string.IsNullOrWhiteSpace(flag.flagName))
            flagByName[flag.flagName] = flag;

        if (!silent)
            FlagStateChanged?.Invoke(flag, initialState);
    }

    public void SetFlagState(GameFlags flag, bool state)
    {
        if (flag == null) return;

        if (!flagStates.TryGetValue(flag, out var currentState))
        {
            RegisterFlag(flag, state, silent: true);
            FlagStateChanged?.Invoke(flag, state);
            return;
        }

        if (currentState == state) return;

        flagStates[flag] = state;
        FlagStateChanged?.Invoke(flag, state);
    }

    /// <summary>
    /// To be used for special flags if we wanna have special behavior
    /// </summary>
    /// <param name="flagName">Flag name</param>
    /// <param name="state"></param>
    public void SetFlagState(string flagName, bool state)
    {
        if (string.IsNullOrWhiteSpace(flagName)) return;

        if (!flagByName.TryGetValue(flagName, out var flag))
        {
            var found = flags.Find(f => f != null && string.Equals(f.flagName, flagName, StringComparison.OrdinalIgnoreCase));
            if (found == null)
            {
                Debug.LogWarning($"GameStateManager: Flag '{flagName}' not found.");
                return;
            }

            flag = found;
            flagByName[flagName] = flag;
        }

        SetFlagState(flag, state);
    }

    public bool GetFlagState(GameFlags flag)
    {
        return flag != null && flagStates.TryGetValue(flag, out var state) && state;
    }

    /// <summary>
    /// To be used for special flags if we wanna have special behavior
    /// </summary>
    /// <param name="flagName">Flag name</param>
    /// <param name="state"></param>
    public bool GetFlagState(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName)) return false;
        if (!flagByName.TryGetValue(flagName, out var flag)) return false;
        return GetFlagState(flag);
    }

    public void ResetFlags(bool defaultState = false)
    {
        foreach (var flag in flags)
        {
            if (flag == null) continue;
            SetFlagState(flag, defaultState);
        }
    }

    public float GetCurrentFormArrivalDelay()
    {
        float delay = baseFormArrivalSeconds - (SubmissionCount * perSubmissionDecreaseSeconds);
        return Mathf.Clamp(delay, minFormArrivalSeconds, baseFormArrivalSeconds);
    }

    public void RegisterSubmissionSuccess()
    {
        SubmissionCount++;
    }

    public void RegisterSubmissionFailure()
    {
        SubmissionFailCount++;
        EvaluateFailureRules();
    }

    public void RegisterExpiredForm(FormDefinition formDefinition)
    {
        if (formDefinition == null) return;
        if (!expiredForms.Contains(formDefinition))
            expiredForms.Add(formDefinition);
    }

    private void EvaluateFailureRules()
    {
        if (failFlagRules == null || failFlagRules.Count == 0) return;

        foreach (var rule in failFlagRules)
        {
            if (rule.failCount <= 0 || rule.flagToSetTrue == null) continue;
            if (SubmissionFailCount >= rule.failCount)
                SetFlagState(rule.flagToSetTrue, true);
        }
    }

    public void RegisterWordBlock(WordBlock wordBlock)
    {
        if (wordBlock == null) return;
        if (!ownedWordBlocks.Contains(wordBlock))
            ownedWordBlocks.Add(wordBlock);
    }

    public void UnregisterWordBlock(WordBlock wordBlock)
    {
        if (wordBlock == null) return;
        ownedWordBlocks.Remove(wordBlock);
    }
}
