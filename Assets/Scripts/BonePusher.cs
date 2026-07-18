using UnityEngine;

public class BonePusher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bone;

    [Header("Rotation Range (local Euler Z)")]
    [SerializeField] private float startAngleZ = -2.4f;
    [SerializeField] private float endAngleZ   =  0f;

    [Header("Push Settings")]
    [SerializeField] private float pushAxisSign     = 1f;     // tip Z INCREASES on push -> +1
    [SerializeField] private float fullPushDistance = 0.08f;  // metres of push to reach point B

    [Header("Feel")]
    [SerializeField] private float followSpeed       = 12f;
    [SerializeField] private float releaseGraceTime  = 0.15f;

    private Transform tool;
    private bool  startCaptured = false;
    private float contactStartZ;
    private float timeSinceLastTouch = 999f;

    private float progress;        // 0..1, monotonic (ratchet)
    private float currentAngle;
    private bool  locked = false;  // true once we reach point B; no coming back

    public float Progress => progress;

    private void Start()
    {
        currentAngle = startAngleZ;
        ApplyRotation(currentAngle);
    }

    private void OnTriggerEnter(Collider other) { RegisterTouch(other); }
    private void OnTriggerStay(Collider other)  { RegisterTouch(other); }

    private void RegisterTouch(Collider other)
    {
        if (locked) return;
        if (!other.CompareTag("BoneTool")) return;

        tool = other.transform;
        timeSinceLastTouch = 0f;

        if (!startCaptured)
        {
            contactStartZ = tool.position.z;
            startCaptured = true;
        }
    }

    private void Update()
    {
        if (locked) return;

        timeSinceLastTouch += Time.deltaTime;
        bool reallyInContact = timeSinceLastTouch < releaseGraceTime;

        if (reallyInContact && tool != null && startCaptured)
        {
            float delta = (tool.position.z - contactStartZ) * pushAxisSign;
            if (delta > 0f)
            {
                float p = Mathf.Clamp01(delta / fullPushDistance);
                if (p > progress) progress = p;   // ratchet: only forward
            }
        }
        else
        {
            // real release -> stop tracking, but KEEP progress (no return to start)
            startCaptured = false;
            tool = null;
        }

        float targetAngle = Mathf.Lerp(startAngleZ, endAngleZ, progress);
        currentAngle = Mathf.MoveTowards(currentAngle, targetAngle,
                                         followSpeed * Mathf.Abs(endAngleZ - startAngleZ) * Time.deltaTime);
        ApplyRotation(currentAngle);

        if (progress >= 1f && Mathf.Approximately(currentAngle, endAngleZ))
            locked = true;
    }

    private void ApplyRotation(float zDeg)
    {
        if (bone == null) return;
        Vector3 e = bone.localEulerAngles;
        e.z = zDeg;
        bone.localEulerAngles = e;
    }
}
