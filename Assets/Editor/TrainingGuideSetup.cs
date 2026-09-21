using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// One-click setup for the guided training. Run it from Tools > Fire Training > Set Up Guided Training with
/// the FireExtinguisher scene open, then save the scene.
/// It puts a Blink (with the StepBlink material) on every object the guide points at, creates the
/// TrainingGuide under Scripts, and fills in every reference and every voice clip found in Assets/Audios.
/// It is safe to run again, for example after adding more audio files.
/// </summary>
public static class TrainingGuideSetup
{
    private const string BlinkMaterialPath = "Assets/Materials/FireExtinguisher/StepBlink.mat";
    private const string AudioFolder = "Assets/Audios";

    // Guide field -> audio file name (without extension) in Assets/Audios.
    private static readonly (string property, string file)[] Clips =
    {
        ("welcomeClip", "Welcome_to_Fire_Extinguisher_Training"),
        ("teleportClip", "First_lets_move_closer_to_the_fire"),
        ("meetClip", "Well_done_You_are_now_next_to"),
        ("foam.pinClip", "Look_at_the_safety_pin_on_top"),
        ("pinRemovedClip", "Pin_removed_Well_done"),
        ("foam.nozzleClip", "Now_use_one_hand_to_grab_the"),
        ("foam.handleClip", "Now_use_your_other_hand_to_grab"),
        ("techniqueClip", "You_are_ready_to_fight_the_fire"),
        ("foamTargets.Array.data[0].clip", "Start_with_the_first_rack_It_is"),
        ("foamTargets.Array.data[1].clip", "Excellent_that_rack_is_out_Now_the"),
        ("switchClip", "Well_done_Both_racks_are_out_For"),
        ("co2.pinClip", "Just_like_before_start_with_the_safety"),
        ("co2.nozzleClip", "Now_with_one_hand_grab_the"),
        ("co2.handleClip", "Then_grab_the_handle_on_top_with"),
        ("co2Targets.Array.data[0].clip", "Now_the_TV_unit_It_is_blinking"),
        ("co2Targets.Array.data[1].clip", "Excellent_One_fire_left_the_bunk_bed"),
        ("completeClip", "Congratulations_You_have_successfully_put_out"),
        ("remindTeleport", "Push_a_thumbstick_forward_aim_at_the"),
        ("remindPin", "Grab_the_blinking_safety_pin_and_pull"),
        ("remindGrab", "Grab_the_blinking_nozzle_with_one_hand"),
        ("remindAim", "Keep_the_handle_pressed_and_aim_at"),
        ("remindSwitch", "Reach_for_the_blinking_black_extinguisher"),
        ("thumbstickClip", "Now_lets_get_closer_to_the_fire"),
    };

    private static int blinksSetUp;

