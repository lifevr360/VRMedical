using System.Collections;
using UnityEngine;

/// <summary>
/// Put this on the always-active parent of a burning object (Rack_1, TVUnit, ...), next to a
/// normal (non-trigger) Collider that covers the object.
/// Every frame an extinguisher spray hits that collider, a timer builds up. It adds up across
/// separate bursts. When it reaches Seconds To Extinguish the flames stop and die down,
/// the Polished object is swapped for the Burned one and the white smoke starts.
/// Each copy has its own timer, so every burning object is handled independently.
/// </summary>
public class ExtinguishableFire : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject fireEffects;            // ObjectN_Fire_Smoke (flames, smoke, audio)
    [SerializeField] private GameObject polishedObject;         // shown while burning
    [SerializeField] private GameObject burnedObject;           // shown once put out
    [SerializeField] private ParticleSystem whiteSmoke;         // ObjectN_White_Smoke, inside the burned object

    [Header("Tuning")]
    [SerializeField, Min(0.1f)] private float secondsToExtinguish = 2f;   // seconds of spray needed
    [SerializeField, Min(0f)] private float fireDieDownSeconds = 1.5f;    // flames stop emitting, then the fire is switched off

    private float exposure;         // seconds of spray received so far
    private int lastHitFrame = -1;
    private bool extinguished;

    /// <summary>Raised once, the moment this fire is put out (after the swap to the burned object).</summary>
    public event System.Action<ExtinguishableFire> Extinguished;

    public bool IsExtinguished => extinguished;


    private void Awake()
    {
        if (fireEffects == null || polishedObject == null || burnedObject == null || whiteSmoke == null)
        {
            Debug.LogError("ExtinguishableFire: assign Fire Effects, Polished Object, Burned Object and White Smoke.", this);
            enabled = false;
            return;
        }

        if (GetComponent<Collider>() == null)
            Debug.LogError("ExtinguishableFire: needs a Collider on this object to receive spray hits.", this);

        // Always start in the burning state, whatever was left switched on/off in the editor.
        fireEffects.SetActive(true);
        polishedObject.SetActive(true);
        burnedObject.SetActive(false);
    }

    // Sent by a particle system whose Collision module has "Send Collision Messages" on.
    private void OnParticleCollision(GameObject other)
    {
        if (extinguished || !enabled)
            return;

        if (!other.TryGetComponent<ExtinguishingSpray>(out _))
            return;

        // Count each frame once, however many spray systems or hits arrive in it.
        if (lastHitFrame == Time.frameCount)
            return;
        lastHitFrame = Time.frameCount;

        exposure += Time.deltaTime;
        if (exposure >= secondsToExtinguish)
            Extinguish();
    }

    private void Extinguish()
    {
        extinguished = true;

        polishedObject.SetActive(false);
        burnedObject.SetActive(true);
        whiteSmoke.gameObject.SetActive(true);
        whiteSmoke.Play();

        StartCoroutine(PutOutFire());
        Extinguished?.Invoke(this);
    }

    private IEnumerator PutOutFire()
    {
        // No new flames or smoke; the ones already alive fade out by themselves.
        foreach (ParticleSystem ps in fireEffects.GetComponentsInChildren<ParticleSystem>())
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        // Fade the crackling sound out over the same time.
        AudioSource[] audio = fireEffects.GetComponentsInChildren<AudioSource>();
        float[] startVolumes = new float[audio.Length];
        for (int i = 0; i < audio.Length; i++)
            startVolumes[i] = audio[i].volume;

        for (float elapsed = 0f; elapsed < fireDieDownSeconds; elapsed += Time.deltaTime)
        {
            float remaining = 1f - elapsed / fireDieDownSeconds;
            for (int i = 0; i < audio.Length; i++)
                audio[i].volume = startVolumes[i] * remaining;

            yield return null;
        }

        fireEffects.SetActive(false);
    }
}
