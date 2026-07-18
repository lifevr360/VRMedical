using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SnapToTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject dropGhost;   // transparent target; provides pose + gets disabled on snap

    [Header("Options")]
    [SerializeField] private bool disableGhostOnSnap = true;   // uncheck to keep the ghost visible after snap

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
        if (grab != null) grab.enabled = false;
        if (disableGhostOnSnap) dropGhost.SetActive(false);

        snapped = true;
    }
}
