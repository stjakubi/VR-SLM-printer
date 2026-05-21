using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach this to any socket interactor GameObject to show a transparent ghost
/// silhouette of the object that should be placed there.
///
/// Behaviour:
///   - Idle:    Ghost pulses gently (slow alpha fade) to attract attention
///   - Nearby:  Ghost brightens when the player holds the correct object close
///   - Snapped: Ghost disappears when something is placed in the socket
///
/// HOW TO SET UP (auto-built by PrinterStructureBuilder, but manual steps below):
///   1. Create a child object under the socket that matches the snappable object's shape
///   2. Give it a semi-transparent material
///   3. Attach this script to the SOCKET GameObject (not the ghost child)
///   4. Assign the ghostObject reference to that child
/// </summary>
public class SocketVisualHint : MonoBehaviour
{
    [Header("Ghost Object")]
    [Tooltip("A child GameObject (same shape as snappable object) with transparent material")]
    public GameObject ghostObject;

    [Header("Pulse Settings")]
    [Tooltip("Slowest alpha value during idle pulse")]
    [Range(0f, 1f)] public float minAlpha = 0.15f;

    [Tooltip("Brightest alpha value during idle pulse")]
    [Range(0f, 1f)] public float maxAlpha = 0.45f;

    [Tooltip("How fast the ghost pulses")]
    public float pulseSpeed = 1.2f;

    [Header("Proximity Highlight")]
    [Tooltip("How close (metres) the held object must be to trigger the bright highlight")]
    public float highlightRadius = 0.35f;

    [Tooltip("Alpha when a held object is nearby")]
    [Range(0f, 1f)] public float highlightAlpha = 0.75f;

    [Tooltip("Tint colour for the highlight state")]
    public Color highlightColor = new Color(0.4f, 0.9f, 1f, 0.75f); // cyan glow

    [Tooltip("Tint colour for idle pulse state")]
    public Color idleColor = new Color(0.7f, 0.7f, 1f, 0.3f); // soft blue

    [Header("Latch behaviour")]
    [Tooltip("If true, once the ghost has been hidden by a successful snap it " +
             "stays hidden permanently for this session — even if the socket " +
             "later reports no selection (e.g. because a print simulation " +
             "moved the snapped object out of the trigger volume). Use this " +
             "for the chamber build-platform socket so the ghost doesn't " +
             "reappear during the print animation.")]
    public bool keepHiddenAfterFirstSnap = false;

    // -----------------------------------------------------------------------
    // Internal
    // -----------------------------------------------------------------------
    private XRSocketInteractor socket;
    private Renderer ghostRenderer;
    private Material ghostMaterial;
    private bool isOccupied      = false;
    private bool hasEverOccupied = false;

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();

        // Defensive: if the serialized ghostObject reference was lost (scene
        // edits, prefab reimport, etc.), find a child named "GhostHint".
        if (ghostObject == null)
        {
            Transform fallback = transform.Find("GhostHint");
            if (fallback != null) ghostObject = fallback.gameObject;
        }

