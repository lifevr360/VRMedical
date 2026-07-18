using UnityEngine;

public class SkinCutController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer skinMesh;

    [SerializeField] private Transform cutStart;
    [SerializeField] private Transform cutEnd;

    [SerializeField] private Transform scalpelTip;

    [Header("Blend Shape")]
    [SerializeField] private int blendShapeCount = 6;

    [Header("Cut Validation")]
    [SerializeField] private float cutWidth = 0.015f;
    [SerializeField] private float minScalpelSpeed = 0.03f;

    [Header("Blade Angle")]
    [SerializeField] private Transform bladeForward;
    [SerializeField] private float requiredAngleDot = 0.5f;

    [Header("Progress")]
    [SerializeField] private float smoothSpeed = 12f;
    [SerializeField] private float maxProgressStep = 0.08f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private bool isCutting = false;

    private Vector3 lastScalpelPos;

    private float targetProgress = 0f;
    private float currentProgress = 0f;

    private Vector3 lineDir;
    private float lineLength;

    private void Start()
    {
        lineDir = (cutEnd.position - cutStart.position).normalized;
        lineLength = Vector3.Distance(cutStart.position, cutEnd.position);

        lastScalpelPos = scalpelTip.position;
    }

    private void Update()
    {
        if (!isCutting)
            return;

        ProcessCut();

        // Smooth progression
        currentProgress = Mathf.Lerp(
            currentProgress,
            targetProgress,
            Time.deltaTime * smoothSpeed
        );

        ApplyBlendShapes(currentProgress);

        lastScalpelPos = scalpelTip.position;
    }

    private void ProcessCut()
    {
        Vector3 scalpelPos = scalpelTip.position;

        //-----------------------------------------
        // 1. Check distance from incision line
        //-----------------------------------------

        Vector3 toScalpel = scalpelPos - cutStart.position;

        float projection =
            Vector3.Dot(toScalpel, lineDir);

        float normalizedT =
            Mathf.Clamp01(projection / lineLength);

        Vector3 closestPoint =
            cutStart.position + lineDir * projection;

        float distFromLine =
            Vector3.Distance(scalpelPos, closestPoint);

        if (distFromLine > cutWidth)
            return;

        //-----------------------------------------
        // 2. Validate movement direction/speed
        //-----------------------------------------

        Vector3 velocity =
            (scalpelPos - lastScalpelPos) / Time.deltaTime;

        float speedAlongLine =
            Vector3.Dot(velocity, lineDir);

        if (speedAlongLine < minScalpelSpeed)
            return;

        //-----------------------------------------
        // 3. Validate blade angle
        //-----------------------------------------

        if (bladeForward != null)
        {
            float angleDot =
                Vector3.Dot(
                    bladeForward.forward,
                    lineDir
                );

            if (angleDot < requiredAngleDot)
                return;
        }

        //-----------------------------------------
        // 4. Prevent teleport cutting
        //-----------------------------------------

        normalizedT = Mathf.Min(
            normalizedT,
            targetProgress + maxProgressStep
        );

        //-----------------------------------------
        // 5. Monotonic progression
        //-----------------------------------------

        if (normalizedT > targetProgress)
        {
            targetProgress = normalizedT;

            // Optional:
            // Trigger haptics/audio here
        }
    }

    private void ApplyBlendShapes(float progress)
    {
        float slice = 1f / blendShapeCount;

        for (int i = 0; i < blendShapeCount; i++)
        {
            float sliceStart = i * slice;

            float weight =
                Mathf.Clamp01(
                    (progress - sliceStart) / slice
                ) * 100f;

            skinMesh.SetBlendShapeWeight(i, weight);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Scalpel"))
            return;

        isCutting = true;

        lastScalpelPos = scalpelTip.position;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Scalpel"))
            return;

        isCutting = false;
    }

    //-----------------------------------------
    // Debug Visualization
    //-----------------------------------------

    private void OnDrawGizmos()
    {
        if (!showDebug || cutStart == null || cutEnd == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(cutStart.position, cutEnd.position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(cutStart.position, 0.005f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(cutEnd.position, 0.005f);

        if (scalpelTip != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(scalpelTip.position, 0.004f);
        }
    }
}