using UnityEngine;
using UnityEngine.SceneManagement;

public class RetryGameButton : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "GameScene";

    public void Retry()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        else
            SceneManager.LoadScene(gameplaySceneName);
    }
}
