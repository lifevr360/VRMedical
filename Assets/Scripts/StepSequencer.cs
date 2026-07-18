using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StepSequencer : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Tooltip("Label for the Inspector, e.g. 'Step 1 - Scalpel'")]
        public string label;

        [Header("Blink Targets")]
        [Tooltip("Blinks while step is active AND pauseOnGrab is NOT held. Typical: the tool/pick.")]
        public GameObject[] pickBlink;

        [Tooltip("Blinks throughout the step regardless of grab state. Typical: drop ghosts.")]
        public GameObject[] guideBlink;

        [Header("Grab Pause (optional)")]
        [Tooltip("If set, pickBlink pauses while this XR Grab is held.")]
        public XRGrabInteractable pauseOnGrab;

        [Header("Done Condition (fill exactly one)")]
        public BlendShapeTrigger doneOnBlendShape;
        public MuscleRetractor   doneOnMuscle;
        [Range(0f, 100f)] public float muscleThreshold = 95f;
        public BonePusher        doneOnBone;
        [Range(0f, 1f)]   public float boneThreshold = 0.95f;
        public SnapToTarget      doneOnSnap;
        public DrillSequence     doneOnDrill;
    }

    [SerializeField] private Step[] steps;

    private Blink[][] pickBlinks;
    private Blink[][] guideBlinks;
    private int currentIndex = 0;
    private bool finished = false;

    private void Awake()
    {
        pickBlinks  = new Blink[steps.Length][];
        guideBlinks = new Blink[steps.Length][];
        for (int s = 0; s < steps.Length; s++)
        {
            pickBlinks[s]  = CacheBlinks(steps[s].pickBlink);
            guideBlinks[s] = CacheBlinks(steps[s].guideBlink);
        }
    }

    private static Blink[] CacheBlinks(GameObject[] gos)
    {
        if (gos == null) return new Blink[0];
        Blink[] arr = new Blink[gos.Length];
        for (int i = 0; i < gos.Length; i++)
            if (gos[i] != null) arr[i] = gos[i].GetComponent<Blink>();
        return arr;
    }

    private void Update()
    {
        if (finished || steps == null || steps.Length == 0) return;

        DriveBlinksForCurrentStep();

        if (IsCurrentStepDone())
            Advance();
    }

    private void DriveBlinksForCurrentStep()
    {
        Step st = steps[currentIndex];
        bool held = st.pauseOnGrab != null && st.pauseOnGrab.isSelected;

        SetBlinks(pickBlinks[currentIndex],  !held);   // pauses on grab
        SetBlinks(guideBlinks[currentIndex], true);    // always blinks during step
    }

    private static void SetBlinks(Blink[] blinks, bool on)
    {
        for (int i = 0; i < blinks.Length; i++)
            if (blinks[i] != null) blinks[i].isBlink = on;
    }

    private bool IsCurrentStepDone()
    {
        Step st = steps[currentIndex];
        if (st.doneOnBlendShape != null) return st.doneOnBlendShape.HasTriggered;
        if (st.doneOnMuscle     != null) return st.doneOnMuscle.BlendValue   >= st.muscleThreshold;
        if (st.doneOnBone       != null) return st.doneOnBone.Progress       >= st.boneThreshold;
        if (st.doneOnSnap       != null) return st.doneOnSnap.IsSnapped;
        if (st.doneOnDrill      != null) return st.doneOnDrill.IsDone;
        return false;   // misconfigured step: never advance
    }

    private void Advance()
    {
        // Stop both blink groups on the step we're leaving.
        SetBlinks(pickBlinks[currentIndex],  false);
        SetBlinks(guideBlinks[currentIndex], false);

        currentIndex++;
        if (currentIndex >= steps.Length) finished = true;
    }
}
