using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireExtinguisherController : MonoBehaviour
{
    private ParticleSystem part;
    private List<ParticleCollisionEvent> collisionEvents;
    private string extinguisherTag; 
    private float extinguishRate = 0.8f;
    private float emissionRate = 0.6f;
    private Vector3 previousScale;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("I am alive");
        part = GetComponent<ParticleSystem>();
        extinguisherTag = this.transform.tag;
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    void OnParticleCollision(GameObject other)
    {
        Debug.Log(other.name + " collided with");
        int numCollisionEvents = part.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            if (other.CompareTag("Fire A") || other.CompareTag("Fire ABC") || other.CompareTag("Fire BC"))
            {
                if (CanExtinguish(other.tag))
                {
                    Debug.Log("Extinguishing fire...");
                    ReduceFireSizeAndEmission(other.transform, numCollisionEvents);
                }
                else
                {
                    Debug.Log("Cannot extinguish this fire type.");
                }
            }
        }
    }

    private bool CanExtinguish(string fireTag)
    {
        Debug.Log($"My tag is: {fireTag} and my substring is: { fireTag.Substring(fireTag.Length - 1)}");
        Debug.Log($"My tag is {extinguisherTag} and my substring is {extinguisherTag.Substring(extinguisherTag.Length - 1)}");
        return fireTag.Substring(fireTag.Length - 1) == extinguisherTag.Substring(extinguisherTag.Length - 1);
    }

    private void ReduceFireSizeAndEmission(Transform fireTransform, int numCollisions)
    {
        previousScale = fireTransform.localScale;
        foreach (ParticleSystem ps in fireTransform.GetComponentsInChildren<ParticleSystem>())
        {
            // Reducir el tamaño del sistema de partículas
            Vector3 newScale = ps.transform.localScale - Vector3.one * extinguishRate * Time.deltaTime / numCollisions;
            ps.transform.localScale = newScale;

            // Reducir la emisión
            var emission = ps.emission;
            var emissionRateOverTime = emission.rateOverTime.constant;
            emissionRateOverTime = Mathf.Max(0, emissionRateOverTime - emissionRate * Time.deltaTime / numCollisions);
            var emissionRateOverTimeCurve = new ParticleSystem.MinMaxCurve(emissionRateOverTime);
            emission.rateOverTime = emissionRateOverTimeCurve;

            // Reducir el tamaño del shape
            var shape = ps.shape;
            shape.scale = new Vector3(
                Mathf.Max(0, shape.scale.x - extinguishRate * Time.deltaTime / numCollisions),
                Mathf.Max(0, shape.scale.y - extinguishRate * Time.deltaTime / numCollisions),
                Mathf.Max(0, shape.scale.z - extinguishRate * Time.deltaTime / numCollisions)
            );
            
            if (newScale.x <= 0 || newScale.y <= 0 || newScale.z <= 0)
            {
                Debug.Log("Fire completely extinguished!");
                fireTransform.gameObject.SetActive(false);
                return;
            }
        }
    }
}
