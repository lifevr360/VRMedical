using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Spoken, step-by-step walkthrough of the fire extinguisher training.
/// Flow: welcome -> teleport to the pad -> meet the extinguishers -> foam: pin, nozzle, handle, technique,
/// Rack 1, Rack 2 -> switch to the CO2 extinguisher: pin, nozzle, handle, TV unit, bunk bed -> congratulations.
/// Each step plays its voice clip and blinks the thing to use (via Blink.isBlink). The clip is cut the moment
/// the user does it. Steps the user has already done are skipped, and if a fire is put out out of order the
/// guide simply moves on to the next one still burning. Finishing all fires plays the last clip at any time.
/// A clip that is not assigned is skipped silently, so the flow can be tested before the audio exists.
/// Use Tools > Fire Training > Set Up Guided Training to fill in the references and the clips.
/// </summary>
public class TrainingGuide : MonoBehaviour
{
    [Serializable]
    private class ExtinguisherParts
    {
        [Header("Parts")]
        public ExtinguisherLock pin;
        public Blink pinBlink;
        public XRBaseInteractable nozzle;
        public Blink nozzleBlink;
        public XRBaseInteractable handle;
        public Blink handleBlink;
        public XRBaseInteractable body;         // the extinguisher itself; grabbing it counts as picking it up
        public Blink bodyBlink;                 // blinks the whole extinguisher to point at it

        [Header("Voice clips")]
        public AudioClip pinClip;
        public AudioClip nozzleClip;
        public AudioClip handleClip;

        [NonSerialized] public bool nozzleGrabbed;
        [NonSerialized] public bool handleGrabbed;
        [NonSerialized] public bool bodyGrabbed;

        /// <summary>True once any part of this extinguisher has been grabbed.</summary>
        public bool PickedUp => bodyGrabbed || nozzleGrabbed || handleGrabbed || (pin != null && pin.HasBeenGrabbed);

        public void Hook()
        {
            if (nozzle != null) nozzle.selectEntered.AddListener(OnNozzleGrabbed);
            if (handle != null) handle.selectEntered.AddListener(OnHandleGrabbed);
            if (body != null) body.selectEntered.AddListener(OnBodyGrabbed);
        }

        public void Unhook()
        {
            if (nozzle != null) nozzle.selectEntered.RemoveListener(OnNozzleGrabbed);
            if (handle != null) handle.selectEntered.RemoveListener(OnHandleGrabbed);
            if (body != null) body.selectEntered.RemoveListener(OnBodyGrabbed);
        }

        private void OnNozzleGrabbed(SelectEnterEventArgs _) => nozzleGrabbed = true;
        private void OnHandleGrabbed(SelectEnterEventArgs _) => handleGrabbed = true;
        private void OnBodyGrabbed(SelectEnterEventArgs _) => bodyGrabbed = true;
    }

    [Serializable]
    private class FireTarget
    {
        public ExtinguishableFire fire;
        public Blink blink;                     // on the fire's Polished object
        public AudioClip clip;

        public bool IsBurning => fire != null && !fire.IsExtinguished;
    }

    [Header("Voice")]
    [SerializeField] private AudioSource voice;                             // created automatically if left empty
    [SerializeField, Min(0f)] private float startDelay = 1.5f;              // seconds before the welcome plays

    [Header("Reminders")]
    [SerializeField, Min(1f)] private float reminderDelaySeconds = 15f;     // silence before a reminder plays
    [SerializeField, Min(0)] private int maxRemindersPerStep = 2;

    [Header("Intro")]
    [SerializeField] private AudioClip welcomeClip;
    [SerializeField] private AudioClip teleportClip;
    [SerializeField] private AudioClip meetClip;
    [SerializeField] private Blink padBlink;                                // the teleport pad
    [SerializeField] private Transform padPoint;                            // where the user should stand
    [SerializeField, Min(0.1f)] private float arrivalRadius = 1f;           // metres from padPoint that counts as "on the pad"
    [SerializeField, Min(1)] private int extinguisherBlinkCount = 3;        // times the red extinguisher blinks at the end of the intro

    [Header("Movement hint")]
    [SerializeField] private AudioClip thumbstickClip;
    [SerializeField] private Blink thumbstickBlink;                         // the left controller's thumbstick
    [SerializeField, Min(0.5f)] private float walkAwayDistance = 1.5f;      // metres from the pad that counts as "already walking"

