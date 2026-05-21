using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to the Right Controller GameObject inside XR Origin.
/// Manages two panels:
///   1. Label panel  — shows part name/description when ray hovers a part
///   2. Error panel  — shows fault alerts during printing, pauses simulation
///                     until the player presses the Acknowledge button
/// </summary>
public class ControllerLabelDisplay : MonoBehaviour
{
    public static ControllerLabelDisplay Instance { get; private set; }

    [Header("Part Label Panel")]
    public GameObject labelPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    [Header("Error / Warning Panel")]
    public GameObject errorPanel;
    public TextMeshProUGUI errorText;

    [Tooltip("The Acknowledge button — only shown for errors that pause the simulation")]
    public GameObject acknowledgeButton;

    [Tooltip("Background image of the error panel — tinted by severity")]
    public Image errorPanelBackground;

    // Captured original tint of errorPanelBackground at Awake. We restore this
    // in HideError() so the controller canvas isn't left red/yellow when the
    // background Image lives on a parent that stays visible (which is how
    // Stefan's scene is wired).
    private Color initialBackgroundColor;
    private bool  hasInitialBackgroundColor = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Remember the un-tinted color of the error background so we can put it
        // back when the player acknowledges. Without this, the canvas keeps the
        // last severity tint forever.
        if (errorPanelBackground != null)
        {
            initialBackgroundColor    = errorPanelBackground.color;
            hasInitialBackgroundColor = true;
        }

        if (labelPanel != null)       labelPanel.SetActive(false);
        if (errorPanel != null)       errorPanel.SetActive(false);
        if (acknowledgeButton != null) acknowledgeButton.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Part label — shown when ray hovers a printer component
    // -----------------------------------------------------------------------

    public void ShowPartLabel(string partName, string description)
    {
        if (labelPanel == null) return;
        if (nameText != null)        nameText.text = partName;
        if (descriptionText != null) descriptionText.text = description;
        labelPanel.SetActive(true);
    }

    public void HidePartLabel()
    {
        if (labelPanel != null) labelPanel.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Error panel — pauses simulation, requires acknowledgment
    // -----------------------------------------------------------------------

    /// <summary>
    /// Shows an error popup.
    /// severity: "error" = red, "warning" = yellow, "info" = white/blue
    /// showAcknowledgeButton: true when the simulation must pause for the player to read it
    /// </summary>
    public void ShowError(string message, string severity = "warning",
                          bool showAcknowledgeButton = false)
    {
        if (errorPanel == null) return;

        errorPanel.SetActive(true);

        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.white; // text always white for readability
        }

        // Tint the panel background by severity
        if (errorPanelBackground != null)
        {
            switch (severity.ToLower())
            {
                case "error":
                    errorPanelBackground.color = new Color(0.6f, 0.05f, 0.05f, 0.92f); // dark red
                    break;
                case "warning":
                    errorPanelBackground.color = new Color(0.5f, 0.38f, 0f, 0.92f);    // dark yellow
                    break;
                default:
                    errorPanelBackground.color = new Color(0.05f, 0.18f, 0.45f, 0.92f); // dark blue
                    break;
            }
        }

        // Show or hide the acknowledge button
        if (acknowledgeButton != null)
            acknowledgeButton.SetActive(showAcknowledgeButton);
    }

    public void HideError()
    {
        if (errorPanel != null)        errorPanel.SetActive(false);
        if (acknowledgeButton != null) acknowledgeButton.SetActive(false);

        // Restore the background to its original tint. Required because the
        // errorPanelBackground Image is often wired to a parent (the whole
        // controller canvas) that stays visible after the error panel hides —
        // without this reset, the canvas stays yellow/red forever.
        if (errorPanelBackground != null && hasInitialBackgroundColor)
            errorPanelBackground.color = initialBackgroundColor;
    }

    // -----------------------------------------------------------------------
    // Called by the Acknowledge button's OnClick event in the Inspector
    // -----------------------------------------------------------------------

    public void OnAcknowledgePressed()
    {
        if (PrinterErrorSimulator.Instance != null)
            PrinterErrorSimulator.Instance.AcknowledgeError();
        else
            HideError(); // fallback if no simulator in scene
    }
}
