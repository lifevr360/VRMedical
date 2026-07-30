using UnityEngine;

namespace IndianRailwayVR
{
    /// <summary>
    /// Fires Animator triggers from a UnityEvent (e.g. a Step's "On Step Begin").
    /// Attach to the object with the Animator (or assign one), then wire
    /// SetTrigger and type the trigger name (e.g. "TrackMove" / "TrainMove").
    /// </summary>
    public class AnimationTrigger : MonoBehaviour
    {
        [SerializeField] Animator m_Animator;

        void Reset()
        {
            // Auto-fill with the Animator on this object when the component is added.
            m_Animator = GetComponent<Animator>();
        }

        /// <summary>Fire an Animator trigger by name (wire this to On Step Begin).</summary>
        public void SetTrigger(string triggerName)
        {
            if (m_Animator == null)
            {
                Debug.LogWarning("[AnimationTrigger] No Animator assigned.");
                return;
            }
            if (string.IsNullOrEmpty(triggerName)) return;

            m_Animator.SetTrigger(triggerName);
            Debug.Log($"[AnimationTrigger] SetTrigger('{triggerName}') on '{m_Animator.name}'.");
        }

        /// <summary>Clear a trigger if you need to cancel/reset it.</summary>
        public void ResetTrigger(string triggerName)
        {
            if (m_Animator != null && !string.IsNullOrEmpty(triggerName))
                m_Animator.ResetTrigger(triggerName);
        }
    }
}
