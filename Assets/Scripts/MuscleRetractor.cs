using UnityEngine;

public class MuscleRetractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer muscleMesh;
    [SerializeField] private int blendShapeIndex = 0;

    [Header("Push Settings")]
    [SerializeField] private float pushAxisSign = 1f;          // tip Z INCREASES on push -> +1
    [SerializeField] private float fullPushDistance = 0.08f;   // metres of push for blend shape 100

    [Header("Feel")]
    [SerializeField] private float followSpeed = 12f;
    [SerializeField] private float returnSpeed = 4f;

    [Header("Exit Tolerance")]
    // how long the tip must be OUT before we count it as a real release
    [SerializeField] private float releaseGraceTime = 0.15f;

    private Transform tool;
    private bool startCaptured = false;
    private float contactStartZ;
    private float timeSinceLastTouch = 999f;

    private float targetValue;
    private float currentValue;

    public float BlendValue => currentValue;

    private void OnTriggerEnter(Collider other) { RegisterTouch(other); }
    private void OnTriggerStay(Collider other) { RegisterTouch(other); }

    private void RegisterTouch(Collider other)
    {
        if (!other.CompareTag("Retractor"))
            return;

        tool = other.transform;
        timeSinceLastTouch = 0f;   // we are touching THIS frame

        // capture start position exactly once, and never again until a real release
        if (!startCaptured)
        {
            contactStartZ = tool.position.z;
            startCaptured = true;
        }
    }

    private void Update()
    {
        // count time since the tip was last seen inside the trigger
        timeSinceLastTouch += Time.deltaTime;

        bool reallyInContact = timeSinceLastTouch < releaseGraceTime;

        if (reallyInContact && tool != null && startCaptured)
        {
            float delta = (tool.position.z - contactStartZ) * pushAxisSign;

            if (delta > 0f)
                targetValue = Mathf.Clamp01(delta / fullPushDistance) * 100f;
        }
        else
        {
            // real release -> reset everything, ease muscle back
            targetValue = 0f;
            startCaptured = false;
            tool = null;
        }

        float speed = (targetValue > currentValue) ? followSpeed : returnSpeed;
        currentValue = Mathf.MoveTowards(currentValue, targetValue,
                                         speed * 100f * Time.deltaTime);

        muscleMesh.SetBlendShapeWeight(blendShapeIndex, currentValue);

        Debug.Log("target: " + targetValue + "  blendWeight: " + currentValue + "  startZ: " + contactStartZ);
    }
}