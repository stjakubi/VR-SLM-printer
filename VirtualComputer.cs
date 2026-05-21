using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// The computer screen standing next to the SLM 280 printer.
/// The player walks up, selects a part to print, and presses Start.
/// During printing it shows a live progress bar and layer counter.
///
/// HOW TO SET UP IN UNITY:
/// 1. Create a cube as the computer monitor (scale ~0.6 x 0.4 x 0.05)
/// 2. Add a World Space Canvas as a child, facing forward
/// 3. Build the UI inside the canvas (see regions below for what's needed)
/// 4. Attach this script to the monitor cube or a parent empty object
/// </summary>
public class VirtualComputer : MonoBehaviour
{
    public static VirtualComputer Instance { get; private set; }

    // -----------------------------------------------------------------------
    // UI PANELS — assign these in the Inspector
    // -----------------------------------------------------------------------
    [Header("Panels (only one visible at a time)")]
    [Tooltip("Shown before printing: part selection list + Start button")]
    public GameObject selectionPanel;

    [Tooltip("Shown during printing: progress bar, layer count, status text")]
    public GameObject progressPanel;

    [Tooltip("Shown when print is done: completion message + stats")]
    public GameObject completionPanel;

    // -----------------------------------------------------------------------
    // SELECTION PANEL elements
    // -----------------------------------------------------------------------
    [Header("Selection Panel Elements")]
    public List<Button> partButtons;          // One button per printable part
    public TextMeshProUGUI selectedPartText;  // Shows which part is selected
    public Button startPrintButton;           // Grayed out until a part is selected

    // -----------------------------------------------------------------------
    // PROGRESS PANEL elements
    // -----------------------------------------------------------------------
    [Header("Progress Panel Elements")]
    public Slider progressBar;               // 0 to 1
    public TextMeshProUGUI progressPercent;  // "47%"
    public TextMeshProUGUI layerText;        // "Layer: 234 / 500"
    public TextMeshProUGUI statusText;       // "Printing...", "PAUSED — Error detected"
    public TextMeshProUGUI materialText;     // Shows material of selected part
    public TextMeshProUGUI estimatedTimeText;// "Estimated remaining: 4h 12m"

    // -----------------------------------------------------------------------
    // COMPLETION PANEL elements
    // -----------------------------------------------------------------------
    [Header("Completion Panel Elements")]
    public TextMeshProUGUI completionPartName;
    public TextMeshProUGUI completionStats;
    public Button printAgainButton;

    // -----------------------------------------------------------------------
    // Quiz panel — registered at runtime by QuizManager
    // -----------------------------------------------------------------------
    private GameObject quizPanelExternal;
    private GameObject takeQuizButtonGO;   // built in Start(), hidden until completion screen

    /// <summary>Called by QuizManager after it builds its panel on this canvas.</summary>
    public void RegisterQuizPanel(GameObject panel) => quizPanelExternal = panel;