    [MenuItem("Tools/Fire Training/Set Up Guided Training")]
    private static void SetUp()
    {
        Material blinkMaterial = AssetDatabase.LoadAssetAtPath<Material>(BlinkMaterialPath);
        if (blinkMaterial == null)
        {
            Debug.LogError("Guided training setup: material not found at " + BlinkMaterialPath);
            return;
        }

        // ---- find everything first, and stop without changing anything if something is missing ----
        var missing = new List<string>();

        Transform interactables = FindRoot("Interactables");
        if (interactables == null) missing.Add("Interactables");
        Transform foamRoot = Child(interactables, "FoamFireExtinguisher", missing);
        Transform co2Root = Child(interactables, "CarbonDioxideFireExtinguisher", missing);
        Transform room = Child(interactables, "Room", missing);
        Transform scriptsRoot = FindRoot("Scripts");
        if (scriptsRoot == null) missing.Add("Scripts");

        Transform foamNozzle = Child(foamRoot, "fire_extinguisher.006", missing);
        Transform foamHandle = Child(foamRoot, "HandleHinge", missing);
        Transform co2Nozzle = Child(co2Root, "pCylinder24.002", missing);
        Transform co2Handle = Child(co2Root, "HandleHinge", missing);

        ExtinguisherLock foamPin = foamRoot != null ? foamRoot.GetComponentInChildren<ExtinguisherLock>(true) : null;
        ExtinguisherLock co2Pin = co2Root != null ? co2Root.GetComponentInChildren<ExtinguisherLock>(true) : null;
        if (foamPin == null) missing.Add("Extinguisher Lock on the foam pin");
        if (co2Pin == null) missing.Add("Extinguisher Lock on the CO2 pin");

        TeleportationAnchor pad = Object.FindObjectOfType<TeleportationAnchor>(true);
        if (pad == null) missing.Add("a Teleportation Anchor (the teleport pad)");

        // Optional: the walking tip still plays without it, the thumbstick just will not blink.
        Transform thumbstick = FindLeftThumbstick();
        if (thumbstick == null)
            Debug.LogWarning("Guided training setup: the left controller's ThumbStick was not found, so it will not blink.");

        Transform rack1 = Child(room, "Rack_1", missing);
        Transform rack2 = Child(room, "Rack_2", missing);
        Transform tv = Child(room, "TVUnit", missing);
        Transform bunk = Child(room, "BunkBed", missing);
        Transform rack1Polished = Child(rack1, "Rack Polished", missing);
        Transform rack2Polished = Child(rack2, "Rack Polished", missing);
        Transform tvPolished = Child(tv, "TVUnit Polished", missing);
        Transform bunkPolished = Child(bunk, "BunkBed Polished", missing);
        ExtinguishableFire fire1 = FireOn(rack1, missing);
        ExtinguishableFire fire2 = FireOn(rack2, missing);
        ExtinguishableFire fire3 = FireOn(tv, missing);
        ExtinguishableFire fire4 = FireOn(bunk, missing);

        if (missing.Count > 0)
        {
            Debug.LogError("Guided training setup stopped, nothing was changed. Not found: " + string.Join(", ", missing));
            return;
        }

        // ---- Blink on everything the guide points at ----
        blinksSetUp = 0;
        Blink padBlink = EnsureBlink(pad.gameObject, blinkMaterial);
        Blink foamBodyBlink = EnsureBlink(foamRoot.gameObject, blinkMaterial);
        Blink thumbstickBlink = thumbstick != null ? EnsureBlink(thumbstick.gameObject, blinkMaterial) : null;
        Blink foamPinBlink = EnsureBlink(foamPin.gameObject, blinkMaterial);
        Blink foamNozzleBlink = EnsureBlink(foamNozzle.gameObject, blinkMaterial);
        Blink foamHandleBlink = EnsureBlink(foamHandle.gameObject, blinkMaterial);
        Blink co2PinBlink = EnsureBlink(co2Pin.gameObject, blinkMaterial);
        Blink co2NozzleBlink = EnsureBlink(co2Nozzle.gameObject, blinkMaterial);
        Blink co2HandleBlink = EnsureBlink(co2Handle.gameObject, blinkMaterial);
        Blink co2BodyBlink = EnsureBlink(co2Root.gameObject, blinkMaterial);
        Blink blink1 = EnsureBlink(rack1Polished.gameObject, blinkMaterial);
        Blink blink2 = EnsureBlink(rack2Polished.gameObject, blinkMaterial);
        Blink blink3 = EnsureBlink(tvPolished.gameObject, blinkMaterial);
        Blink blink4 = EnsureBlink(bunkPolished.gameObject, blinkMaterial);

        // ---- the guide itself ----
        TrainingGuide guide = Object.FindObjectOfType<TrainingGuide>(true);
        if (guide == null)
        {
            var go = new GameObject("TrainingGuide");
            Undo.RegisterCreatedObjectUndo(go, "Create Training Guide");
            go.transform.SetParent(scriptsRoot, false);
            guide = Undo.AddComponent<TrainingGuide>(go);
        }

        var so = new SerializedObject(guide);
        Set(so, "padBlink", padBlink);
        Set(so, "padPoint", pad.teleportAnchorTransform != null ? pad.teleportAnchorTransform : pad.transform);

        if (thumbstickBlink != null)
            Set(so, "thumbstickBlink", thumbstickBlink);

        SetParts(so, "foam", foamPin, foamPinBlink, foamNozzle, foamNozzleBlink, foamHandle, foamHandleBlink, foamRoot, foamBodyBlink);
        SetParts(so, "co2", co2Pin, co2PinBlink, co2Nozzle, co2NozzleBlink, co2Handle, co2HandleBlink, co2Root, co2BodyBlink);

        SetTargets(so, "foamTargets", (fire1, blink1), (fire2, blink2));
        SetTargets(so, "co2Targets", (fire3, blink3), (fire4, blink4));

        // ---- voice clips ----
        var found = new Dictionary<string, AudioClip>();
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            found[Path.GetFileNameWithoutExtension(path).ToLowerInvariant()] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        var noAudio = new List<string>();
        foreach ((string property, string file) in Clips)
        {
            if (found.TryGetValue(file.ToLowerInvariant(), out AudioClip clip)) Set(so, property, clip);
            else noAudio.Add(file);
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(guide);
        EditorSceneManager.MarkSceneDirty(guide.gameObject.scene);
        Selection.activeObject = guide.gameObject;

        Debug.Log("Guided training set up on '" + guide.name + "'. Blink set up on " + blinksSetUp + " objects. Voice clips assigned: "
                  + (Clips.Length - noAudio.Count) + " of " + Clips.Length + "."
                  + (noAudio.Count > 0 ? " Not found in " + AudioFolder + " yet: " + string.Join(", ", noAudio) + "." : "")
                  + " Now save the scene (Ctrl+S).", guide);
    }

    private static Transform FindRoot(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name)
                return root.transform;

        return null;
    }

