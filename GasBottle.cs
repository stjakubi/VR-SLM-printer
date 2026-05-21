using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach to the gas bottle GameObject.
/// The bottle is grabbable (XRGrabInteractable) and snaps into the
/// XRSocketInteractor on the gas inlet when placed nearby.
///
/// The ghost silhouette at the socket is handled by SocketVisualHint.cs.
/// This script notifies WorkflowManager when the bottle is successfully attached.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GasBottle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The XRSocketInteractor on the printer's gas inlet.\n" +
             "If left empty, auto-found by tag 'GasSocket' at Start.")]
    public XRSocketInteractor gasSocket;

    [Header("Attached Appearance")]
    [Tooltip("Colour tint applied to the bottle when connected (optional)")]
    public Color connectedTint = new Color(0.3f, 0.9f, 0.4f); // green glow

    // -----------------------------------------------------------------------
    private XRGrabInteractable grabInteractable;
    private Renderer           bottleRenderer;
    private Color              originalColor;
    private bool               isConnected = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        bottleRenderer   = GetComponent<Renderer>();

        if (bottleRenderer != null)
            originalColor = bottleRenderer.material.GetColor("_BaseColor");
    }

    void Start()
    {
        if (gasSocket == null)
        {
            Debug.LogWarning("[GasBottle] gasSocket not assigned — rebuild the printer structure.");
            return;
        }

        // Listen to socket events — socket fires when THIS bottle is inserted/removed
        gasSocket.selectEntered.AddListener(OnSnappedIntoSocket);
        gasSocket.selectExited.AddListener(OnRemovedFromSocket);
    }

    // -----------------------------------------------------------------------
    // Called when ANY interactable enters this socket
    // -----------------------------------------------------------------------
    private void OnSnappedIntoSocket(SelectEnterEventArgs args)
    {
        // Confirm it's this bottle (not some other object) that was snapped
        if (args.interactableObject.transform != transform) return;
        if (isConnected) return;

        isConnected = true;

        // Visual feedback — tint the bottle green
        if (bottleRenderer != null)
            bottleRenderer.material.SetColor("_BaseColor", connectedTint);

        // Notify workflow
        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnGasBottleAttached();

        // Contextual educational error — shows what can go wrong with gas supply
        if (PrinterErrorSimulator.Instance != null)
            PrinterErrorSimulator.Instance.TriggerContextualError(
                PrinterErrorSimulator.ErrorContext.Gas);

        Debug.Log("[GasBottle] Snapped into gas inlet.");
    }

    // -----------------------------------------------------------------------
    // Called when the bottle is pulled back out
    // -----------------------------------------------------------------------
    private void OnRemovedFromSocket(SelectExitEventArgs args)
    {
        if (args.interactableObject.transform != transform) return;

        isConnected = false;

        // Restore original colour
        if (bottleRenderer != null)
            bottleRenderer.material.SetColor("_BaseColor", originalColor);

        Debug.Log("[GasBottle] Removed from gas inlet.");
    }

    // -----------------------------------------------------------------------
    void OnDestroy()
    {
        if (gasSocket != null)
        {
            gasSocket.selectEntered.RemoveListener(OnSnappedIntoSocket);
            gasSocket.selectExited.RemoveListener(OnRemovedFromSocket);
        }
    }
}
