using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.VRTemplate;
using Unity.XR.CoreUtils;

namespace IndianRailwayVR
{
    /// <summary>
    /// Scenario 2 - Technical Point Failure (interactive, ordered procedure).
    ///
    /// Plays a list of <see cref="Step"/>s one at a time. Each step:
    ///   1. Applies its "On Step Start" enable/disable list.
    ///   2. Starts blinking its highlight objects, plays its audio, and fires On Step Begin
    ///      (use this to trigger animations - e.g. the track or train animation).
    ///   3. Waits for its completion condition (button press / grab / snap / knob value /
    ///      timer / manual).
    ///   4. Stops blinking, fires On Step Completed, applies its "On Step End" list.
    ///   5. Waits the common delay, then moves to the next step.
    /// After the last step it fires <see cref="onProcedureCompleted"/>.
    /// </summary>
    public class ProcedureManager : MonoBehaviour
    {
        public enum CompletionMode
        {
            Timed,      // waits "Wait Seconds" (or the audio length if Wait Seconds is 0)
            Button,     // waits for a UI Button click
            Grab,       // waits for an XR Grab Interactable to be grabbed
            Snap,       // waits for a SnapToTarget to snap (its onSnapped event)
            KnobValue,  // waits for an XRKnob to reach a target value
            Manual      // waits for an external call to CompleteCurrentStep()
        }

        /// <summary>An object with a tick box. Ticked = enable, unticked = disable.</summary>
        [Serializable]
        public class ObjectToggle
        {
            public GameObject target;
            [Tooltip("Ticked = enable this object. Unticked = disable it.")]
            public bool enable;
        }

        [Serializable]
        public class Step
        {
            [Tooltip("Label for the Inspector only.")]
            public string stepName;

            [Header("1. Applied when the step begins")]
            public List<ObjectToggle> onStepStart = new List<ObjectToggle>();

            [Header("2. Runs while the step is active")]
            public AudioClip audioClip;
            [Tooltip("These objects' Blink scripts turn on for the duration of the step.")]
            public List<Blink> blinkObjects = new List<Blink>();
            [Tooltip("Fires when the step begins - wire animations here (e.g. play track / train animation).")]
            public UnityEvent onStepBegin;

            [Header("3. How this step is completed")]
            public CompletionMode completion = CompletionMode.Timed;

            [Tooltip("Timed mode: seconds to wait. 0 + an audio clip = wait for the audio to finish.")]
            public float waitSeconds = 0f;
            [Tooltip("Button mode: the UI button that completes this step.")]
            public Button button;
            [Tooltip("Grab mode: the grab interactable that completes this step when grabbed.")]
            public XRGrabInteractable grabInteractable;
            [Tooltip("Snap mode: the SnapToTarget that completes this step when it snaps.")]
            public SnapToTarget snapTarget;
            [Tooltip("KnobValue mode: the knob to watch.")]
            public XRKnob knob;
            [Tooltip("KnobValue mode: the value to reach (e.g. 0 to set the point, 1 to restore).")]
            [Range(0f, 1f)] public float knobReachValue = 0f;
            [Tooltip("KnobValue mode: how close to the target counts as reached.")]
            public float knobThreshold = 0.02f;

            [Header("4. Applied after the step completes")]
            public UnityEvent onStepCompleted;
            public List<ObjectToggle> onStepEnd = new List<ObjectToggle>();
        }

        [Header("Steps (played top to bottom)")]
        [SerializeField] List<Step> m_Steps = new List<Step>();

        [Header("Scene references")]
        [SerializeField] AudioSource m_AudioSource;
        [Tooltip("The XR Origin (rig) to teleport. Only needed if you use TeleportRig().")]
        [SerializeField] XROrigin m_XROrigin;

        [Header("Flow")]
        [SerializeField] bool m_PlayOnStart = true;
        [SerializeField] float m_StartDelay = 1f;
        [Tooltip("Pause (seconds) between one step finishing and the next starting.")]
        [SerializeField] float m_DelayBetweenSteps = 1f;

        [Header("Events")]
        public UnityEvent onProcedureStarted;
        public UnityEvent onProcedureCompleted;

        Coroutine m_ProcedureCo;
        Step m_CurrentStep;
        bool m_StepComplete;

        void Start()
        {
            if (m_PlayOnStart)
                StartProcedure();
        }

        /// <summary>Begin (or restart) the procedure from the first step.</summary>
        public void StartProcedure()
        {
            if (m_ProcedureCo != null)
                StopCoroutine(m_ProcedureCo);
            m_ProcedureCo = StartCoroutine(RunProcedure());
        }

        /// <summary>
        /// Force the current step to complete. Use for Manual steps, or call from an
        /// Animation Event at the end of a clip to advance when the animation finishes.
        /// </summary>
        public void CompleteCurrentStep() => m_StepComplete = true;

