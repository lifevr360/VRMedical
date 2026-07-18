using UnityEngine;

public class BlendShapeTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkinnedMeshRenderer skinMesh;

    [Header("Blend Shape Settings")]
    [SerializeField] private int blendShapeIndex;

    [SerializeField] private float blendShapeValue = 100f;

    [Header("Options")]
    [SerializeField] private bool triggerOnlyOnce = true;

    public GameObject SkinTearColliders;

    private bool alreadyTriggered = false;

    public bool HasTriggered => alreadyTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Scalpel"))
            return;

        if (triggerOnlyOnce && alreadyTriggered)
            return;


        skinMesh.SetBlendShapeWeight(
            blendShapeIndex,
            blendShapeValue
        );

        if(blendShapeIndex == 5)
        {
            skinMesh.SetBlendShapeWeight(6,75f);
            SkinTearColliders.SetActive(false);
        }

        alreadyTriggered = true;
    }
}