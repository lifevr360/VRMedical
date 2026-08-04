using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public class TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public TransformData(Vector3 pos, Quaternion rot, Vector3 scl)
        {
            position = pos;
            rotation = rot;
            scale = scl;
        }
    }

    [Tooltip("Assign all the models (muscles, bones, organs) to this list.")]
    public List<GameObject> objectsToReset = new List<GameObject>();

    public List<GameObject> objectsToEnable = new List<GameObject>();
    public List<GameObject> toggleLabels = new List<GameObject>();
    private Dictionary<GameObject, TransformData> originalTransforms = new Dictionary<GameObject, TransformData>();

    void Start()
    {
        foreach (GameObject obj in objectsToReset)
        {
            if (obj != null)
            {
                originalTransforms[obj] = new TransformData(
                    obj.transform.position,
                    obj.transform.rotation,
                    obj.transform.localScale
                );
            }
        }

        foreach (GameObject obj in toggleLabels)
        {
            obj.SetActive(false);
        }
    }

    // Call this function on button click
    public void ResetModels()
    {
        foreach (GameObject obj in objectsToReset)
        {
            if (obj != null && originalTransforms.ContainsKey(obj))
            {
                TransformData data = originalTransforms[obj];
                obj.transform.position = data.position;
                obj.transform.rotation = data.rotation;
                obj.transform.localScale = data.scale;
            }
        }
    }

    public void EnableObject(GameObject target)
    {
        foreach (var obj in objectsToEnable)
        {
            obj.SetActive(obj == target);
        }
    }

    public void ToggleLabels()
    {
        foreach (GameObject obj in toggleLabels)
        {
            obj.SetActive(!obj.activeSelf);
        }
    }
}
