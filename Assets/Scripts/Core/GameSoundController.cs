using UnityEngine;

public class GameSoundController : MonoBehaviour
{
    public enum DragSoundType
    {
        None,
        WordBlock,
        Form
    }

    public static GameSoundController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Mail")]
    [SerializeField] private AudioClip mailOutgoingClip;
    [SerializeField] private AudioClip mailIncomingClip;

    [Header("Drag")]
    [SerializeField] private AudioClip wordBlockPressClip;
    [SerializeField] private AudioClip wordBlockReleaseClip;
    [SerializeField] private AudioClip formPressClip;
    [SerializeField] private AudioClip formReleaseClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayMailOutgoing()
    {
        Play(mailOutgoingClip);
    }

    public void PlayMailIncoming()
    {
        Play(mailIncomingClip);
    }

    public void PlayDragPress(DragSoundType dragType)
    {
        switch (dragType)
        {
            case DragSoundType.WordBlock:
                Play(wordBlockPressClip);
                break;
            case DragSoundType.Form:
                Play(formPressClip);
                break;
        }
    }

    public void PlayDragRelease(DragSoundType dragType)
    {
        switch (dragType)
        {
            case DragSoundType.WordBlock:
                Play(wordBlockReleaseClip);
                break;
            case DragSoundType.Form:
                Play(formReleaseClip);
                break;
        }
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }
}