    private static Transform Child(Transform parent, string name, List<string> missing)
    {
        Transform child = parent != null ? parent.Find(name) : null;
        if (child == null)
            missing.Add(name + (parent != null ? " (under " + parent.name + ")" : ""));

        return child;
    }

    private static ExtinguishableFire FireOn(Transform obj, List<string> missing)
    {
        ExtinguishableFire fire = obj != null ? obj.GetComponentInChildren<ExtinguishableFire>(true) : null;
        if (obj != null && fire == null)
            missing.Add("Extinguishable Fire on " + obj.name);

        return fire;
    }

    private static Blink EnsureBlink(GameObject target, Material material)
    {
        Blink blink = target.GetComponent<Blink>();
        if (blink == null)
            blink = Undo.AddComponent<Blink>(target);

        Undo.RecordObject(blink, "Set up Blink");
        blink.highlightMaterial = material;
        blink.isBlink = false;
        EditorUtility.SetDirty(blink);
        blinksSetUp++;
        return blink;
    }

    // The thumbstick cap on the left controller model (inside the XR rig).
    private static Transform FindLeftThumbstick()
    {
        Transform xr = FindRoot("XR");
        if (xr == null)
            return null;

        foreach (Transform t in xr.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "ThumbStick")
                continue;

            for (Transform up = t.parent; up != null; up = up.parent)
                if (up.name == "Left Controller")
                    return t;
        }

        return null;
    }

    private static void SetParts(SerializedObject so, string group, ExtinguisherLock pin, Blink pinBlink, Transform nozzle,
                                 Blink nozzleBlink, Transform handle, Blink handleBlink, Transform body, Blink bodyBlink)
    {
        Set(so, group + ".pin", pin);
        Set(so, group + ".pinBlink", pinBlink);
        Set(so, group + ".nozzle", nozzle.GetComponent<XRBaseInteractable>());
        Set(so, group + ".nozzleBlink", nozzleBlink);
        Set(so, group + ".handle", handle.GetComponent<XRBaseInteractable>());
        Set(so, group + ".handleBlink", handleBlink);
        Set(so, group + ".body", body.GetComponent<XRBaseInteractable>());
        Set(so, group + ".bodyBlink", bodyBlink);
    }

    private static void SetTargets(SerializedObject so, string array, params (ExtinguishableFire fire, Blink blink)[] targets)
    {
        SerializedProperty list = so.FindProperty(array);
        list.arraySize = targets.Length;
        for (int i = 0; i < targets.Length; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("fire").objectReferenceValue = targets[i].fire;
            element.FindPropertyRelative("blink").objectReferenceValue = targets[i].blink;
        }
    }

    private static void Set(SerializedObject so, string path, Object value)
    {
        SerializedProperty property = so.FindProperty(path);
        if (property == null)
        {
            Debug.LogWarning("Guided training setup: the guide has no field '" + path + "'.");
            return;
        }

        property.objectReferenceValue = value;
    }
}
