using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// The safety pin ("lock") of an extinguisher. Put this on the pin object, next to a Collider and an
/// XR Simple Interactable.
/// While the pin is in place the handle's interactable is switched off, so the handle cannot be
/// grabbed: the lever does not squeeze and no spray starts.
/// Grab the pin and pull it Pull Distance away from where it sits to unlock the handle. Let go too
/// early and the pin springs back. Once it has been pulled out, letting go of it removes it.
/// The pin follows the hand in its parent's space, so this also works while the extinguisher moves.
/// </summary>
public class ExtinguisherLock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRBaseInteractable handleInteractable;     // HandleHinge's XR Simple Interactable

    [Header("Feel")]
    [SerializeField, Min(0.005f)] private float pullDistance = 0.06f;   // metres the pin must be pulled out
    [SerializeField, Min(0f)] private float returnSeconds = 0.15f;      // seconds to spring back if let go too early

    private XRBaseInteractable pinInteractable;
    private Vector3 restLocalPosition;

    private IXRSelectInteractor holder;         // the hand holding the pin, if any
    private Vector3 grabHandLocal;              // hand position (parent space) when the pin was grabbed
    private Vector3 grabPinOffset;              // how far out the pin already was when grabbed
    private bool pulled;

    /// <summary>True once the pin has been pulled out and the handle is unlocked.</summary>
    public bool IsPulled => pulled;

    /// <summary>True once the pin has been grabbed at least once.</summary>
    public bool HasBeenGrabbed { get; private set; }


    private void Awake()
    {
        pinInteractable = GetComponent<XRBaseInteractable>();
        if (pinInteractable == null || handleInteractable == null)
        {
            Debug.LogError("ExtinguisherLock: needs an XR interactable on this pin and Handle Interactable assigned.", this);
            enabled = false;
            return;
        }

        restLocalPosition = transform.localPosition;

        // Start locked: the handle ignores hands until the pin is pulled out.
        handleInteractable.enabled = false;
    }

    private void OnEnable()
    {
        if (pinInteractable == null)
            return;

        pinInteractable.selectEntered.AddListener(OnPinGrabbed);
        pinInteractable.selectExited.AddListener(OnPinReleased);
    }

    private void OnDisable()
    {
        if (pinInteractable == null)
            return;

        pinInteractable.selectEntered.RemoveListener(OnPinGrabbed);
        pinInteractable.selectExited.RemoveListener(OnPinReleased);
    }

    private void OnPinGrabbed(SelectEnterEventArgs args)
    {
        StopAllCoroutines();        // a spring-back may still be running

        HasBeenGrabbed = true;
        holder = args.interactorObject;
        grabHandLocal = ToParentSpace(HandPosition());
        grabPinOffset = transform.localPosition - restLocalPosition;
    }

    private void OnPinReleased(SelectExitEventArgs _)
    {
        holder = null;
        StartCoroutine(pulled ? HidePin() : ReturnToRest());
    }

    private void LateUpdate()
    {
        if (holder == null)
            return;

        // The pin follows the hand. Measured in the parent's space, so it still works while the
        // extinguisher is being carried or moved.
        Vector3 handMoved = ToParentSpace(HandPosition()) - grabHandLocal;
        transform.localPosition = restLocalPosition + grabPinOffset + handMoved;

        if (!pulled && Vector3.Distance(transform.position, FromParentSpace(restLocalPosition)) >= pullDistance)
        {
            pulled = true;
            handleInteractable.enabled = true;
        }
    }

    private IEnumerator ReturnToRest()
    {
        Vector3 from = transform.localPosition;
        for (float elapsed = 0f; elapsed < returnSeconds; elapsed += Time.deltaTime)
        {
            transform.localPosition = Vector3.Lerp(from, restLocalPosition, elapsed / returnSeconds);
            yield return null;
        }

        transform.localPosition = restLocalPosition;
    }

    private IEnumerator HidePin()
    {
        yield return null;      // let the pin's own select event finish first
        gameObject.SetActive(false);
    }

    private Vector3 HandPosition()
    {
        return holder.GetAttachTransform(pinInteractable).position;
    }

    private Vector3 ToParentSpace(Vector3 world)
    {
        return transform.parent != null ? transform.parent.InverseTransformPoint(world) : world;
    }

    private Vector3 FromParentSpace(Vector3 local)
    {
        return transform.parent != null ? transform.parent.TransformPoint(local) : local;
    }
}
