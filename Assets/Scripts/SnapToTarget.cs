using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SnapToTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject dropGhost;   // transparent target; provides pose + gets disabled on snap

    [Header("Options")]
    [SerializeField] private bool disableGhostOnSnap = true;   // uncheck to keep the ghost visible after snap

    [Header("Scenario 2 - swap on snap")]
    [SerializeField] private GameObject knobHandle;            // handle 3 (XRKnob) - enabled on snap
    [SerializeField] private bool disableSelfOnSnap = true;    // disable the carry handle (this object) on snap
    public UnityEngine.Events.UnityEvent onSnapped;            // fired after a successful snap

    private Rigidbody rb;
    private XRGrabInteractable grab;
    private Blink ghostBlink;
    private bool snapped = false;

    public bool IsSnapped => snapped;


    private void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        if (dropGhost != null)
            ghostBlink = dropGhost.GetComponent<Blink>();

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs _)
    {
        
        if (snapped) return;
        if (ghostBlink != null) ghostBlink.isBlink = true;
    }

    private void OnReleased(SelectExitEventArgs _)
    {
        if (snapped) return;
        if (ghostBlink != null) ghostBlink.isBlink = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (snapped || dropGhost == null) return;
        if (other.gameObject != dropGhost) return;   // only the assigned ghost triggers this pick

        // 1. Snap pose
        transform.position = dropGhost.transform.position;
        transform.rotation = dropGhost.transform.rotation;

        // 2. Freeze physics
        if (rb != null)
        {
            rb.velocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
            rb.useGravity      = false;
        }

        // 3. Stop blinking, prevent re-grab, optionally hide ghost
        if (ghostBlink != null) ghostBlink.isBlink = false;
        if (grab != null) grab.enabled = true;
        if (disableGhostOnSnap) dropGhost.SetActive(false);

        snapped = true;

        Debug.Log($"[SnapToTarget] '{gameObject.name}' snapped to '{dropGhost.name}'.");

        // Scenario 2 swap: bring in the knob handle, notify, then hide the carry handle.
        if (knobHandle != null) knobHandle.SetActive(true);
        onSnapped?.Invoke();
        if (disableSelfOnSnap) gameObject.SetActive(false);
    }
}
