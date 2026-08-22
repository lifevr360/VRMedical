using UnityEngine;

/// <summary>
/// Rotates the operating lever onto the carry handle when the extinguisher is grabbed,
/// and back to its rest angle when it is released.
/// Both angles are absolute local euler values -- what you type here is what the
/// pivot's Rotation field will read in the inspector.
/// Hook OnGrabbed / OnReleased to the XR Grab Interactable's Select Entered / Select Exited events.
/// </summary>
public class FireExtinguisherLever : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leverPivot;              // HandleHInge

    [Header("Poses (absolute local rotation)")]
    [SerializeField] private Vector3 restEuler = Vector3.zero;
    [SerializeField] private Vector3 squeezedEuler = new Vector3(0f, 24f, 0f);

    [Header("Feel")]
    [SerializeField] private float squeezeDuration = 0.12f;     // seconds to close
    [SerializeField] private float releaseDuration = 0.20f;     // seconds to spring back

    private float t;            // 0 = rest, 1 = squeezed
    private float target;


    private void Awake()
    {
        if (leverPivot == null)
        {
            Debug.LogError("FireExtinguisherLever: leverPivot is not assigned.", this);
            enabled = false;
            return;
        }

        leverPivot.localEulerAngles = restEuler;
    }

    /// <summary>Called on grab. Swings the lever up to the carry handle.</summary>
    public void OnGrabbed()
    {
        target = 1f;
    }

    /// <summary>Called on release. Springs the lever back to rest.</summary>
    public void OnReleased()
    {
        target = 0f;
    }

    private void Update()
    {
        if (Mathf.Approximately(t, target))
            return;

        float duration = (target > t) ? squeezeDuration : releaseDuration;
        t = Mathf.MoveTowards(t, target, Time.deltaTime / Mathf.Max(duration, 0.0001f));

        // Snap to the exact authored value at the ends so the inspector reads what you typed,
        // rather than a quaternion round-trip like 23.99998.
        if (Mathf.Approximately(t, 0f))
            leverPivot.localEulerAngles = restEuler;
        else if (Mathf.Approximately(t, 1f))
            leverPivot.localEulerAngles = squeezedEuler;
        else
            leverPivot.localRotation = Quaternion.Slerp(Quaternion.Euler(restEuler),
                                                        Quaternion.Euler(squeezedEuler),
                                                        t);
    }


#if UNITY_EDITOR
    [ContextMenu("Capture Current As Rest")]
    private void CaptureRest()
    {
        if (leverPivot == null) return;
        UnityEditor.Undo.RecordObject(this, "Capture Rest");
        restEuler = leverPivot.localEulerAngles;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Capture Current As Squeezed")]
    private void CaptureSqueezed()
    {
        if (leverPivot == null) return;
        UnityEditor.Undo.RecordObject(this, "Capture Squeezed");
        squeezedEuler = leverPivot.localEulerAngles;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