    /// <summary>Hide all own panels and hand control to the quiz.</summary>
    public void ShowQuizPanel()
    {
        SetPanel(null);   // hides checklist, selection, progress, completion
        if (takeQuizButtonGO != null) takeQuizButtonGO.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Printable parts data
    // -----------------------------------------------------------------------
    [System.Serializable]
    public class PrintablePart
    {
        public string partName;
        public string material;
        public string materialGrade;
        public int totalLayers;
        public string simulatedPrintTime;
        public float realSimDuration;

        [Tooltip("The cooled material for this part's layers (drag Mat_Titanium etc.)")]
        public Material partMaterial;

        [Tooltip("Footprint width of this part on the build platform (X axis)")]
        public float footprintX = 0.14f;

        [Tooltip("Footprint depth of this part on the build platform (Z axis)")]
        public float footprintZ = 0.14f;

        [Tooltip("How many layers to show in the animation (10-20 recommended)")]
        public int visibleLayers = 16;
    }

    // -----------------------------------------------------------------------
    // Material gating — only parts whose materialGrade matches the currently
    // loaded powder can be printed. The rest show as locked in the selection
    // list. This models the real-world constraint that an SLM machine runs one
    // metal powder at a time: to print a different alloy you'd have to swap
    // the cartridge, purge the machine, and re-seed the chamber.
    // -----------------------------------------------------------------------
    [Header("Material Gating")]
    [Tooltip("Material grade currently loaded in the powder cartridge. Only " +
             "parts with this exact materialGrade will be printable; all " +
             "others appear as LOCKED. Default is Ti-6Al-4V (Aerospace Bracket).")]
    public string activeMaterialGrade = "Ti-6Al-4V";

    [Header("Printable Parts")]
    public List<PrintablePart> printableParts = new List<PrintablePart>
    {
        new PrintablePart {
            partName           = "Aerospace Bracket",
            material           = "Titanium Alloy",
            materialGrade      = "Ti-6Al-4V",
            totalLayers        = 420,
            simulatedPrintTime = "6h 30m",
            realSimDuration    = 60f,
            footprintX         = 0.18f,  // wide flat bracket shape
            footprintZ         = 0.08f,
            visibleLayers      = 12
        },
        new PrintablePart {
            partName           = "Industrial Gear",
            material           = "Stainless Steel",
            materialGrade      = "316L",
            totalLayers        = 280,
            simulatedPrintTime = "4h 15m",
            realSimDuration    = 45f,
            footprintX         = 0.14f,  // compact square (gear)
            footprintZ         = 0.14f,
            visibleLayers      = 14
        },
        new PrintablePart {
            partName           = "Heat Sink",
            material           = "Aluminium Alloy",
            materialGrade      = "AlSi10Mg",
            totalLayers        = 190,
            simulatedPrintTime = "2h 50m",
            realSimDuration    = 30f,
            footprintX         = 0.16f,  // wider, lower profile
            footprintZ         = 0.12f,
            visibleLayers      = 10
        },
        new PrintablePart {
            partName           = "Turbine Blade",
            material           = "Nickel Superalloy",
            materialGrade      = "Inconel 718",
            totalLayers        = 580,
            simulatedPrintTime = "9h 00m",
            realSimDuration    = 75f,
            footprintX         = 0.20f,  // long thin blade shape
            footprintZ         = 0.05f,
            visibleLayers      = 20
        },
        new PrintablePart {
            partName           = "Heat Exchanger",
            material           = "Copper Alloy",
            materialGrade      = "CuCrZr",
            totalLayers        = 340,
            simulatedPrintTime = "5h 10m",
            realSimDuration    = 50f,
            footprintX         = 0.15f,  // chunky square block
            footprintZ         = 0.15f,
            visibleLayers      = 16
        },
    };

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    private int selectedPartIndex = -1;
    public PrintablePart SelectedPart =>
        selectedPartIndex >= 0 ? printableParts[selectedPartIndex] : null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -----------------------------------------------------------------------
    // Checklist panel — built procedurally, shown before selection panel
    // -----------------------------------------------------------------------
    private GameObject checklistPanel;
    private Button     beginPrintingButton;

    // Labels for the 4 task rows — updated by RefreshChecklist()
    private TextMeshProUGUI[] taskLabels = new TextMeshProUGUI[4];

    private static readonly string[] TaskNames =
    {
        "Attach shielding gas bottle",
        "Load powder cartridge",
        "Insert build platform",
        "Calibrate laser (HMI panel)"
    };

    void Start()
    {
        BuildChecklistPanel();
        ShowChecklistPanel();

        // Wire up part selection buttons — apply material gating here.
        // Parts whose materialGrade doesn't match the currently loaded powder
        // are shown as LOCKED (grey, non-interactable, explanatory label).
        for (int i = 0; i < partButtons.Count && i < printableParts.Count; i++)
        {
            int index   = i;
            var part    = printableParts[i];
            bool locked = !IsPartPrintable(part);

            var btnLabel = partButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (btnLabel != null)
            {
                if (locked)
                {
                    btnLabel.text =
                        $"<color=#888888>{part.partName}</color>\n" +
                        $"<size=65%><color=#FF9966>[LOCKED] Requires {part.materialGrade} powder</color></size>";
                }
                else
                {
                    btnLabel.text =
                        $"{part.partName}\n" +
                        $"<size=70%>{part.material} ({part.materialGrade})</size>";
                }
            }

            if (locked)
            {
                // Disable interactivity and grey the button.
                partButtons[i].interactable = false;
                ColorBlock cb = partButtons[i].colors;
                cb.disabledColor = new Color(0.18f, 0.14f, 0.14f);
                partButtons[i].colors = cb;
            }
            else
            {
                partButtons[i].onClick.AddListener(() => SelectPart(index));
            }
        }

        if (startPrintButton != null)
            startPrintButton.onClick.AddListener(OnStartPressed);

        if (printAgainButton != null)
            printAgainButton.onClick.AddListener(ShowSelectionPanel);

        // Build the "Take Knowledge Quiz" button at scene start so the
        // GraphicRaycaster registers it — hidden until the completion panel shows.
        BuildTakeQuizButton();

        SetStartButtonEnabled(false);

        // Subscribe to WorkflowManager so checklist refreshes when tasks complete
        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnPrepTaskChanged += RefreshChecklist;

        RefreshChecklist();
    }

    void OnDestroy()
    {
        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnPrepTaskChanged -= RefreshChecklist;
    }

    // -----------------------------------------------------------------------
    // Build the checklist panel procedurally on the same canvas
    // -----------------------------------------------------------------------
    private void BuildChecklistPanel()
    {
        // Find the canvas this VirtualComputer uses (first Canvas in children)
        Canvas targetCanvas = GetComponentInChildren<Canvas>();
        if (targetCanvas == null)
        {
            Debug.LogWarning("[VirtualComputer] No Canvas found — checklist panel skipped.");
            return;
        }
        Transform canvasT = targetCanvas.transform;

        checklistPanel = new GameObject("ChecklistPanel");
        checklistPanel.transform.SetParent(canvasT, false);
        RectTransform panelRect = checklistPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // The VirtualComputer canvas has its X axis flipped (faces player).
        // Flip this panel's X scale to -1 so text reads left-to-right correctly.
        checklistPanel.transform.localScale = new Vector3(-1f, 1f, 1f);

        // Dark background
        Image bg    = checklistPanel.AddComponent<Image>();
        bg.color    = new Color(0.04f, 0.04f, 0.08f, 0.98f);
        bg.raycastTarget = false;

        // Title
        GameObject titleGO    = new GameObject("Title");
        titleGO.transform.SetParent(checklistPanel.transform, false);
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text            = "PRE-PRINT CHECKLIST";
        title.fontSize        = 20;
        title.fontStyle       = TMPro.FontStyles.Bold;
        title.color           = new Color(0.4f, 0.9f, 1f);
        title.alignment       = TMPro.TextAlignmentOptions.Center;
        title.raycastTarget   = false;
        RectTransform titleR  = titleGO.GetComponent<RectTransform>();
        titleR.anchoredPosition = new Vector2(0f, 155f);
        titleR.sizeDelta        = new Vector2(340f, 30f);

        // Subtitle
        GameObject subGO       = new GameObject("Subtitle");
        subGO.transform.SetParent(checklistPanel.transform, false);
        TextMeshProUGUI sub    = subGO.AddComponent<TextMeshProUGUI>();
        sub.text               = "Complete all tasks before starting a print job.";
        sub.fontSize           = 11;
        sub.color              = new Color(0.7f, 0.7f, 0.7f);
        sub.alignment          = TMPro.TextAlignmentOptions.Center;
        sub.raycastTarget      = false;
        RectTransform subR     = subGO.GetComponent<RectTransform>();
        subR.anchoredPosition  = new Vector2(0f, 128f);
        subR.sizeDelta         = new Vector2(340f, 22f);

        // 4 task rows
        float rowStartY = 88f;
        float rowStep   = 44f;

        for (int i = 0; i < 4; i++)
        {
            float y = rowStartY - i * rowStep;

            // Row background
            GameObject rowGO   = new GameObject($"Task_{i}");
            rowGO.transform.SetParent(checklistPanel.transform, false);
            Image rowBg        = rowGO.AddComponent<Image>();
            rowBg.color        = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            rowBg.raycastTarget = false;
            RectTransform rowR = rowGO.GetComponent<RectTransform>();
            rowR.anchoredPosition = new Vector2(0f, y);
            rowR.sizeDelta        = new Vector2(340f, 36f);

            // Task label (icon + name)
            GameObject labelGO       = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            TextMeshProUGUI label    = labelGO.AddComponent<TextMeshProUGUI>();
            label.text               = $"[ ]  {TaskNames[i]}";
            label.fontSize           = 13;
            label.color              = new Color(0.6f, 0.6f, 0.6f);
            label.alignment          = TMPro.TextAlignmentOptions.Center;
            label.raycastTarget      = false;
            RectTransform labelR     = labelGO.GetComponent<RectTransform>();
            labelR.anchorMin         = Vector2.zero;
            labelR.anchorMax         = Vector2.one;
            labelR.offsetMin         = Vector2.zero;
            labelR.offsetMax         = Vector2.zero;

            taskLabels[i] = label;
        }

        // "BEGIN PRINTING" button — locked until all tasks done
        GameObject btnGO = new GameObject("BeginButton");
        btnGO.transform.SetParent(checklistPanel.transform, false);
        Image btnImg     = btnGO.AddComponent<Image>();
        btnImg.color     = new Color(0.25f, 0.25f, 0.25f);
        beginPrintingButton = btnGO.AddComponent<Button>();
        beginPrintingButton.targetGraphic = btnImg;
        beginPrintingButton.interactable  = false;
        beginPrintingButton.onClick.AddListener(OnBeginPrintingPressed);

        ColorBlock cb        = beginPrintingButton.colors;
        cb.normalColor       = new Color(0.1f, 0.6f, 0.15f);
        cb.highlightedColor  = new Color(0.2f, 0.8f, 0.25f);
        cb.pressedColor      = new Color(0.05f, 0.4f, 0.1f);
        cb.disabledColor     = new Color(0.25f, 0.25f, 0.25f);
        cb.colorMultiplier   = 1f;
        cb.fadeDuration      = 0.1f;
        beginPrintingButton.colors = cb;

        RectTransform btnRect      = btnGO.GetComponent<RectTransform>();
        btnRect.anchoredPosition   = new Vector2(0f, -120f);
        btnRect.sizeDelta          = new Vector2(260f, 44f);

        GameObject btnLabel        = new GameObject("Label");
        btnLabel.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnText    = btnLabel.AddComponent<TextMeshProUGUI>();
        btnText.text               = "COMPLETE ALL TASKS FIRST";
        btnText.fontSize           = 14;
        btnText.fontStyle          = TMPro.FontStyles.Bold;
        btnText.color              = Color.white;
        btnText.alignment          = TMPro.TextAlignmentOptions.Center;
        btnText.raycastTarget      = false;
        RectTransform btnLabelRect = btnLabel.GetComponent<RectTransform>();
        btnLabelRect.anchorMin     = Vector2.zero;
        btnLabelRect.anchorMax     = Vector2.one;
        btnLabelRect.offsetMin     = Vector2.zero;
        btnLabelRect.offsetMax     = Vector2.zero;
    }

    // -----------------------------------------------------------------------
    // Refresh checklist rows based on current achievement state
    // -----------------------------------------------------------------------
    private void RefreshChecklist()
    {
        if (WorkflowManager.Instance == null || taskLabels[0] == null) return;

        bool[] done =
        {
            WorkflowManager.Instance.GasAttached,
            WorkflowManager.Instance.PowderLoaded,
            WorkflowManager.Instance.PlatformInserted,
            WorkflowManager.Instance.LaserCalibrated
        };

        for (int i = 0; i < 4; i++)
        {
            if (done[i])
            {
                taskLabels[i].text  = $"<color=#44FF66>[OK]</color>  {TaskNames[i]}";
                taskLabels[i].color = new Color(0.4f, 1f, 0.5f);

                // Tint row background green
                Transform rowT = taskLabels[i].transform.parent;
                Image rowBg    = rowT.GetComponent<Image>();
                if (rowBg != null) rowBg.color = new Color(0.05f, 0.18f, 0.08f, 0.9f);
            }
            else
            {
                taskLabels[i].text  = $"[ ]  {TaskNames[i]}";
                taskLabels[i].color = new Color(0.6f, 0.6f, 0.6f);

                Transform rowT = taskLabels[i].transform.parent;
                Image rowBg    = rowT.GetComponent<Image>();
                if (rowBg != null) rowBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            }
        }

        bool allDone = WorkflowManager.Instance.AllPrepComplete;
        if (beginPrintingButton != null)
        {
            beginPrintingButton.interactable = allDone;
            var label = beginPrintingButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = allDone ? "▶  START PRINTING" : "COMPLETE ALL TASKS FIRST";
        }

        // Auto-advance: once all tasks are ticked, switch to selection panel after
        // a short delay so the player sees all checkmarks before the screen changes.
        if (allDone && checklistPanel != null && checklistPanel.activeSelf)
            StartCoroutine(AutoAdvanceToSelection());
    }

    private System.Collections.IEnumerator AutoAdvanceToSelection()
    {
        yield return new WaitForSeconds(1.5f);   // brief pause so player sees all green ticks
        ShowSelectionPanel();
    }

    private void OnBeginPrintingPressed()
    {
        // Button still works as a manual trigger in case auto-advance hasn't fired yet
        ShowSelectionPanel();
    }

    /// <summary>
    /// No-op kept for backwards compatibility — the door status indicator was
    /// removed, but DoorInteractable / VRDoor still call this method when the
    /// door state changes. Leaving it as an empty stub avoids having to clean
    /// up every call site if the indicator is ever brought back later.
    /// </summary>
    public void UpdateDoorStatus(bool isOpen) { /* indicator removed */ }

    // -----------------------------------------------------------------------
    // Part selection
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns true if the given part's materialGrade matches the currently
    /// loaded powder. Used by the UI to lock out parts that require a powder
    /// the operator hasn't loaded.
    /// </summary>
    public bool IsPartPrintable(PrintablePart part)
    {
        if (part == null) return false;
        if (string.IsNullOrEmpty(activeMaterialGrade)) return true;  // no gate set
        return string.Equals(part.materialGrade, activeMaterialGrade,
                             System.StringComparison.OrdinalIgnoreCase);
    }

    public void SelectPart(int index)
    {
        if (index < 0 || index >= printableParts.Count) return;

        // Hard gate — should not happen because locked buttons are non-interactable,
        // but guard anyway in case SelectPart is invoked programmatically.
        if (!IsPartPrintable(printableParts[index]))
        {
            Debug.LogWarning($"[VirtualComputer] Ignored selection of '{printableParts[index].partName}' — " +
                             $"requires {printableParts[index].materialGrade}, but active powder is {activeMaterialGrade}.");
            return;
        }

        selectedPartIndex = index;
        PrintablePart part = printableParts[index];

        if (selectedPartText != null)
            selectedPartText.text =
                $"Selected: <b>{part.partName}</b>\n" +
                $"Material: {part.material} ({part.materialGrade})\n" +
                $"Layers: {part.totalLayers}   Est. time: {part.simulatedPrintTime}";

        SetStartButtonEnabled(true);

        // Highlight selected button, dim others
        for (int i = 0; i < partButtons.Count; i++)
        {
            ColorBlock cb = partButtons[i].colors;
            cb.normalColor = i == index
                ? new Color(0.2f, 0.6f, 1f)   // blue = selected
                : new Color(0.15f, 0.15f, 0.15f); // dark = unselected
            partButtons[i].colors = cb;
        }

        Debug.Log($"[VirtualComputer] Selected: {part.partName}");
    }

    // -----------------------------------------------------------------------
    // Start button pressed
    // -----------------------------------------------------------------------

    private void OnStartPressed()
    {
        if (selectedPartIndex < 0) return;

        // Gate: all pre-print tasks must be complete
        if (WorkflowManager.Instance != null && !WorkflowManager.Instance.AllPrepComplete)
        {
            // Send player back to checklist to see what's missing
            ShowChecklistPanel();
            return;
        }

        if (PrintSimulationManager.Instance != null)
            PrintSimulationManager.Instance.StartPrint(printableParts[selectedPartIndex]);

        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnPrintStarted();
    }

    // -----------------------------------------------------------------------
    // Panel switching — called by PrintSimulationManager
    // -----------------------------------------------------------------------

    public void ShowChecklistPanel()
    {
        SetPanel(checklistPanel);
        RefreshChecklist();
    }

    public void ShowSelectionPanel()
    {
        SetPanel(selectionPanel);
        selectedPartIndex = -1;
        SetStartButtonEnabled(false);
        if (selectedPartText != null) selectedPartText.text = "No part selected";
    }

    public void ShowProgressPanel(PrintablePart part)
    {
        SetPanel(progressPanel);
        if (materialText != null)
            materialText.text = $"{part.material} ({part.materialGrade})";
        UpdateProgress(0f, 0, part.totalLayers);
        SetStatus("Initialising... Flooding chamber with inert gas");
    }

    public void ShowCompletionPanel(PrintablePart part)
    {
        SetPanel(completionPanel);
        if (completionPartName != null)
            completionPartName.text = $"[OK] Print Complete\n{part.partName}";
        if (completionStats != null)
            completionStats.text =
                $"Material: {part.material} ({part.materialGrade})\n" +
                $"Layers printed: {part.totalLayers}\n" +
                $"Simulated print time: {part.simulatedPrintTime}\n" +
                "Status: SUCCESSFUL";

        // Show the quiz button (built during Start so the GraphicRaycaster knows about it).
        if (takeQuizButtonGO != null) takeQuizButtonGO.SetActive(true);
    }

    private void BuildTakeQuizButton()
    {
        Canvas targetCanvas = GetComponentInChildren<Canvas>();
        if (targetCanvas == null) return;

        // World Space canvas needs an event camera for mouse clicks to register.
        if (targetCanvas.worldCamera == null)
            targetCanvas.worldCamera = Camera.main;

        // Stop 3D colliders from blocking UI raycasts.
        var gr = targetCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (gr != null) gr.blockingMask = 0;

        // Match the X-flip scale used by the checklist panel on this canvas.
        Vector3 panelScale = checklistPanel != null
            ? checklistPanel.transform.localScale
            : new Vector3(-1f, 1f, 1f);

        // Container — created at scene start, hidden until completion panel is shown.
        GameObject btnGO = new GameObject("TakeQuizButton");
        btnGO.transform.SetParent(targetCanvas.transform, false);
        btnGO.transform.localScale = panelScale;
        takeQuizButtonGO = btnGO;

        Image img = btnGO.AddComponent<Image>();
        img.color = new Color(0.10f, 0.28f, 0.60f);   // blue — distinct from the green "Print Again"

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            if (QuizManager.Instance != null)
                QuizManager.Instance.StartQuiz();
        });

        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.10f, 0.28f, 0.60f);
        cb.highlightedColor = new Color(0.15f, 0.42f, 0.88f);
        cb.pressedColor     = new Color(0.06f, 0.18f, 0.40f);
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.1f;
        btn.colors = cb;

        RectTransform rt    = btnGO.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, -152f);   // below the "Print Again" button
        rt.sizeDelta        = new Vector2(240f, 36f);

        GameObject lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text          = ">> TAKE KNOWLEDGE QUIZ";
        lbl.fontSize      = 13f;
        lbl.fontStyle     = TMPro.FontStyles.Bold;
        lbl.color         = Color.white;
        lbl.alignment     = TMPro.TextAlignmentOptions.Center;
        lbl.raycastTarget = false;
        RectTransform lr  = lblGO.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;

        // Start hidden — shown only when ShowCompletionPanel() is called.
        btnGO.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Live updates during printing — called by PrintSimulationManager
    // -----------------------------------------------------------------------

    public void UpdateProgress(float value, int currentLayer, int totalLayers)
    {
        if (progressBar != null)     progressBar.value = value;
        if (progressPercent != null) progressPercent.text = $"{Mathf.RoundToInt(value * 100f)}%";
        if (layerText != null)       layerText.text = $"Layer: {currentLayer} / {totalLayers}";
    }

    public void SetStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }

    public void SetEstimatedTime(string time)
    {
        if (estimatedTimeText != null) estimatedTimeText.text = $"Est. remaining: {time}";
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void SetPanel(GameObject panel)
    {
        if (checklistPanel    != null) checklistPanel.SetActive(panel == checklistPanel);
        if (selectionPanel    != null) selectionPanel.SetActive(panel == selectionPanel);
        if (progressPanel     != null) progressPanel.SetActive(panel == progressPanel);
        if (completionPanel   != null) completionPanel.SetActive(panel == completionPanel);
        // Take Quiz button is only visible alongside the completion panel.
        if (takeQuizButtonGO  != null) takeQuizButtonGO.SetActive(panel == completionPanel);
        // Keep quiz panel under QuizManager's own control; just hide it when
        // VirtualComputer switches to one of its own panels.
        if (quizPanelExternal != null && panel != null)
            quizPanelExternal.SetActive(false);
    }

    private void SetStartButtonEnabled(bool enabled)
    {
        if (startPrintButton != null)
        {
            startPrintButton.interactable = enabled;
            var img = startPrintButton.GetComponent<Image>();
            if (img != null)
                img.color = enabled
                    ? new Color(0.1f, 0.7f, 0.2f)   // green = ready
                    : new Color(0.25f, 0.25f, 0.25f); // grey = not ready
        }
    }
}
