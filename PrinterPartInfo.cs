using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;

/// <summary>
/// Attach this script to any 3D object representing a printer component.
/// When the player points their VR controller ray at the object, the label
/// appears ABOVE THE CONTROLLER (not above the part) via ControllerLabelDisplay.
/// The part also highlights to confirm selection.
/// </summary>
public class PrinterPartInfo : MonoBehaviour
{
    [Header("Part Information")]
    [Tooltip("The name of this printer component, e.g. 'Recoater Blade'")]
    public string partName = "Part Name";

    [Tooltip("A short description shown when the player looks at this part")]
    [TextArea(3, 5)]
    public string partDescription = "Description of this printer component.";

    [Header("Highlight Settings")]
    [Tooltip("Material applied when this part is hovered over")]
    public Material highlightMaterial;

    private Material originalMaterial;
    private Renderer partRenderer;
    private XRBaseInteractable interactable;
    private bool hasBeenDiscovered = false;

    void Awake()
    {
        partRenderer = GetComponent<Renderer>();
        if (partRenderer != null)
            originalMaterial = partRenderer.material;

        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }
        else
        {
            Debug.LogWarning($"[PrinterPartInfo] No XRBaseInteractable on {gameObject.name}.");
        }
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        // Only the RIGHT controller's ray triggers the info label.
        // Left-controller hovers are ignored so the player can keep the left
        // hand free for grabbing / workflow interaction without spamming labels.
        if (!IsRightHand(args.interactorObject))
            return;

        // Show label above controller
        if (ControllerLabelDisplay.Instance != null)
            ControllerLabelDisplay.Instance.ShowPartLabel(partName, partDescription);

        // Highlight the part
        ApplyHighlight();

        // Register first-time discovery with the LearningManager
        if (!hasBeenDiscovered)
        {
            hasBeenDiscovered = true;
            if (LearningManager.Instance != null)
                LearningManager.Instance.RegisterPartDiscovered();
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        // Mirror the enter-side filter — only the right-hand exit affects the label.
        if (!IsRightHand(args.interactorObject))
            return;

        // We deliberately DO NOT hide the label here. The label keeps showing
        // the last part the player looked at until a different part is hovered,
        // so the player can read the description without having to keep their
        // ray steady on the part. The highlight is still removed so they can
        // tell they're no longer pointing at it.
        RemoveHighlight();
    }

    // -----------------------------------------------------------------------
    // True if the interactor belongs to the right-hand controller.
    // Checks XRBaseInteractor.handedness first (XRI 3.0), then falls back to
    // matching "Right" in the GameObject name hierarchy for older rigs.
    // -----------------------------------------------------------------------
    private bool IsRightHand(object interactorObject)
    {
        var mb = interactorObject as MonoBehaviour;
        if (mb == null) return false;

        // XRI 3.0: XRBaseInteractor exposes a handedness enum (None/Left/Right)
        var baseInteractor = mb.GetComponent<XRBaseInteractor>();
        if (baseInteractor != null)
        {
            if (baseInteractor.handedness == InteractorHandedness.Right) return true;
            if (baseInteractor.handedness == InteractorHandedness.Left)  return false;
            // handedness == None -> fall through to name-based check
        }

        // Fallback: look up the transform chain for "Right" / "Left" tokens.
        Transform t = mb.transform;
        while (t != null)
        {
            string n = t.name;
            if (n.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("Left",  System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
            t = t.parent;
        }
        return false;
    }

    private void ApplyHighlight()
    {
        if (partRenderer != null && highlightMaterial != null)
            partRenderer.material = highlightMaterial;
    }

    private void RemoveHighlight()
    {
        if (partRenderer != null && originalMaterial != null)
            partRenderer.material = originalMaterial;
    }
}
