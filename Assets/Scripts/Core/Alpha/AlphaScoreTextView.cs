using TMPro;
using UnityEngine;

public class AlphaScoreTextView : MonoBehaviour
{
    [SerializeField] private AlphaScoreService scoreService;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string scorePrefix = "Score: ";
    [SerializeField] private bool showDelta;

    private void Awake()
    {
        if (scoreService == null)
            scoreService = FindFirstObjectByType<AlphaScoreService>();

        if (scoreText == null)
            scoreText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (scoreService != null)
            scoreService.ScoreChanged += OnScoreChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (scoreService != null)
            scoreService.ScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int score, int delta)
    {
        if (scoreText == null) return;

        if (showDelta && delta != 0)
            scoreText.text = $"{scorePrefix}{score} ({delta:+#;-#;0})";
        else
            scoreText.text = $"{scorePrefix}{score}";
    }

    public void Refresh()
    {
        if (scoreText == null) return;

        int score = scoreService != null ? scoreService.CurrentScore : AlphaScoreSession.CurrentScore;
        scoreText.text = $"{scorePrefix}{score}";
    }
}
