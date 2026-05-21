using UnityEngine;

/// <summary>
/// Tracks the SLM 280 operational workflow.
///
/// PRE-PRINT TASKS (any order — achievements):
///   • Attach gas bottle          → OnGasBottleAttached()
///   • Load powder cartridge      → OnPowderLoaded()
///   • Insert build platform      → OnPlatformInserted()
///   • Close front door           → OnDoorClosed()
///   • Calibrate laser (HMI)      → OnLaserCalibrated()
///   Once all five are done → AllPrepComplete = true → VirtualComputer unlocks.
///
/// POST-PRINT PHASE (sequential — order matters):
///   1. Print started             → OnPrintStarted()
///   2. Print completes           → OnPrintCompleted()
///   3. Open front door           → OnDoorOpened()
///   4. Remove part + platform    → OnPartRemoved()
/// </summary>
public class WorkflowManager : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Singleton
    // -----------------------------------------------------------------------
    public static WorkflowManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -----------------------------------------------------------------------
    // Pre-print achievements  (independent — any order)
    // -----------------------------------------------------------------------
    public bool GasAttached      { get; private set; }
    public bool PowderLoaded     { get; private set; }
    public bool PlatformInserted { get; private set; }
    public bool DoorClosed       { get; private set; }
    public bool LaserCalibrated  { get; private set; }

    /// <summary>True when every pre-print task has been completed.</summary>
    /// Door state is tracked separately and shown as a live indicator on the
    /// VirtualComputer screen — it is not a checklist achievement.
    public bool AllPrepComplete =>
        GasAttached && PowderLoaded && PlatformInserted && LaserCalibrated;

    /// <summary>
    /// Fired whenever a pre-print achievement is ticked off.
    /// VirtualComputer subscribes to this to refresh the checklist panel.
    /// </summary>
    public event System.Action OnPrepTaskChanged;

    // -----------------------------------------------------------------------
    // Post-print phase  (sequential)
    // -----------------------------------------------------------------------
    public enum PostPrintPhase { Idle, Printing, PrintDone, DoorOpened, Complete }

    [Header("Post-Print State (read only)")]
    [SerializeField] private PostPrintPhase printPhase = PostPrintPhase.Idle;
    public PostPrintPhase PrintPhase => printPhase;

    // -----------------------------------------------------------------------
    // Kept for backward-compat with LaserCalibration.cs #if !UNITY_EDITOR gate
    // -----------------------------------------------------------------------
    public enum WorkflowStep
    {
        AttachGas, LoadPowder, InsertPlatform, CloseDoor,
        CalibrateLaser, StartPrint, WaitForPrint, OpenDoor, RemovePart, Complete
    }

    // -----------------------------------------------------------------------
    // Start
    // -----------------------------------------------------------------------
    void Start()
    {
        ShowHint("Welcome! Complete all pre-print tasks on the checklist, then start printing.");
    }

    // -----------------------------------------------------------------------
    // Pre-print achievement notifications  (called by other scripts)
    // -----------------------------------------------------------------------

    public void OnGasBottleAttached()
    {
        if (GasAttached) return;
        GasAttached = true;
        AchievementUnlocked(
            "[OK]  Gas Bottle Attached",
            "Shielding gas (Argon / Nitrogen) supply connected. " +
            "Inert atmosphere prevents oxidation of reactive metal powder during fusion.");
    }

    public void OnPowderLoaded()
    {
        if (PowderLoaded) return;
        PowderLoaded = true;
        AchievementUnlocked(
            "[OK]  Powder Cartridge Loaded",
            "Metal powder cartridge inserted into the supply module. " +
            "Typical SLM 280 2.0 powders: Ti-6Al-4V, 316L, AlSi10Mg, Inconel 718.");
    }

    public void OnPlatformInserted()
    {
        if (PlatformInserted) return;
        PlatformInserted = true;
        AchievementUnlocked(
            "[OK]  Build Platform Inserted",
            "Steel build platform locked into the build chamber. " +
            "The platform lowers by one layer thickness (~20–80 um) after each scan.");
    }

    public void OnDoorClosed()
    {
        if (DoorClosed) return;
        DoorClosed = true;
        AchievementUnlocked(
            "[OK]  Chamber Door Closed",
            "Build chamber sealed. The machine will now purge the chamber with inert gas " +
            "until O2 levels drop below 0.1 % before allowing a print to start.");
    }

    public void OnLaserCalibrated()
    {
        if (LaserCalibrated) return;
        LaserCalibrated = true;
        AchievementUnlocked(
            "[OK]  Laser Calibrated",
            "700 W fiber laser and galvanometer mirrors aligned. " +
            "Beam diameter < 80 um confirmed. Scan field geometry verified.");
    }

    // -----------------------------------------------------------------------
    // Post-print sequential notifications
    // -----------------------------------------------------------------------

    public void OnPrintStarted()
    {
        if (printPhase != PostPrintPhase.Idle) return;
        printPhase = PostPrintPhase.Printing;
        ShowHint("Printing in progress. Monitor the build on the computer screen.");
        Debug.Log("[WorkflowManager] Print started.");
    }

    public void OnPrintCompleted()
    {
        if (printPhase != PostPrintPhase.Printing) return;
        printPhase = PostPrintPhase.PrintDone;
        ShowHint("Print finished! Open the FRONT DOOR to access the build chamber.");
        Debug.Log("[WorkflowManager] Print completed.");
    }

    public void OnDoorOpened()
    {
        if (printPhase != PostPrintPhase.PrintDone) return;
        printPhase = PostPrintPhase.DoorOpened;
        ShowHint("Door open. Allow the chamber to cool, then remove the build platform.");
        Debug.Log("[WorkflowManager] Door opened after print.");
    }

    public void OnPartRemoved()
    {
        if (printPhase != PostPrintPhase.DoorOpened) return;
        printPhase = PostPrintPhase.Complete;
        ShowHint("Part removed! Workflow complete. The part is ready for post-processing.");
        Debug.Log("[WorkflowManager] Part removed — workflow complete.");
    }

    // -----------------------------------------------------------------------
    // Backward-compat shim used by LaserCalibration.cs (#if !UNITY_EDITOR)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Legacy check kept so LaserCalibration.cs compiles in non-editor builds.
    /// For CalibrateLaser: the door just needs to be closed.
    /// For StartPrint: all prep tasks must be complete.
    /// </summary>
    public bool IsStepReady(WorkflowStep step)
    {
        switch (step)
        {
            case WorkflowStep.CalibrateLaser:
                if (!DoorClosed)
                {
                    ShowHint("Close the build chamber door before calibrating the laser.");
                    return false;
                }
                return true;

            case WorkflowStep.StartPrint:
                if (!AllPrepComplete)
                {
                    ShowHint("Complete all pre-print tasks on the checklist first.");
                    return false;
                }
                return true;

            default:
                return true;
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void AchievementUnlocked(string title, string detail)
    {
        Debug.Log($"[WorkflowManager] Achievement: {title}");

        if (ControllerLabelDisplay.Instance != null)
            ControllerLabelDisplay.Instance.ShowPartLabel(title, detail);

        // Notify VirtualComputer (and any other listeners) to refresh the checklist
        OnPrepTaskChanged?.Invoke();

        if (AllPrepComplete)
        {
            Debug.Log("[WorkflowManager] All pre-print tasks complete — VirtualComputer unlocked.");
            ShowHint("All pre-print tasks complete! Go to the computer and start the print.");
        }
    }

    private void ShowHint(string message)
    {
        if (ControllerLabelDisplay.Instance != null)
            ControllerLabelDisplay.Instance.ShowPartLabel("Workflow", message);
        Debug.Log($"[WorkflowManager] Hint: {message}");
    }

    /// <summary>Resets everything back to zero (for replay / quiz mode).</summary>
    public void ResetWorkflow()
    {
        GasAttached      = false;
        PowderLoaded     = false;
        PlatformInserted = false;
        DoorClosed       = false;
        LaserCalibrated  = false;
        printPhase       = PostPrintPhase.Idle;
        OnPrepTaskChanged?.Invoke();
        ShowHint("Workflow reset. Complete all pre-print tasks to begin.");
        Debug.Log("[WorkflowManager] Workflow reset.");
    }

    public bool IsComplete() => printPhase == PostPrintPhase.Complete;
}