        /// <summary>
        /// Teleport the XR rig so the user stands at (and faces) the target transform.
        /// Place the target where the user should stand (floor level); its forward is the
        /// direction the user will face. Wire this to a Button or a step's On Step Begin.
        /// </summary>
        public void TeleportRig(Transform target)
        {
            if (target == null || m_XROrigin == null || m_XROrigin.Camera == null)
                return;

            var rig = m_XROrigin.transform;
            var cam = m_XROrigin.Camera.transform;

            // 1. Yaw: rotate the rig (around the camera) so the user faces the target's forward.
            var targetFwd = target.forward;
            targetFwd.y = 0f;
            if (targetFwd.sqrMagnitude > 0.0001f)
            {
                var camFwd = cam.forward;
                camFwd.y = 0f;
                float deltaYaw = Vector3.SignedAngle(camFwd, targetFwd, Vector3.up);
                m_XROrigin.RotateAroundCameraUsingOriginUp(deltaYaw);
            }

            // 2. Position: move the rig so the camera's ground position sits over the target,
            //    keeping the user's real head height above the target's floor level.
            var camOffset = cam.position - rig.position;
            camOffset.y = 0f;
            var destination = target.position - camOffset;
            destination.y = target.position.y;
            rig.position = destination;
        }

        IEnumerator RunProcedure()
        {
            onProcedureStarted?.Invoke();

            if (m_StartDelay > 0f)
                yield return new WaitForSeconds(m_StartDelay);

            for (int i = 0; i < m_Steps.Count; i++)
            {
                Debug.Log($"[Procedure] Step {i + 1}/{m_Steps.Count} started: " +
                          $"{(string.IsNullOrEmpty(m_Steps[i].stepName) ? "(unnamed)" : m_Steps[i].stepName)}");
                yield return RunStep(m_Steps[i]);

                if (m_DelayBetweenSteps > 0f && i < m_Steps.Count - 1)
                    yield return new WaitForSeconds(m_DelayBetweenSteps);
            }

            onProcedureCompleted?.Invoke();
        }

        IEnumerator RunStep(Step step)
        {
            m_CurrentStep = step;

            // 1. Enable/disable objects for this step.
            ApplyToggles(step.onStepStart);

            // 2. Blink + audio + begin event (trigger animations here).
            SetBlink(step.blinkObjects, true);
            step.onStepBegin?.Invoke();

            if (step.audioClip != null && m_AudioSource != null)
            {
                m_AudioSource.Stop();
                m_AudioSource.clip = step.audioClip;
                m_AudioSource.Play();
            }

            // 3. Wait for the completion condition.
            if (step.completion == CompletionMode.Timed)
            {
                float d = step.waitSeconds > 0f
                    ? step.waitSeconds
                    : (step.audioClip != null ? step.audioClip.length : 0f);
                if (d > 0f)
                    yield return new WaitForSeconds(d);
            }
            else
            {
                m_StepComplete = false;
                SetupCompletion(step);
                yield return new WaitUntil(() => m_StepComplete);
                TeardownCompletion(step);
            }

            // 4. Stop blink, fire completed event, apply end toggles.
            SetBlink(step.blinkObjects, false);
            step.onStepCompleted?.Invoke();
            ApplyToggles(step.onStepEnd);
        }

        // ---------- completion wiring ----------

        void SetupCompletion(Step step)
        {
            switch (step.completion)
            {
                case CompletionMode.Button:
                    if (step.button != null) step.button.onClick.AddListener(MarkComplete);
                    break;
                case CompletionMode.Grab:
                    if (step.grabInteractable != null) step.grabInteractable.selectEntered.AddListener(OnGrabbed);
                    break;
                case CompletionMode.Snap:
                    if (step.snapTarget != null)
                    {
                        if (step.snapTarget.IsSnapped) MarkComplete();     // already snapped
                        else step.snapTarget.onSnapped.AddListener(MarkComplete);
                    }
                    break;
                case CompletionMode.KnobValue:
                    if (step.knob != null)
                    {
                        step.knob.onValueChange.AddListener(OnKnobChanged);
                        if (KnobReached(step, step.knob.value)) MarkComplete();  // already at target
                    }
                    break;
                case CompletionMode.Manual:
                    // completed only via CompleteCurrentStep()
                    break;
            }
        }

        void TeardownCompletion(Step step)
        {
            switch (step.completion)
            {
                case CompletionMode.Button:
                    if (step.button != null) step.button.onClick.RemoveListener(MarkComplete);
                    break;
                case CompletionMode.Grab:
                    if (step.grabInteractable != null) step.grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                    break;
                case CompletionMode.Snap:
                    if (step.snapTarget != null) step.snapTarget.onSnapped.RemoveListener(MarkComplete);
                    break;
                case CompletionMode.KnobValue:
                    if (step.knob != null) step.knob.onValueChange.RemoveListener(OnKnobChanged);
                    break;
            }
        }

        void MarkComplete() => m_StepComplete = true;
        void OnGrabbed(SelectEnterEventArgs _) => MarkComplete();

        void OnKnobChanged(float v)
        {
            if (m_CurrentStep != null && KnobReached(m_CurrentStep, v))
                MarkComplete();
        }

        static bool KnobReached(Step step, float v)
        {
            // Reaching a low target (e.g. 0) = value at/below it; a high target (e.g. 1) = value at/above it.
            return step.knobReachValue <= 0.5f
                ? v <= step.knobReachValue + step.knobThreshold
                : v >= step.knobReachValue - step.knobThreshold;
        }

        // ---------- helpers ----------

        void ApplyToggles(List<ObjectToggle> toggles)
        {
            if (toggles == null) return;
            foreach (var t in toggles)
                if (t != null && t.target != null)
                    t.target.SetActive(t.enable);
        }

        void SetBlink(List<Blink> blinkObjects, bool value)
        {
            if (blinkObjects == null) return;
            foreach (var b in blinkObjects)
                if (b != null)
                    b.isBlink = value;
        }
    }
}
