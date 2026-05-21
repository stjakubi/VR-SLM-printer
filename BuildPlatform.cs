using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// The unified SLM_BuildPlatform model — one GameObject plays every role:
///   1. Storage   — sitting outside the chamber, grabbable by the player.
///   2. Snapped   — docked into the chamber socket, workflow unlocked to close door.
///   3. Printing  — driven by PrintSimulationManager, moving downward layer by layer.
///   4. Completed — print done, becomes grabbable again so the player can remove it.
///
/// This script owns the grab + physics state transitions so nothing else has to.
/// PartRemoval handles the workbench-side of the "Completed" phase separately.
/// </summary>
public class BuildPlatform : MonoBehaviour
{
    public enum State { Storage, Snapped, Printing, Completed }

    [Header("References")]
    [Tooltip("The XRSocketInteractor inside the build chamber. Assigned automatically by PrinterStructureBuilder.")]
    public XRSocketInteractor chamberSocket;

    [Header("Appearance")]
    [Tooltip("Colour tint applied while the platform is docked in the chamber (visual confirmation).")]
    public Color insertedTint = new Color(0.6f, 0.8f, 1.0f);

    public State CurrentState { get; private set; } = State.Storage;

    // -----------------------------------------------------------------------
    private Rigidbody           rb;
    private XRGrabInteractable  grab;
    private Renderer[]          platformRenderers;
    private Color[]             originalColors;

    void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // Cache renderers + their base colours so we can tint / restore on snap / unsnap.
        platformRenderers = GetComponentsInChildren<Renderer>();
        originalColors    = new Color[platformRenderers.Length];
        for (int i = 0; i < platformRenderers.Length; i++)
        {
            Material m = platformRenderers[i] != null ? platformRenderers[i].material : null;
            if (m != null && m.HasProperty("_BaseColor"))
                originalColors[i] = m.GetColor("_BaseColor");
        }
    }

    void Start()
    {
        if (chamberSocket == null)
        {
            Debug.LogWarning("[BuildPlatform] chamberSocket not assigned — rebuild the printer structure.");
            return;
        }

        chamberSocket.selectEntered.AddListener(OnSnappedIntoSocket);
        chamberSocket.selectExited.AddListener(OnRemovedFromSocket);
    }

    // -----------------------------------------------------------------------
    // Socket callbacks
    // -----------------------------------------------------------------------
    private void OnSnappedIntoSocket(SelectEnterEventArgs args)
    {
        if (args.interactableObject.transform != transform) return;
        if (CurrentState != State.Storage) return;

        CurrentState = State.Snapped;

        // Freeze physics — simulation drives this object from now on.
        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
            rb.useGravity      = false;
        }

        // Visual: tint the platform blue to confirm insertion
        ApplyTint(insertedTint);

        // Hide the chamber ghost hint once the platform is docked. SocketVisualHint
        // also listens to this same event and hides itself, but we hide here too as
        // a safety net so the blue ghost always disappears on snap.
        if (chamberSocket != null)
        {
            SocketVisualHint hint = chamberSocket.GetComponent<SocketVisualHint>();
            if (hint != null && hint.ghostObject != null)
                hint.ghostObject.SetActive(false);
        }

        // Notify workflow (Step 4 unlock — close door)
        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnPlatformInserted();

        if (PrinterErrorSimulator.Instance != null)
            PrinterErrorSimulator.Instance.TriggerContextualError(
                PrinterErrorSimulator.ErrorContext.Platform);

        Debug.Log("[BuildPlatform] Snapped into chamber socket — ready for printing.");
    }

    private void OnRemovedFromSocket(SelectExitEventArgs args)
    {
        if (args.interactableObject.transform != transform) return;
        // Ignore the "removal" the simulation triggers by repositioning us —
        // only a genuine player pull-out should return us to Storage.
        if (CurrentState == State.Printing || CurrentState == State.Completed) return;

        CurrentState = State.Storage;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
        }

        // Show the ghost hint again so the player can re-dock.
        if (chamberSocket != null)
        {
            SocketVisualHint hint = chamberSocket.GetComponent<SocketVisualHint>();
            if (hint != null && hint.ghostObject != null)
                hint.ghostObject.SetActive(true);
        }

        RestoreTint();
        Debug.Log("[BuildPlatform] Pulled out of chamber — back to Storage.");
    }

    // -----------------------------------------------------------------------
    // Called by PrintSimulationManager as the print coroutine starts/ends
    // -----------------------------------------------------------------------
    public void EnterPrintingState()
    {
        CurrentState = State.Printing;

        // Make absolutely sure the player cannot yank the platform mid-print.
        if (grab != null) grab.enabled = false;
        if (rb   != null) { rb.isKinematic = true; rb.useGravity = false; }
    }

    public void EnterCompletedState()
    {
        CurrentState = State.Completed;
        // PartRemoval.ActivateRemoval() re-enables the grab when it's safe to pull the
        // platform out — handled there so the workflow stays in one place.
    }

    // -----------------------------------------------------------------------
    private void ApplyTint(Color c)
    {
        if (platformRenderers == null) return;
        foreach (Renderer r in platformRenderers)
        {
            if (r == null) continue;
            Material m = r.material;
            if (m != null && m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
        }
    }

    private void RestoreTint()
    {
        if (platformRenderers == null || originalColors == null) return;
        for (int i = 0; i < platformRenderers.Length; i++)
        {
            if (platformRenderers[i] == null) continue;
            Material m = platformRenderers[i].material;
            if (m != null && m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", originalColors[i]);
        }
    }

    void OnDestroy()
    {
        if (chamberSocket != null)
        {
            chamberSocket.selectEntered.RemoveListener(OnSnappedIntoSocket);
            chamberSocket.selectExited.RemoveListener(OnRemovedFromSocket);
        }
    }
}
