using System.Collections;
using UnityEngine;

namespace IndianRailwayVR
{
    /// <summary>
    /// Fades a full-screen black overlay in/out. Put one in every scene.
    /// The overlay should be a black Image on a World-Space Canvas parented to the XR
    /// camera (so it always covers the view in the headset), with a CanvasGroup.
    ///
    /// - Fades IN automatically on Start (so a scene "appears" from black).
    /// - SceneLoader calls FadeOut() before loading the next scene.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_Group;
        [Tooltip("Seconds for a full fade.")]
        [SerializeField] float m_Duration = 0.4f;
        [Tooltip("Start the scene fully black and fade in.")]
        [SerializeField] bool m_FadeInOnStart = true;

        public float Duration => m_Duration;

        Coroutine m_Co;

        void Reset() => m_Group = GetComponent<CanvasGroup>();

        void Awake()
        {
            if (m_Group == null) m_Group = GetComponent<CanvasGroup>();
        }

        void Start()
        {
            if (m_FadeInOnStart)
            {
                SetAlpha(1f);   // begin black
                FadeIn();       // fade to clear
            }
            else
            {
                SetAlpha(0f);
            }
        }

        /// <summary>Fade to clear (transparent).</summary>
        public void FadeIn() => StartFade(0f);

        /// <summary>Fade to black.</summary>
        public void FadeOut() => StartFade(1f);

        void StartFade(float target)
        {
            if (m_Co != null) StopCoroutine(m_Co);
            m_Co = StartCoroutine(FadeRoutine(target));
        }

        IEnumerator FadeRoutine(float target)
        {
            if (m_Group == null) yield break;

            float start = m_Group.alpha;
            float t = 0f;
            while (t < m_Duration)
            {
                t += Time.unscaledDeltaTime;   // unscaled so a paused timescale still fades
                SetAlpha(Mathf.Lerp(start, target, t / m_Duration));
                yield return null;
            }
            SetAlpha(target);
        }

        void SetAlpha(float a)
        {
            if (m_Group == null) return;
            m_Group.alpha = a;
            m_Group.blocksRaycasts = a > 0.5f;   // block UI clicks while the screen is dark
        }
    }
}
