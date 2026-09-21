using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Makes a teleport pad light up while the teleport ray points at it.
/// Put this on the Teleport Anchor object, next to its Teleportation Anchor component.
/// It fades every renderer under the pad to Hover Color when hovered and back again when the ray leaves.
/// </summary>
public class TeleportAnchorHighlight : MonoBehaviour
{
    [Header("Feel")]
    [SerializeField] private Color hoverColor = new Color(0.25f, 0.85f, 1f, 1f);
    [SerializeField, Min(0f)] private float fadeSeconds = 0.15f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");    // URP Lit / Simple Lit / Unlit

    private XRBaseInteractable anchor;
    private Renderer[] pads;
    private MaterialPropertyBlock block;
    private Color normalColor;
    private bool hovered;
    private float amount;       // 0 = normal colour, 1 = fully highlighted


    private void Awake()
    {
        anchor = GetComponent<XRBaseInteractable>();
        pads = GetComponentsInChildren<Renderer>();
        if (anchor == null || pads.Length == 0 || !pads[0].sharedMaterial.HasProperty(BaseColorId))
        {
            Debug.LogError("TeleportAnchorHighlight: needs a Teleportation Anchor on this object and a pad with a URP material.", this);
            enabled = false;
            return;
        }

        block = new MaterialPropertyBlock();
        normalColor = pads[0].sharedMaterial.GetColor(BaseColorId);
    }

    private void OnEnable()
    {
        if (anchor == null)
            return;

        anchor.hoverEntered.AddListener(OnHoverEntered);
        anchor.hoverExited.AddListener(OnHoverExited);
    }

    private void OnDisable()
    {
        if (anchor == null)
            return;

        anchor.hoverEntered.RemoveListener(OnHoverEntered);
        anchor.hoverExited.RemoveListener(OnHoverExited);
    }

    private void OnHoverEntered(HoverEnterEventArgs _) => hovered = true;

    private void OnHoverExited(HoverExitEventArgs _) => hovered = false;

    private void Update()
    {
        float target = hovered ? 1f : 0f;
        if (Mathf.Approximately(amount, target))
            return;

        amount = fadeSeconds > 0f ? Mathf.MoveTowards(amount, target, Time.deltaTime / fadeSeconds) : target;

        Color colour = Color.Lerp(normalColor, hoverColor, amount);
        foreach (Renderer pad in pads)
        {
            pad.GetPropertyBlock(block);
            block.SetColor(BaseColorId, colour);
            pad.SetPropertyBlock(block);
        }
    }
}
