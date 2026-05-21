using UnityEngine;
using System.Collections;

/// <summary>
/// Drives the full SLM print simulation once the operator has snapped the build
/// platform into the chamber, closed the door, and pressed Start on the HMI.
///
/// What this version does (clean rewrite — previous version was stale):
///   • Starts only when the BuildPlatform state machine is in Snapped / Printing.
///   • Lowers Build_Platform one layer thickness at a time (as a smooth ramp over totalTime).
///   • Sweeps Chamber_RecoaterBar back and forth across the powder bed each layer —
///     the single most recognisable SLM process motion.
///   • Advances PrintingPartLayers one layer per step so the finished part grows upward.
///   • Pushes progress + status + estimated-time-remaining to the VirtualComputer HMI.
///   • Hooks into PrinterErrorSimulator for mid-print fault scenarios.
///   • On completion, parks the recoater at its home position, notifies WorkflowManager,
///     and activates PartRemoval so the platform becomes grabbable again.
///
/// Design:
///   • All references resolve automatically at Start() AND at StartPrint() — so a
///     "Build From Imported Model" rebuild doesn't leave stale Inspector links.
///   • Recoater home-X is captured at each StartPrint() (not once at Awake), so if the
///     recoater has drifted in the editor the first print still looks right.
/// </summary>
public class PrintSimulationManager : MonoBehaviour
{
    public static PrintSimulationManager Instance { get; private set; }

    // =======================================================================
    // Inspector fields
    // =======================================================================

    [Header("Scene References (auto-resolved at runtime)")]
    [Tooltip("The unified Build_Platform child of SLM_280_Printer.")]
    public Transform buildPlatform;

    [Tooltip("State machine on Build_Platform — gates grab/physics during printing.")]
    public BuildPlatform buildPlatformState;

    [Tooltip("Re-enables grab on the platform once printing finishes.")]
    public PartRemoval partRemoval;

    [Tooltip("Layer-by-layer stack that grows on top of the platform.")]
    public PrintingPartLayers printingPartLayers;

    [Header("Recoater Animation")]
    [Tooltip("Master switch — when OFF the recoater stays still for the entire print. Default OFF: the build-platform animation carries the visual story and the recoater sweep was too busy on top of it.")]
    public bool animateRecoater = false;

    [Tooltip("Chamber_RecoaterBar from the imported model — swept across the build area each layer.")]
    public Transform recoaterBar;

    public enum RecoaterAxis { WorldX, WorldZ, LocalX, LocalZ }

    [Tooltip("Which axis the recoater slides along. WorldX = left/right from the player's view (default).")]
    public RecoaterAxis recoaterSweepAxis = RecoaterAxis.WorldX;

    [Tooltip("Total peak-to-peak travel of the recoater (metres). Motion is centered on its rest position.")]
    public float recoaterSweepDistance = 0.30f;

    [Tooltip("Seconds for ONE full out-and-back recoater cycle (roughly one layer).")]
    public float recoaterCycleSeconds = 1.8f;

    [Header("Platform Descent")]
    [Tooltip("Total downward travel over the full print, in Unity units. Matches PrintingPartLayers total height.")]
    public float platformDropDistance = 0.32f;

    [Tooltip("Starting Y (local) of the platform at the moment StartPrint is called — captured automatically.")]
    public float platformStartY = 0.0f;

    [Header("Laser Flash (optional visual)")]
    public Renderer laserScanPlane;
    public Material laserActiveMaterial;
    public Material laserIdleMaterial;

    // =======================================================================
    // Runtime state
    // =======================================================================

    public enum PrintState { Idle, Starting, Printing, Paused, Completed }
    public PrintState CurrentState { get; private set; } = PrintState.Idle;

    private VirtualComputer.PrintablePart currentPart;
    private Coroutine printCoroutine;

    private Vector3 recoaterHomeWorld = Vector3.zero;  // rest position captured at StartPrint
    private Vector3 recoaterHomeLocal = Vector3.zero;  // same, in its parent's local space
    private float   printProgress = 0f;                // 0..1
    private int     currentLayer  = 0;
    private bool    isPaused      = false;

