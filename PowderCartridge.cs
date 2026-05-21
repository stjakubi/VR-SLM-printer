using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// The metal powder cartridge the player grabs and inserts into the
/// Powder Supply Module on the right side of the SLM 280 2.0.
///
/// On snap: WorkflowManager.OnPowderLoaded() is called → Step 3 unlocks.
/// The ghost silhouette at the socket is handled automatically by SocketVisualHint.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class PowderCartridge : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The XRSocketInteractor on the Powder Supply Module.\n" +
             "If empty, auto-found by tag 'PowderSocket' at Start.")]
    public XRSocketInteractor powderSocket;

    [Header("Appearance")]
    [Tooltip("Tint applied when the cartridge is correctly loaded")]
    public Color loadedTint = new Color(0.9f, 0.75f, 0.3f); // warm gold

    // -----------------------------------------------------------------------
    private Renderer cartridgeRenderer;
    private Color    originalColor;
    private bool     isLoaded = false;

    void Awake()
    {
        cartridgeRenderer = GetComponent<Renderer>();
        if (cartridgeRenderer != null)
            originalColor = cartridgeRenderer.material.GetColor("_BaseColor");
    }

    void Start()
    {
        if (powderSocket == null)
        {
            Debug.LogWarning("[PowderCartridge] powderSocket not assigned — rebuild the printer structure.");
            return;
        }

        powderSocket.selectEntered.AddListener(OnSnappedIntoSocket);
        powderSocket.selectExited.AddListener(OnRemovedFromSocket);
    }

    private void OnSnappedIntoSocket(SelectEnterEventArgs args)
    {
        if (args.interactableObject.transform != transform) return;
        if (isLoaded) return;

        isLoaded = true;

        if (cartridgeRenderer != null)
            cartridgeRenderer.material.SetColor("_BaseColor", loadedTint);

        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnPowderLoaded();

        // Contextual educational error — shows what can go wrong with powder
        if (PrinterErrorSimulator.Instance != null)
            PrinterErrorSimulator.Instance.TriggerContextualError(
                PrinterErrorSimulator.ErrorContext.Powder);

        Debug.Log("[PowderCartridge] Cartridge loaded into powder supply.");
    }

    private void OnRemovedFromSocket(SelectExitEventArgs args)
    {
        if (args.interactableObject.transform != transform) return;

        isLoaded = false;

        if (cartridgeRenderer != null)
            cartridgeRenderer.material.SetColor("_BaseColor", originalColor);

        Debug.Log("[PowderCartridge] Cartridge removed from powder supply.");
    }

    void OnDestroy()
    {
        if (powderSocket != null)
        {
            powderSocket.selectEntered.RemoveListener(OnSnappedIntoSocket);
            powderSocket.selectExited.RemoveListener(OnRemovedFromSocket);
        }
    }
}