        if (ghostObject != null)
        {
            ghostRenderer = ghostObject.GetComponent<Renderer>();
            if (ghostRenderer != null)
            {
                // Instance the material so we can change alpha at runtime
                ghostMaterial = new Material(ghostRenderer.material);
                ghostRenderer.material = ghostMaterial;
            }
        }
    }

    void Start()
    {
        if (socket != null)
        {
            socket.selectEntered.AddListener(_ => OnSocketOccupied());
            socket.selectExited.AddListener(_ => OnSocketVacated());
        }
    }

    void Update()
    {
        if (ghostObject == null) return;

        // Belt-and-braces: in case the selectEntered listener wasn't wired up at
        // Start (this can happen if the socket's interactable was added after
        // the listener was bound), poll the socket's selection state directly
        // every frame and force the ghost off whenever something is in it.
        if (socket != null && socket.hasSelection)
        {
            if (ghostObject.activeSelf) ghostObject.SetActive(false);
            isOccupied      = true;
            hasEverOccupied = true;
            return;
        }
        else if (isOccupied)
        {
            // Socket no longer reports a selection. If we're latching, keep the
            // ghost hidden — the print simulation moves the platform out of the
            // trigger volume, but conceptually it's still "in" the socket.
            if (keepHiddenAfterFirstSnap && hasEverOccupied)
            {
                // Stay hidden — do not reset isOccupied or re-enable the ghost.
                return;
            }

            isOccupied = false;
            ghostObject.SetActive(true);
        }

        if (ghostMaterial == null) return;
        if (isOccupied) return; // ghost already hidden

        // Check if player is holding something near this socket
        bool nearbyHeld = IsHeldObjectNearby();

        if (nearbyHeld)
        {
            // Solid highlight: show clearly where to place it
            Color c = highlightColor;
            c.a = highlightAlpha;
            ghostMaterial.color = c;
        }
        else
        {
            // Gentle pulse
            float alpha = Mathf.Lerp(minAlpha, maxAlpha,
                (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f);
            Color c = idleColor;
            c.a = alpha;
            ghostMaterial.color = c;
        }
    }

    // -----------------------------------------------------------------------
    // Check if there's a grabbed interactable within highlightRadius
    // -----------------------------------------------------------------------
    private bool IsHeldObjectNearby()
    {
        // Find all colliders within radius
        Collider[] hits = Physics.OverlapSphere(transform.position, highlightRadius);
        foreach (Collider hit in hits)
        {
            var interactable = hit.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (interactable != null && interactable.isSelected)
                return true;
        }
        return false;
    }

    // -----------------------------------------------------------------------
    // Socket events
    // -----------------------------------------------------------------------
    private void OnSocketOccupied()
    {
        isOccupied      = true;
        hasEverOccupied = true;
        if (ghostObject != null) ghostObject.SetActive(false);
        Debug.Log($"[SocketVisualHint] Socket on {gameObject.name} occupied — ghost hidden.");
    }

    private void OnSocketVacated()
    {
        // If we're latching, the ghost stays hidden forever — the simulation
        // moving the platform mustn't trigger a re-show.
        if (keepHiddenAfterFirstSnap && hasEverOccupied)
        {
            Debug.Log($"[SocketVisualHint] Socket on {gameObject.name} vacated — latch active, ghost stays hidden.");
            return;
        }

        isOccupied = false;
        if (ghostObject != null) ghostObject.SetActive(true);
        Debug.Log($"[SocketVisualHint] Socket on {gameObject.name} vacated — ghost shown.");
    }

    /// <summary>
    /// Forcibly resets the latch so the ghost can be shown again on the next
    /// print cycle. Call this from PrintSimulationManager / WorkflowManager
    /// when the workflow loops back to the start.
    /// </summary>
    public void ResetLatch()
    {
        hasEverOccupied = false;
        isOccupied      = (socket != null && socket.hasSelection);
        if (ghostObject != null) ghostObject.SetActive(!isOccupied);
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveAllListeners();
            socket.selectExited.RemoveAllListeners();
        }
    }

    // -----------------------------------------------------------------------
    // Helper: call this from PrinterStructureBuilder to auto-create the ghost
    // -----------------------------------------------------------------------
    public static SocketVisualHint CreateOnSocket(
        XRSocketInteractor socket,
        PrimitiveType ghostShape,
        Vector3 ghostLocalScale,
        Color ghostIdleColor,
        Color ghostHighlightColor,
        bool keepHiddenAfterFirstSnap = false)
    {
        SocketVisualHint hint           = socket.gameObject.AddComponent<SocketVisualHint>();
        hint.idleColor                  = ghostIdleColor;
        hint.highlightColor             = ghostHighlightColor;
        hint.keepHiddenAfterFirstSnap   = keepHiddenAfterFirstSnap;

        // Create the ghost child
        GameObject ghost = GameObject.CreatePrimitive(ghostShape);
        ghost.name = "GhostHint";
        ghost.transform.SetParent(socket.transform);
        ghost.transform.localPosition = Vector3.zero;
        ghost.transform.localRotation = Quaternion.identity;
        ghost.transform.localScale    = ghostLocalScale;

        // Remove collider from ghost so it doesn't interfere with physics
        Collider col = ghost.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        // Create transparent material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        // Enable transparency
        mat.SetFloat("_Surface", 1f);           // 0 = opaque, 1 = transparent
        mat.SetFloat("_Blend", 0f);             // Alpha blend
        mat.SetFloat("_AlphaClip", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;

        Color c = ghostIdleColor;
        c.a = 0.3f;
        mat.SetColor("_BaseColor", c);

        Renderer r = ghost.GetComponent<Renderer>();
        if (r != null) r.material = mat;

        hint.ghostObject = ghost;

        return hint;
    }
}
