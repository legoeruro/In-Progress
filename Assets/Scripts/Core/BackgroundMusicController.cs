using UnityEngine;

[DisallowMultipleComponent]
public class BackgroundMusicController : MonoBehaviour
{
    public static BackgroundMusicController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool persistAcrossScenes = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = volume;

        if (musicClip != null && audioSource.clip != musicClip)
            audioSource.clip = musicClip;

        if (playOnAwake)
            Play();
    }

    public void Play()
    {
        if (audioSource == null || audioSource.clip == null)
            return;

        audioSource.volume = volume;
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    public void Stop()
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
            audioSource.volume = volume;
    }

    public void SetMusic(AudioClip clip, bool restartIfPlaying = true)
    {
        musicClip = clip;
        if (audioSource == null)
            return;

        bool wasPlaying = audioSource.isPlaying;
        audioSource.clip = musicClip;

        if (restartIfPlaying && wasPlaying)
            audioSource.Play();
    }
}
