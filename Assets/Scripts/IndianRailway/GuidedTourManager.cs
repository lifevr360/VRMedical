using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace IndianRailwayVR
{
    /// <summary>
    /// Scenario 1 - Equipment Familiarisation (guided tour).
    ///
    /// Plays each <see cref="TourStep"/> in order, fully automatically:
    ///   1. Applies the step's "On Step Start" enable/disable list
    ///      (this is where you switch on that step's separate info panel).
    ///   2. In parallel: plays the step audio and starts the Blink on the listed objects.
    ///   3. Waits until the audio finishes. If there is no audio it waits
    ///      "Wait Time If No Audio" seconds (0 = go straight to the next step).
    ///   4. Stops the Blink, then applies the step's "On Step End" enable/disable list.
    ///   5. After the last step, fires <see cref="onTourCompleted"/>
    ///      (wire this to EquipmentInfoManager.EnableInteraction()).
    /// </summary>
    public class GuidedTourManager : MonoBehaviour
    {
        /// <summary>A single object with a tick box. Ticked = enable, unticked = disable.</summary>
        [Serializable]
        public class ObjectToggle
        {
            public GameObject target;
            [Tooltip("Ticked = this object will be enabled. Unticked = it will be disabled.")]
            public bool enable;
        }

        [Serializable]
        public class TourStep
        {
            [Tooltip("Label for you in the Inspector - not shown to the user.")]
            public string stepName;

            [Header("1. Applied the moment this step begins")]
            [Tooltip("Enable the separate info panel for this step here (tick it), " +
                     "and switch off the previous one (untick it).")]
            public List<ObjectToggle> onStepStart = new List<ObjectToggle>();

            [Header("2. Runs in parallel with the step")]
            public AudioClip audioClip;
            [Tooltip("These objects' Blink scripts turn on while the step is active, " +
                     "and turn off when it ends.")]
            public List<Blink> blinkObjects = new List<Blink>();

            [Tooltip("Used ONLY when no audio clip is assigned. " +
                     "0 = skip straight to the next step, 1 = wait 1 second, etc.")]
            public float waitTimeIfNoAudio = 0f;

            [Header("3. Applied AFTER the audio (or wait) finishes")]
            public List<ObjectToggle> onStepEnd = new List<ObjectToggle>();
        }

        [Header("Steps (played top to bottom)")]
        [SerializeField] List<TourStep> m_Steps = new List<TourStep>();

        [Header("Scene references")]
        [SerializeField] AudioSource m_AudioSource;

        [Header("Flow")]
        [Tooltip("Start the tour automatically when the scene loads.")]
        [SerializeField] bool m_PlayOnStart = true;
        [Tooltip("Small delay before the first step so the user settles into the room.")]
        [SerializeField] float m_StartDelay = 1.5f;
        [Tooltip("Delay (seconds) after a step finishes before the next step begins. " +
                 "Applies between every step (not after the last one).")]
        [SerializeField] float m_DelayBetweenSteps = 1f;

        [Header("Events")]
        public UnityEvent onTourStarted;
        public UnityEvent onTourCompleted;   // <-- wire this to EquipmentInfoManager.EnableInteraction()

        Coroutine m_TourCo;

        void Start()
        {
            if (m_PlayOnStart)
                StartTour();
        }

        /// <summary>Begin (or restart) the guided tour from the first step.</summary>
        public void StartTour()
        {
            if (m_TourCo != null)
                StopCoroutine(m_TourCo);
            m_TourCo = StartCoroutine(RunTour());
        }

        IEnumerator RunTour()
        {
            onTourStarted?.Invoke();

            if (m_StartDelay > 0f)
                yield return new WaitForSeconds(m_StartDelay);

            for (int i = 0; i < m_Steps.Count; i++)
            {
                yield return RunStep(m_Steps[i]);

                // Wait the common delay before starting the next step (skip after the last one).
                if (m_DelayBetweenSteps > 0f && i < m_Steps.Count - 1)
                    yield return new WaitForSeconds(m_DelayBetweenSteps);
            }

            onTourCompleted?.Invoke();
        }

        IEnumerator RunStep(TourStep step)
        {
            // 1. Apply the "on start" enable/disable list (turns on this step's info panel).
            ApplyToggles(step.onStepStart);

            // 2. Start blinking + play audio in parallel.
            SetBlink(step.blinkObjects, true);

            if (step.audioClip != null && m_AudioSource != null)
            {
                m_AudioSource.Stop();
                m_AudioSource.clip = step.audioClip;
                m_AudioSource.Play();

                // 3a. Do NOT advance until the audio has finished.
                yield return new WaitForSeconds(step.audioClip.length);
            }
            else if (step.waitTimeIfNoAudio > 0f)
            {
                // 3b. No audio: wait the configured time.
                yield return new WaitForSeconds(step.waitTimeIfNoAudio);
            }
            else
            {
                // 3c. No audio and wait time 0: skip straight on (one frame).
                yield return null;
            }

            // 4. Stop blinking, then apply the "on end" enable/disable list.
            SetBlink(step.blinkObjects, false);
            ApplyToggles(step.onStepEnd);
        }

        void ApplyToggles(List<ObjectToggle> toggles)
        {
            if (toggles == null) return;
            foreach (var t in toggles)
            {
                if (t != null && t.target != null)
                    t.target.SetActive(t.enable);
            }
        }

        void SetBlink(List<Blink> blinkObjects, bool value)
        {
            if (blinkObjects == null) return;
            foreach (var b in blinkObjects)
            {
                if (b != null)
                    b.isBlink = value;
            }
        }
    }
}
