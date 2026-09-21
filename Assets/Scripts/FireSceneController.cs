using System.Collections;
using UnityEngine;

/// <summary>
/// Watches every ExtinguishableFire in the scene. Once all of them have been put out, the
/// Burning Smokes (the smoke that is not tied to one object) stop emitting, fade out and are switched off.
/// Fires are found automatically, so only the smoke objects need to be assigned.
/// Put this on an object that stays active, e.g. the Scripts group.
/// </summary>
public class FireSceneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] burningSmokes;                    // Burning Smoke (1), Burning Smoke (2)

    [Header("Tuning")]
    [SerializeField, Min(0f)] private float dieDownSeconds = 5f;            // smoke stops emitting, then is switched off after this long (0 = at once)

    private ExtinguishableFire[] fires;
    private int remaining;          // fires still burning


    private void Start()
    {
        fires = FindObjectsByType<ExtinguishableFire>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (fires.Length == 0)
        {
            Debug.LogWarning("FireSceneController: no ExtinguishableFire found in the scene.", this);
            return;
        }

        foreach (ExtinguishableFire fire in fires)
        {
            if (fire.IsExtinguished)
                continue;

            remaining++;
            fire.Extinguished += OnFireExtinguished;
        }
    }

    private void OnDestroy()
    {
        if (fires == null)
            return;

        foreach (ExtinguishableFire fire in fires)
            if (fire != null)
                fire.Extinguished -= OnFireExtinguished;
    }

    private void OnFireExtinguished(ExtinguishableFire fire)
    {
        fire.Extinguished -= OnFireExtinguished;

        remaining--;
        if (remaining <= 0)
            StartCoroutine(PutOutBurningSmokes());
    }

    private IEnumerator PutOutBurningSmokes()
    {
        // No new smoke; the puffs already in the air fade out by themselves.
        foreach (GameObject smoke in burningSmokes)
        {
            if (smoke == null)
                continue;

            foreach (ParticleSystem ps in smoke.GetComponentsInChildren<ParticleSystem>())
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(dieDownSeconds);

        foreach (GameObject smoke in burningSmokes)
            if (smoke != null)
                smoke.SetActive(false);
    }
}
