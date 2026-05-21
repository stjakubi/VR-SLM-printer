using UnityEngine;
using TMPro;

/// <summary>
/// Manages the overall learning flow.
/// Tracks which printer parts the player has discovered and shows progress.
/// Attach to an empty GameObject named "LearningManager" in the scene.
/// </summary>
public class LearningManager : MonoBehaviour
{
    public static LearningManager Instance { get; private set; }

    [Header("Progress UI")]
    [Tooltip("Text element showing how many parts the player has discovered")]
    public TextMeshProUGUI progressText;

    [Tooltip("If true (default), totalParts is auto-counted at Start by looking at " +
             "every PrinterPartInfo component in the scene. Disable to use the manual " +
             "value below — useful if you only want a subset of parts to count toward " +
             "completion.")]
    public bool autoCountParts = true;

    [Tooltip("Total number of printer parts to discover. Ignored when " +
             "autoCountParts is true; otherwise used as-is.")]
    public int totalParts = 12;

    private int discoveredParts = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (autoCountParts)
        {
            // Count every PrinterPartInfo currently in the scene — this is the
            // ground-truth number of labelable parts after PrinterStructureBuilder
            // has wired everything up.
            PrinterPartInfo[] allParts = FindObjectsByType<PrinterPartInfo>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            totalParts = allParts.Length;
            Debug.Log($"[LearningManager] Auto-counted {totalParts} labelable parts in the scene.");
        }

        UpdateProgressUI();
    }

    /// <summary>
    /// Called automatically by PrinterPartInfo the first time each part is hovered.
    /// </summary>
    public void RegisterPartDiscovered()
    {
        if (discoveredParts >= totalParts) return;

        discoveredParts++;
        UpdateProgressUI();

        Debug.Log($"[LearningManager] Parts discovered: {discoveredParts}/{totalParts}");

        if (discoveredParts >= totalParts)
        {
            Debug.Log("[LearningManager] All parts discovered! Quiz available.");

            // Notify the player via the controller label panel.
            if (ControllerLabelDisplay.Instance != null)
                ControllerLabelDisplay.Instance.ShowError(
                    "All parts explored!\nComplete the print workflow, then take the Knowledge Quiz on the computer.", "info");
        }
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
            progressText.text = $"Parts explored: {discoveredParts} / {totalParts}";
    }
}
