using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Contextual educational error system for the SLM 280 2.0 VR trainer.
///
/// Errors are triggered by specific player interactions, not random timers.
/// Each interaction context has its own pool of realistic fault scenarios.
/// When triggered, the simulation pauses, the player reads what can go wrong
/// with that specific step, then acknowledges to continue.
///
/// CONTEXTS:
///   Gas      — triggered when gas bottle is snapped in
///   Powder   — triggered when powder cartridge is loaded
///   Platform — triggered when build platform is inserted
///   Door     — triggered when chamber door is closed
///   Laser    — triggered at the start of laser calibration
///   Print    — sparse errors during printing (recoater crash, overtemp)
/// </summary>
public class PrinterErrorSimulator : MonoBehaviour
{
    public static PrinterErrorSimulator Instance { get; private set; }

    public enum ErrorContext { Gas, Powder, Platform, Door, Laser, Print }

    [Header("Print-time errors")]
    [Tooltip("If true, 1-2 realistic errors fire during printing to simulate real faults")]
    public bool enablePrintErrors = true;

    [Tooltip("Seconds into the print before the first print-time error fires")]
    public float firstPrintErrorDelay = 20f;

    [Header("Events")]
    public UnityEvent onSimulationPaused;
    public UnityEvent onSimulationResumed;

    public bool IsWaitingForAcknowledgment { get; private set; } = false;

    private bool isPrinting = false;
    private Coroutine printErrorCoroutine;

    // Track which context errors have already been shown this session
    private HashSet<ErrorContext> shownContexts = new HashSet<ErrorContext>();

