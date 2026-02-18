using System;
using UnityEngine;

public class AlphaScoreService : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int basePoints = 100;
    [SerializeField] private int fieldScalar = 25;
    [SerializeField] private int spawnScalar = 60;
    [SerializeField] private int speedBonusMax = 50;
    [SerializeField] private bool clampScoreToZero = true;

    public int CurrentScore { get; private set; }
    public event Action<int, int> ScoreChanged;

    private void Awake()
    {
        CurrentScore = AlphaScoreSession.CurrentScore;
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        AlphaScoreSession.Reset();
        ScoreChanged?.Invoke(CurrentScore, 0);
    }

    public void ApplySubmission(bool isSuccess, Form form, FormValidationResult validation, float spawnPressure01)
    {
        int delta;

        if (isSuccess)
        {
            int fieldBonus = fieldScalar * Mathf.Max(0, validation.CorrectFields);
            int spawnBonus = Mathf.RoundToInt(spawnScalar * Mathf.Clamp01(spawnPressure01));
            int speedBonus = form != null ? Mathf.RoundToInt(speedBonusMax * form.GetRemainingTimeNormalized()) : 0;

            delta = basePoints + fieldBonus + spawnBonus + speedBonus;
        }
        else
        {
            int penalty = basePoints + fieldScalar * Mathf.Max(0, validation.IncorrectOrEmptyFields);
            delta = -penalty;
        }

        int before = CurrentScore;
        CurrentScore += delta;
        if (clampScoreToZero)
            CurrentScore = Mathf.Max(0, CurrentScore);

        AlphaScoreSession.SetScore(CurrentScore);
        ScoreChanged?.Invoke(CurrentScore, CurrentScore - before);
    }
}
