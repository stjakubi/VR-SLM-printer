using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// HMI control panel laser calibration sequence.
///
/// The player walks up to the Control Panel and presses the Calibrate button.
/// A progress bar fills over several seconds while educational messages explain
/// what each calibration stage does. On completion, WorkflowManager.OnLaserCalibrated()
/// is called and the Start Print step unlocks on the VirtualComputer.
///
/// HOW TO SET UP IN UNITY (auto-built by PrinterStructureBuilder):
///   1. World-space Canvas on the Control_Panel face
///   2. A Button ("Calibrate Laser")
///   3. A Slider (progress bar)
///   4. A TMP text for status messages
///   5. This script attached to the Canvas or a parent object
/// </summary>
public class LaserCalibration : MonoBehaviour
{
    [Header("UI References")]
    public Button          calibrateButton;
    public Slider          progressBar;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI titleText;

    [Header("Timing")]
    [Tooltip("Total calibration duration in seconds")]
    public float calibrationDuration = 8f;

    // -----------------------------------------------------------------------
    private bool isCalibrating  = false;
    private bool isCalibrated   = false;

    // Calibration stage messages shown during the progress sequence
    private static readonly (float progress, string message)[] Stages =
    {
        (0.00f, "Initialising laser controller..."),
        (0.15f, "Warming up fiber laser — checking output power..."),
        (0.30f, "Scanning galvanometer mirrors — X axis OK"),
        (0.45f, "Scanning galvanometer mirrors — Y axis OK"),
        (0.55f, "Measuring beam focus position at Z = 0 mm..."),
        (0.68f, "Verifying scan field geometry — 280 × 280 mm grid"),
        (0.80f, "Checking beam diameter: target < 80 um"),
        (0.90f, "Running thermal compensation model..."),
        (1.00f, "[OK] Calibration complete — laser ready")
    };

    void Start()
    {
        if (titleText != null)
            titleText.text = "LASER CALIBRATION";

        if (statusText != null)
            statusText.text = "Press CALIBRATE to begin laser alignment sequence.";

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value    = 0f;
        }

        if (calibrateButton != null)
            calibrateButton.onClick.AddListener(OnCalibratePressed);
    }

    // -----------------------------------------------------------------------
    private void OnCalibratePressed()
    {
        Debug.Log("[LaserCalibration] Button clicked.");   // confirm the click is received

        if (isCalibrating || isCalibrated) return;

#if !UNITY_EDITOR
        // Workflow gate — only enforced in real builds (not during editor testing).
        // Door must be closed (step CalibrateLaser must be ready) before calibrating.
        if (WorkflowManager.Instance != null &&
            !WorkflowManager.Instance.IsStepReady(WorkflowManager.WorkflowStep.CalibrateLaser))
        {
            if (statusText != null)
                statusText.text = "[!]  Close the build chamber door before calibrating.";
            return;
        }
#endif

        // Show laser-specific educational error before calibration runs
        if (PrinterErrorSimulator.Instance != null)
            PrinterErrorSimulator.Instance.TriggerContextualError(
                PrinterErrorSimulator.ErrorContext.Laser);

        StartCoroutine(RunCalibration());
    }

    // -----------------------------------------------------------------------
    private IEnumerator RunCalibration()
    {
        isCalibrating = true;

        if (calibrateButton != null)
        {
            calibrateButton.interactable = false;
            var img = calibrateButton.GetComponent<Image>();
            if (img != null) img.color = new Color(0.3f, 0.3f, 0.3f); // grey out
        }

        float elapsed  = 0f;
        int   stageIdx = 0;

        while (elapsed < calibrationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / calibrationDuration);

            // Update progress bar
            if (progressBar != null)
                progressBar.value = progress;

            // Advance stage messages
            while (stageIdx < Stages.Length - 1 && progress >= Stages[stageIdx + 1].progress)
                stageIdx++;

            if (statusText != null)
                statusText.text = Stages[stageIdx].message;

            yield return null;
        }

        // Finished
        isCalibrating = false;
        isCalibrated  = true;

        if (progressBar != null) progressBar.value = 1f;
        if (statusText  != null) statusText.text   = "[OK]  LASER READY  —  Open print software";

        if (titleText != null)
            titleText.text = "[OK] CALIBRATION COMPLETE";

        // Change button to show done state
        if (calibrateButton != null)
        {
            var label = calibrateButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = "CALIBRATED [OK]";
            var img = calibrateButton.GetComponent<Image>();
            if (img != null) img.color = new Color(0.1f, 0.55f, 0.15f); // green
        }

        // Show educational label on controller
        if (ControllerLabelDisplay.Instance != null)
            ControllerLabelDisplay.Instance.ShowPartLabel(
                "[OK] Laser Calibrated",
                "The galvanometer mirrors and focus optics are aligned. " +
                "Beam diameter confirmed at < 80 um. " +
                "The SLM 280 2.0 uses a 700 W IPG fiber laser — " +
                "never operate without proper laser safety eyewear.");

        // Notify workflow → Step 6 (StartPrint) unlocks
        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnLaserCalibrated();

        Debug.Log("[LaserCalibration] Calibration complete.");
    }
}