    [Header("Foam extinguisher (red)")]
    [SerializeField] private ExtinguisherParts foam;
    [SerializeField] private AudioClip techniqueClip;
    [SerializeField] private FireTarget[] foamTargets;                      // Rack 1, Rack 2

    [Header("Carbon dioxide extinguisher (black)")]
    [SerializeField] private AudioClip switchClip;
    [SerializeField] private ExtinguisherParts co2;
    [SerializeField] private FireTarget[] co2Targets;                       // TV unit, Bunk bed

    [Header("Shared clips")]
    [SerializeField] private AudioClip pinRemovedClip;
    [SerializeField] private AudioClip completeClip;

    [Header("Reminder clips")]
    [SerializeField] private AudioClip remindTeleport;
    [SerializeField] private AudioClip remindPin;
    [SerializeField] private AudioClip remindGrab;
    [SerializeField] private AudioClip remindAim;
    [SerializeField] private AudioClip remindSwitch;

    private readonly HashSet<Blink> blinking = new HashSet<Blink>();
    private readonly List<FireTarget> allTargets = new List<FireTarget>();
    private Camera headCamera;
    private bool finished;


    private void Start()
    {
        if (voice == null)
            voice = gameObject.AddComponent<AudioSource>();

        voice.playOnAwake = false;
        voice.loop = false;
        voice.spatialBlend = 0f;                // narration is not positional

        if (foamTargets != null) allTargets.AddRange(foamTargets);
        if (co2Targets != null) allTargets.AddRange(co2Targets);

        foam.Hook();
        co2.Hook();

        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        foam.Unhook();
        co2.Unhook();
    }

    private void Update()
    {
        if (!finished && AllFiresOut())
            Finish();
    }

    // The whole walkthrough, top to bottom.
    private IEnumerator Run()
    {
        yield return new WaitForSeconds(startDelay);
        yield return PlayFull(welcomeClip);

        // Move to the pad. The intro to the extinguishers only plays once the user is standing there.
        yield return Step(teleportClip, AtPad, remindTeleport, padBlink);
        yield return new WaitUntil(AtPad);

        // The red extinguisher blinks over the last moments of the intro ("we will begin with the red foam extinguisher").
        yield return PlayFullBlinking(meetClip, foam.PickedUp ? null : foam.bodyBlink, extinguisherBlinkCount);

        // Foam extinguisher: pin, nozzle, handle, then the first fires.
        yield return PinSteps(foam);
        yield return Step(foam.nozzleClip, () => foam.nozzleGrabbed, remindGrab, foam.nozzleBlink);
        yield return Step(foam.handleClip, () => foam.handleGrabbed, remindGrab, foam.handleBlink);
        if (Remaining(foamTargets) > 0)
        {
            // The extinguisher is in hand: show how to walk to the fire, then the technique.
            yield return Hint(thumbstickClip, WalkedAway, thumbstickBlink);
            yield return PlayFull(techniqueClip);
        }
        yield return Targets(foamTargets);

        // Carbon dioxide extinguisher for whatever is still burning.
        if (Remaining(co2Targets) > 0)
        {
            if (!co2.PickedUp)
                yield return Step(switchClip, () => co2.PickedUp, remindSwitch, co2.bodyBlink);

            yield return PinSteps(co2);
            yield return Step(co2.nozzleClip, () => co2.nozzleGrabbed, remindGrab, co2.nozzleBlink);
            yield return Step(co2.handleClip, () => co2.handleGrabbed, remindGrab, co2.handleBlink);
            yield return Targets(co2Targets);
        }
    }

    // Grab the pin (the voice is cut and the blinking stops), pull it out, then "Pin removed".
    private IEnumerator PinSteps(ExtinguisherParts parts)
    {
        if (parts.pin == null || parts.pin.IsPulled)
            yield break;

        yield return Step(parts.pinClip, () => parts.pin.HasBeenGrabbed || parts.pin.IsPulled, remindPin, parts.pinBlink);

        // Let go too early and the pin springs back, so keep reminding until it is out.
        yield return Step(null, () => parts.pin.IsPulled, remindPin);
        yield return PlayFull(pinRemovedClip);
    }

    // One fire at a time, in order, skipping any that are already out.
    private IEnumerator Targets(FireTarget[] targets)
    {
        if (targets == null)
            yield break;

        foreach (FireTarget target in targets)
            if (target.IsBurning)
                yield return Step(target.clip, () => !target.IsBurning, remindAim, target.blink);
    }

