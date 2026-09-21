using UnityEngine;

/// <summary>
/// Plays a looping spray sound from the nozzle while the extinguisher is spraying.
/// Put this on the nozzle object. FireExtinguisherLever turns the spraying on and off (the same place that
/// starts and stops the smoke and the vibration). The sound fades in quickly, and fades out a moment after
/// the handle is released. The clip must loop cleanly.
/// It lives on the nozzle, not on the smoke object, because the smoke object is switched off on release,
/// which would cut the sound instead of fading it.
/// </summary>
public class SpraySound : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioClip loopClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;    // 1 = comes from the nozzle, 0 = plain stereo

    [Header("Feel")]
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.05f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.2f;

    private AudioSource source;
    private bool spraying;
    private float level;            // 0 = silent, 1 = full volume


    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.clip = loopClip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.dopplerLevel = 0f;       // the nozzle moves with the hand, so no pitch shifting
        source.volume = 0f;
    }

    private void OnDisable()
    {
        spraying = false;
        level = 0f;

        if (source != null)
            source.Stop();
    }

    /// <summary>Called by FireExtinguisherLever when the spray starts (true) or stops (false).</summary>
    public void SetSpraying(bool value)
    {
        spraying = value;
    }

    private void Update()
    {
        float target = spraying ? 1f : 0f;
        if (!Mathf.Approximately(level, target))
        {
            float duration = spraying ? fadeInSeconds : fadeOutSeconds;
            level = duration > 0f ? Mathf.MoveTowards(level, target, Time.deltaTime / duration) : target;
        }

        source.volume = level * volume;

        if (level > 0f && !source.isPlaying && loopClip != null)
        {
            source.time = Random.value * loopClip.length;       // start somewhere different each time
            source.Play();
        }
        else if (level <= 0f && source.isPlaying)
        {
            source.Stop();
        }
    }
}
