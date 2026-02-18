using TMPro;
using UnityEngine;

public class AlphaLoseScoreText : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private string scorePrefix = "Final Score: ";

    private void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponent<TMP_Text>();

        if (scoreText == null)
            return;

        scoreText.text = $"{scorePrefix}{AlphaScoreSession.CurrentScore}";
    }
}