    // Plays the instruction, blinks the target and waits for the goal. The voice is cut the moment the goal
    // is reached. If the user is silent for Reminder Delay Seconds, the reminder clip plays (a few times at most).
    private IEnumerator Step(AudioClip clip, Func<bool> goalReached, AudioClip reminder, params Blink[] toBlink)
    {
        if (goalReached())
            yield break;

        SetBlink(true, toBlink);
        Play(clip);

        float quietFor = 0f;
        int reminders = 0;
        while (!goalReached())
        {
            if (voice.isPlaying)
            {
                quietFor = 0f;
            }
            else
            {
                quietFor += Time.deltaTime;
                if (reminders < maxRemindersPerStep && quietFor >= reminderDelaySeconds)
                {
                    Play(reminder);
                    reminders++;
                    quietFor = 0f;
                }
            }

            yield return null;
        }

        voice.Stop();
        SetBlink(false, toBlink);

        if (toBlink.Length > 0)
            yield return null;      // let Blink restore the materials before the next step starts blinking something else
    }

    // A short tip that never holds the flow up: plays the clip and blinks the target while it plays.
    // Skipped if the user has already done it, and cut short the moment they do.
    private IEnumerator Hint(AudioClip clip, Func<bool> alreadyDone, Blink toBlink)
    {
        if (alreadyDone())
            yield break;

        SetBlink(true, toBlink);
        Play(clip);

        float end = Time.time + (clip != null ? clip.length : 3f);
        while (Time.time < end && !alreadyDone())
            yield return null;

        voice.Stop();
        SetBlink(false, toBlink);
        yield return null;
    }

    private IEnumerator PlayFull(AudioClip clip)
    {
        if (clip == null)
            yield break;

        Play(clip);
        yield return new WaitForSeconds(clip.length);
    }

    // Plays the whole clip and blinks the target a few times over its last moments, so the blinking lines up
    // with the closing words. Takes exactly as long as the clip.
    private IEnumerator PlayFullBlinking(AudioClip clip, Blink target, int times)
    {
        float blinkTime = target != null ? BlinkTime(target, times) : 0f;
        float length = clip != null ? clip.length : 0f;

        Play(clip);
        if (length > blinkTime)
            yield return new WaitForSeconds(length - blinkTime);

        if (target == null)
            yield break;

        // Blink runs on/off cycles of 2 x blinkDuration. Stop after the last "on" phase, before the next one.
        SetBlink(true, target);
        yield return new WaitForSeconds(blinkTime);
        SetBlink(false, target);
        yield return null;
    }

    private static float BlinkTime(Blink blink, int times)
    {
        return Mathf.Max(0f, (times * 2f - 0.5f) * blink.blinkDuration);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
            return;

        voice.Stop();
        voice.clip = clip;
        voice.Play();
    }

    private void Finish()
    {
        finished = true;
        StopAllCoroutines();
        ClearBlinks();
        voice.Stop();
        Play(completeClip);
    }

    private bool AllFiresOut()
    {
        int fires = 0;
        foreach (FireTarget target in allTargets)
        {
            if (target.fire == null)
                continue;

            if (target.IsBurning)
                return false;
            fires++;
        }

        return fires > 0;
    }

    private static int Remaining(FireTarget[] targets)
    {
        int burning = 0;
        if (targets != null)
            foreach (FireTarget target in targets)
                if (target.IsBurning)
                    burning++;

        return burning;
    }

    // True when the user's head is over the teleport pad.
    private bool AtPad()
    {
        return DistanceFromPad() <= arrivalRadius;
    }

    // True once the user has walked (or teleported) away from the pad.
    private bool WalkedAway()
    {
        return DistanceFromPad() >= walkAwayDistance;
    }

    // Horizontal distance from the user's head to the pad. Very large if there is no camera or pad to measure.
    private float DistanceFromPad()
    {
        if (headCamera == null)
            headCamera = Camera.main;

        if (headCamera == null || padPoint == null)
            return float.MaxValue;

        Vector3 away = headCamera.transform.position - padPoint.position;
        away.y = 0f;
        return away.magnitude;
    }

    private void SetBlink(bool on, params Blink[] targets)
    {
        foreach (Blink blink in targets)
        {
            if (blink == null)
                continue;

            blink.isBlink = on;
            if (on) blinking.Add(blink);
            else blinking.Remove(blink);
        }
    }

    private void ClearBlinks()
    {
        foreach (Blink blink in blinking)
            if (blink != null)
                blink.isBlink = false;

        blinking.Clear();
    }
}
