using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Vibrates the controller that holds the nozzle while the extinguisher is spraying.
/// Put this on the nozzle object, next to its XR Grab Interactable. FireExtinguisherLever turns the
/// spraying on and off (the same place that starts and stops the smoke).
/// Only the hand holding the nozzle vibrates, whichever hand that is. Hands without a controller
/// (hand tracking) have no haptics, so nothing happens for them.
/// A short buzz is sent again and again instead of one long one, so the vibration dies out by itself
/// a moment after the spray stops or the nozzle is let go.
/// </summary>
public class NozzleHaptics : MonoBehaviour
{
    [Header("Feel")]
    [SerializeField, Range(0f, 1f)] private float strength = 0.4f;      // 0 = off, 1 = strongest
    [SerializeField, Min(0.02f)] private float buzzInterval = 0.1f;     // seconds between buzzes
    [SerializeField, Min(0.02f)] private float buzzDuration = 0.12f;    // length of one buzz; a little longer than the interval so it feels continuous

    private XRBaseInteractable nozzle;
    private HapticImpulsePlayer holderHaptics;      // the controller holding the nozzle, if any
    private bool spraying;
    private float nextBuzzTime;


    private void Awake()
    {
        nozzle = GetComponent<XRBaseInteractable>();
        if (nozzle == null)
        {
            Debug.LogError("NozzleHaptics: needs an XR Grab Interactable on this object.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (nozzle == null)
            return;

        nozzle.selectEntered.AddListener(OnNozzleGrabbed);
        nozzle.selectExited.AddListener(OnNozzleReleased);
    }

    private void OnDisable()
    {
        if (nozzle != null)
        {
            nozzle.selectEntered.RemoveListener(OnNozzleGrabbed);
            nozzle.selectExited.RemoveListener(OnNozzleReleased);
        }

        // Never leave this switched on while disabled, so nothing can get stuck buzzing.
        spraying = false;
        holderHaptics = null;
    }

    /// <summary>Called by FireExtinguisherLever when the spray starts (true) or stops (false).</summary>
    public void SetSpraying(bool value)
    {
        spraying = value;
        nextBuzzTime = 0f;      // first buzz right away
    }

    private void OnNozzleGrabbed(SelectEnterEventArgs args)
    {
        // The controller that grabbed the nozzle. A tracked hand has no haptic player, so this stays null.
        holderHaptics = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>();
    }

    private void OnNozzleReleased(SelectExitEventArgs _)
    {
        holderHaptics = null;
    }

    private void Update()
    {
        if (!spraying || holderHaptics == null || Time.time < nextBuzzTime)
            return;

        holderHaptics.SendHapticImpulse(strength, buzzDuration);
        nextBuzzTime = Time.time + buzzInterval;
    }
}