    // =======================================================================

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ResolveReferences();
    }

    // -----------------------------------------------------------------------
    // Finds Build_Platform + Chamber_RecoaterBar in the scene even if the
    // Inspector references were wiped by a rebuild. Called at Start() and
    // again at the top of StartPrint() so rebuilds never break the sim.
    // -----------------------------------------------------------------------
    private void ResolveReferences()
    {
        // Build_Platform
        if (buildPlatform == null)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "Build_Platform" && t.hideFlags == HideFlags.None)
                {
                    buildPlatform = t;
                    break;
                }
            }
        }

        if (buildPlatform != null)
        {
            if (buildPlatformState == null) buildPlatformState = buildPlatform.GetComponent<BuildPlatform>();
            if (partRemoval        == null) partRemoval        = buildPlatform.GetComponent<PartRemoval>();
            if (printingPartLayers == null) printingPartLayers = buildPlatform.GetComponentInChildren<PrintingPartLayers>(true);
        }

        // Chamber_RecoaterBar
        if (recoaterBar == null)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "Chamber_RecoaterBar" && t.hideFlags == HideFlags.None)
                {
                    recoaterBar = t;
                    break;
                }
            }
        }
    }

    // =======================================================================
    // Public API — called by VirtualComputer when the operator presses Start
    // =======================================================================

    public void StartPrint(VirtualComputer.PrintablePart part)
    {
        if (CurrentState == PrintState.Starting || CurrentState == PrintState.Printing)
        {
            Debug.LogWarning("[PrintSimulation] StartPrint ignored — already running.");
            return;
        }

        ResolveReferences();

        if (buildPlatform == null)
        {
            Debug.LogError("[PrintSimulation] Cannot start — Build_Platform not in scene.");
            return;
        }
        if (part == null)
        {
            Debug.LogError("[PrintSimulation] Cannot start — no PrintablePart passed.");
            return;
        }

        currentPart   = part;
        printProgress = 0f;
        currentLayer  = 0;
        isPaused      = false;

        // Capture the platform's current Y as the print start — whatever height the
        // player docked it at is layer 0.
        platformStartY = buildPlatform.localPosition.y;

        // Capture recoater rest position fresh so tweaks in-editor don't offset subsequent prints.
        if (recoaterBar != null)
        {
            recoaterHomeWorld = recoaterBar.position;
            recoaterHomeLocal = recoaterBar.localPosition;
        }

        // Lock the platform down — no grab during print.
        if (buildPlatformState != null) buildPlatformState.EnterPrintingState();

        // Configure and reset the slice stack for the chosen part.
        if (printingPartLayers != null)
        {
            printingPartLayers.ConfigureForPart(part);
            printingPartLayers.ResetLayers();
        }

        if (VirtualComputer.Instance != null)
            VirtualComputer.Instance.ShowProgressPanel(part);

        if (PrinterErrorSimulator.Instance != null)
            PrinterErrorSimulator.Instance.StartPrinting();

        CurrentState   = PrintState.Starting;
        printCoroutine = StartCoroutine(PrintRoutine());

        Debug.Log($"[PrintSimulation] Started: {part.partName} — {part.totalLayers} layers, sim time {part.realSimDuration:F0}s");
    }

    // =======================================================================
    // Pause / Resume — called by PrinterErrorSimulator events
    // =======================================================================

    public void PauseSimulation()
    {
        if (CurrentState != PrintState.Printing) return;
        isPaused     = true;
        CurrentState = PrintState.Paused;

        if (VirtualComputer.Instance != null)
            VirtualComputer.Instance.SetStatus("[!] PAUSED — Read the error and press Acknowledge");

        Debug.Log("[PrintSimulation] Paused.");
    }

    public void ResumeSimulation()
    {
        if (CurrentState != PrintState.Paused) return;
        isPaused     = false;
        CurrentState = PrintState.Printing;

        if (VirtualComputer.Instance != null)
            VirtualComputer.Instance.SetStatus("Printing resumed...");

        Debug.Log("[PrintSimulation] Resumed.");
    }

    // =======================================================================
    // Main coroutine
    // =======================================================================

    private IEnumerator PrintRoutine()
    {
        // ---- Startup: flood chamber with inert gas ----
        if (VirtualComputer.Instance != null)
            VirtualComputer.Instance.SetStatus("Flooding chamber with inert gas...");
        yield return new WaitForSeconds(3f);

        if (VirtualComputer.Instance != null)
            VirtualComputer.Instance.SetStatus("Oxygen level: 0.08% — Starting laser scan");

        CurrentState = PrintState.Printing;

        float elapsed     = 0f;
        float totalTime   = Mathf.Max(1f, currentPart.realSimDuration);
        int   totalLayers = Mathf.Max(1, currentPart.totalLayers);

        while (elapsed < totalTime)
        {
            // Pause handling — freeze time while the operator is acknowledging an error.
            while (isPaused) yield return null;

            yield return null;
            elapsed += Time.deltaTime;

            printProgress = Mathf.Clamp01(elapsed / totalTime);
            currentLayer  = Mathf.RoundToInt(printProgress * totalLayers);

            // --- Platform descent (smooth, across the full print) ---
            if (buildPlatform != null)
            {
                Vector3 pos = buildPlatform.localPosition;
                pos.y = platformStartY - (printProgress * platformDropDistance);
                buildPlatform.localPosition = pos;
            }

            // --- Recoater sweep (OPTIONAL — default off) ---
            // Disabled per design decision 2026-04-23: the layer-by-layer part
            // growth is already the focal point of the simulation, and sweeping
            // the recoater on top of it felt busy and distracting. The flag and
            // all original parameters are preserved so the motion can be turned
            // back on from the Inspector without code changes.
            if (animateRecoater && recoaterBar != null)
            {
                // sin over time gives a smooth -1..+1..-1 oscillation; × half-distance
                // makes the peak-to-peak travel equal to recoaterSweepDistance.
                float omega  = (2f * Mathf.PI) / Mathf.Max(0.1f, recoaterCycleSeconds);
                float offset = Mathf.Sin(elapsed * omega) * (recoaterSweepDistance * 0.5f);

                switch (recoaterSweepAxis)
                {
                    case RecoaterAxis.WorldX:
                        recoaterBar.position = recoaterHomeWorld + Vector3.right   * offset;
                        break;
                    case RecoaterAxis.WorldZ:
                        recoaterBar.position = recoaterHomeWorld + Vector3.forward * offset;
                        break;
                    case RecoaterAxis.LocalX:
                        recoaterBar.localPosition = recoaterHomeLocal + Vector3.right   * offset;
                        break;
                    case RecoaterAxis.LocalZ:
                        recoaterBar.localPosition = recoaterHomeLocal + Vector3.forward * offset;
                        break;
                }
            }

            // --- Layer-by-layer part growth ---
            if (printingPartLayers != null)
                printingPartLayers.UpdateLayers(printProgress);

            // --- Laser plane flash (optional) ---
            if (laserScanPlane != null && laserActiveMaterial != null && laserIdleMaterial != null)
            {
                bool laserOn = Mathf.Sin(elapsed * 30f) > 0f;
                laserScanPlane.material = laserOn ? laserActiveMaterial : laserIdleMaterial;
            }

            // --- HMI updates ---
            if (VirtualComputer.Instance != null)
            {
                VirtualComputer.Instance.UpdateProgress(printProgress, currentLayer, totalLayers);

                float remaining = totalTime - elapsed;
                int remMin = Mathf.FloorToInt(remaining / 60f);
                int remSec = Mathf.RoundToInt(remaining % 60f);
                VirtualComputer.Instance.SetEstimatedTime($"{remMin}m {remSec:00}s");
                VirtualComputer.Instance.SetStatus(GetStatusMessage(printProgress));
            }
        }

        CompletePrint();
    }

    // =======================================================================
    // Completion
    // =======================================================================

    private void CompletePrint()
    {
        CurrentState = PrintState.Completed;

        // Park the recoater back at its captured rest position — but only if we
        // actually moved it. When animateRecoater is off we never touch it, so
        // there is nothing to reset.
        if (animateRecoater && recoaterBar != null)
        {
            if (recoaterSweepAxis == RecoaterAxis.WorldX || recoaterSweepAxis == RecoaterAxis.WorldZ)
                recoaterBar.position      = recoaterHomeWorld;
            else
                recoaterBar.localPosition = recoaterHomeLocal;
        }

        // Make sure the laser plane is idle.
        if (laserScanPlane != null && laserIdleMaterial != null)
            laserScanPlane.material = laserIdleMaterial;

        if (PrinterErrorSimulator.Instance != null)
            PrinterErrorSimulator.Instance.StopPrinting();

        if (VirtualComputer.Instance != null)
        {
            VirtualComputer.Instance.UpdateProgress(1f, currentPart.totalLayers, currentPart.totalLayers);
            VirtualComputer.Instance.SetStatus("[OK] Print complete. Cooling down...");
        }

        // Flip the platform to Completed state. PartRemoval re-enables the grab
        // so the player can pull the platform out and carry it to the workbench.
        if (buildPlatformState != null) buildPlatformState.EnterCompletedState();

        if (WorkflowManager.Instance != null) WorkflowManager.Instance.OnPrintCompleted();

        if (partRemoval != null) partRemoval.ActivateRemoval();

        StartCoroutine(ShowCompletionAfterDelay());

        Debug.Log("[PrintSimulation] Print completed successfully.");
    }

    private IEnumerator ShowCompletionAfterDelay()
    {
        yield return new WaitForSeconds(4f);
        if (VirtualComputer.Instance != null)
            VirtualComputer.Instance.ShowCompletionPanel(currentPart);

        if (LearningManager.Instance != null)
            Debug.Log("[PrintSimulation] Notifying LearningManager — simulation complete.");
    }

    // =======================================================================
    // Status strings shown on the HMI at various progress stages
    // =======================================================================

    private string GetStatusMessage(float progress)
    {
        if (progress < 0.05f) return "Preheating build platform...";
        if (progress < 0.15f) return "Printing — first layers (critical phase)";
        if (progress < 0.40f) return "Printing — building base geometry";
        if (progress < 0.70f) return "Printing — mid-section";
        if (progress < 0.90f) return "Printing — final layers";
        return "Printing — completing top surface...";
    }
}