    // -----------------------------------------------------------------------
    // Contextual error banks — one pool per interaction context
    // -----------------------------------------------------------------------
    private static readonly Dictionary<ErrorContext, PrinterError[]> ContextErrors =
        new Dictionary<ErrorContext, PrinterError[]>
    {
        // ---- GAS BOTTLE ----
        { ErrorContext.Gas, new[]
        {
            new PrinterError(
                "[!]  Gas Supply — What Can Go Wrong",
                "Now that the gas bottle is connected, be aware of these real faults:\n\n" +
                "• LOW PRESSURE — If the bottle is nearly empty, O2 levels rise above 0.5% " +
                "and the machine aborts the print automatically.\n\n" +
                "• LEAK IN LINE — A loose fitting causes slow O2 rise mid-build, " +
                "contaminating every layer printed after the leak started.\n\n" +
                "• WRONG GAS — Always verify the label: Argon for titanium & steel, " +
                "Nitrogen is acceptable for some alloys but NOT for reactive metals.\n\n" +
                "Always check the pressure gauge before starting a long job.",
                "warning")
        }},

        // ---- POWDER CARTRIDGE ----
        { ErrorContext.Powder, new[]
        {
            new PrinterError(
                "[!]  Powder Supply — What Can Go Wrong",
                "The powder cartridge is loaded. Common powder-related faults:\n\n" +
                "• MOISTURE CONTAMINATION — Humid powder produces gas pores inside printed " +
                "parts. Always store powder in sealed containers with desiccant.\n\n" +
                "• CROSS-CONTAMINATION — Never mix powder batches or alloy types. " +
                "Even 1% Titanium in a Stainless Steel build changes mechanical properties.\n\n" +
                "• LOW SUPPLY — Running out of powder mid-build ruins the entire part. " +
                "Check the estimated consumption on the HMI before starting.\n\n" +
                "• CLUMPING — Recycled powder that was not sieved can clog the recoater " +
                "and cause an uneven layer — or a recoater crash.",
                "warning")
        }},

        // ---- BUILD PLATFORM ----
        { ErrorContext.Platform, new[]
        {
            new PrinterError(
                "[!]  Build Platform — What Can Go Wrong",
                "Platform inserted. This step has serious consequences if done wrong:\n\n" +
                "• IMPROPER SEATING — If the platform is not fully locked into the Z-axis " +
                "drive, it will shift during printing and ruin layer alignment.\n\n" +
                "• RECOATER COLLISION — If a part warps upward from the platform surface, " +
                "the recoater blade crashes into it. This is the most common SLM failure " +
                "and can damage the blade or the entire recoater assembly.\n\n" +
                "• DIRTY SURFACE — Residue from a previous build reduces adhesion of the " +
                "first layer. Always clean and sand the platform between jobs.\n\n" +
                "• WRONG MATERIAL — The platform material must be compatible with the " +
                "powder alloy to allow proper fusion of the first layer.",
                "error")
        }},

        // ---- DOOR CLOSING ----
        { ErrorContext.Door, new[]
        {
            new PrinterError(
                "[!]  Chamber Door — What Can Go Wrong",
                "Door is now sealed. The door interlock is a critical safety system:\n\n" +
                "• INCOMPLETE SEAL — Even a small gap allows oxygen in. " +
                "O2 above 0.1% prevents the laser from firing; above 1% it will " +
                "cause metal powder to oxidise and potentially ignite.\n\n" +
                "• INTERLOCK SENSOR FAULT — If the sensor fails to confirm 'closed', " +
                "the machine will not start the purge cycle. Check the sensor connector.\n\n" +
                "• OPENING DURING PRINT — Never open the door mid-build. " +
                "The sudden O2 influx will contaminate the current layer and may " +
                "cause a fire with reactive powders like Titanium.\n\n" +
                "Always confirm O2 < 0.1% on the HMI before pressing Start Print.",
                "error")
        }},

        // ---- LASER CALIBRATION ----
        { ErrorContext.Laser, new[]
        {
            new PrinterError(
                "[!]  Laser Calibration — Check Before You Proceed",
                "Before running calibration, be aware of these laser system faults:\n\n" +
                "• CONTAMINATED OPTICS WINDOW — Smoke residue on the protective glass " +
                "reduces laser power reaching the powder bed. Clean the window with " +
                "the supplied lens tissue before every build.\n\n" +
                "• POWER DEVIATION — If the output is >5% below target, parts will have " +
                "insufficient fusion energy, causing porosity and weak layer bonds.\n\n" +
                "• GALVANOMETER DRIFT — Mirrors can drift with temperature changes. " +
                "Always run calibration after the machine has been idle for more than 4 hours.\n\n" +
                "• LASER SAFETY — The SLM 280 2.0 uses a 700 W Class 4 fiber laser. " +
                "Never operate without the door sealed and interlocks active.",
                "warning")
        }},

        // ---- DURING PRINT (sparse, keeps simulation feel alive) ----
        { ErrorContext.Print, new[]
        {
            new PrinterError(
                "[X]  ERROR — Recoater Blade Collision",
                "The recoater blade has detected unexpected resistance and stopped.\n\n" +
                "WHAT HAPPENED:\n" +
                "A section of the part has warped or curled upward from the build platform, " +
                "protruding above the powder layer surface.\n\n" +
                "WHAT TO DO:\n" +
                "• Do NOT open the chamber until temperature drops below 40 deg C\n" +
                "• Inspect the blade for chips or deformation after cooldown\n" +
                "• Review the support structure strategy for this part geometry\n\n" +
                "This is the most common failure mode in SLM printing. " +
                "Proper support placement and scan strategy prevent most cases.",
                "error"),

            new PrinterError(
                "[!]  WARNING — Build Chamber Overtemperature",
                "Chamber temperature has exceeded the safe operating threshold.\n\n" +
                "WHAT HAPPENED:\n" +
                "Accumulated heat from previous layers has raised the ambient chamber " +
                "temperature above the preset limit. This affects the melt pool dynamics.\n\n" +
                "WHAT TO DO:\n" +
                "• The print is paused automatically\n" +
                "• Allow the chamber to cool — monitor the temperature on the HMI\n" +
                "• Check inert gas circulation (flow rate affects cooling)\n" +
                "• Consider using an inter-layer delay for thermally sensitive alloys\n\n" +
                "Inconel and Titanium builds are especially prone to heat accumulation.",
                "warning")
        }}
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -----------------------------------------------------------------------
    // Called by interaction scripts at the moment of each player action
    // -----------------------------------------------------------------------

    /// <summary>
    /// Shows one contextual educational error for the given interaction.
    /// Each context is only shown once per session so it doesn't become annoying.
    /// </summary>
    public void TriggerContextualError(ErrorContext context)
    {
        // Only show each context once per session
        if (shownContexts.Contains(context)) return;
        shownContexts.Add(context);

        if (!ContextErrors.TryGetValue(context, out var pool)) return;

        var error = pool[Random.Range(0, pool.Length)];
        ShowError(error);
    }

    // -----------------------------------------------------------------------
    // Print-time errors — called by PrintSimulationManager
    // -----------------------------------------------------------------------

    public void StartPrinting()
    {
        isPrinting = true;
        if (enablePrintErrors)
            printErrorCoroutine = StartCoroutine(PrintErrorRoutine());
    }

    public void StopPrinting()
    {
        isPrinting = false;
        if (printErrorCoroutine != null)
            StopCoroutine(printErrorCoroutine);
    }

    private IEnumerator PrintErrorRoutine()
    {
        // Wait before first error so print gets going
        yield return new WaitForSeconds(firstPrintErrorDelay);
        if (!isPrinting) yield break;

        // Trigger recoater crash
        TriggerPrintError(0);

        // Wait, then trigger overtemperature
        yield return new WaitForSeconds(Random.Range(20f, 35f));
        if (!isPrinting) yield break;

        // Only trigger second error if player has acknowledged the first
        if (!IsWaitingForAcknowledgment)
            TriggerPrintError(1);
    }

    private void TriggerPrintError(int index)
    {
        var pool = ContextErrors[ErrorContext.Print];
        if (index >= pool.Length) return;
        ShowError(pool[index]);
    }

    // -----------------------------------------------------------------------
    // Player acknowledges the error → simulation resumes
    // -----------------------------------------------------------------------

    public void AcknowledgeError()
    {
        if (!IsWaitingForAcknowledgment) return;
        IsWaitingForAcknowledgment = false;

        if (ControllerLabelDisplay.Instance != null)
            ControllerLabelDisplay.Instance.HideError();

        onSimulationResumed?.Invoke();
        Debug.Log("[ErrorSimulator] Error acknowledged — simulation resumed.");
    }

    // -----------------------------------------------------------------------
    // Internal
    // -----------------------------------------------------------------------

    private void ShowError(PrinterError error)
    {
        IsWaitingForAcknowledgment = true;

        if (ControllerLabelDisplay.Instance != null)
            ControllerLabelDisplay.Instance.ShowError(
                error.message, error.severity, showAcknowledgeButton: true);

        onSimulationPaused?.Invoke();
        Debug.Log($"[ErrorSimulator] Showing: {error.title}");
    }

    /// <summary>Resets shown-context tracking so all educational messages appear again.</summary>
    public void ResetSession()
    {
        shownContexts.Clear();
    }
}

[System.Serializable]
public class PrinterError
{
    public string title;
    public string message;
    public string severity;

    public PrinterError(string title, string message, string severity)
    {
        this.title    = title;
        this.message  = message;
        this.severity = severity;
    }
}
