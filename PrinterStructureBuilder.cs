using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Builds a simplified SLM Solutions 280 2.0 metal 3D printer from Unity primitives.
///
/// HOW TO USE:
/// 1. Create an empty GameObject in the Hierarchy, name it "SLM_280_Printer"
/// 2. Attach this script to it
/// 3. Drag your PartLabelCanvas prefab into the "Label Canvas Prefab" slot
/// 4. Right-click the component header in the Inspector
/// 5. Click "Build SLM 280 Printer Structure"
/// 6. The full printer appears with all parts labelled automatically
/// </summary>
public class PrinterStructureBuilder : MonoBehaviour
{
    [Header("Materials (optional — assign in Inspector before building)")]
    public Material cabinetMaterial;       // Grey metal look
    public Material glassMaterial;         // Semi-transparent for windows
    public Material screenMaterial;        // Dark with slight glow for control panel
    public Material highlightMaterial;     // Assigned automatically to all parts

    // -----------------------------------------------------------------------
    // Right-click the component in Inspector → "Build SLM 280 Printer Structure"
    // -----------------------------------------------------------------------
    [ContextMenu("Build SLM 280 Printer Structure")]
    public void BuildPrinter()
    {
        // Remove any previously built structure
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);

        // ------------------------------------------------------------------
        // MAIN CABINET BODY
        // The large outer steel housing. Real size: 2.6 x 2.76 x 1.2 m
        // We use a slightly simplified scale here.
        // ------------------------------------------------------------------
        CreatePart(
            "Main_Cabinet",
            PrimitiveType.Cube,
            new Vector3(0f, 1.3f, 0f),
            new Vector3(2.0f, 2.6f, 1.0f),
            "Main Cabinet",
            "The outer steel housing of the SLM 280 2.0. Fully sealed to maintain " +
            "the inert gas atmosphere required for safe metal powder processing. " +
            "Total machine weight: approximately 1,300 kg.",
            cabinetMaterial
        );

        // ------------------------------------------------------------------
        // FRONT DOOR — with hinge pivot and clickable handle
        // ------------------------------------------------------------------
        BuildFrontDoor();

        // ------------------------------------------------------------------
        // VIEWING WINDOW
        // Small glass window to observe the build process without opening the door.
        // ------------------------------------------------------------------
        CreatePart(
            "Viewing_Window",
            PrimitiveType.Cube,
            new Vector3(0f, 1.3f, 0.56f),
            new Vector3(0.35f, 0.35f, 0.02f),
            "Viewing Window",
            "A laser-safe observation window allowing operators to monitor the " +
            "printing process without opening the build chamber. " +
            "The tinted glass filters the laser radiation. Never look directly at the laser.",
            glassMaterial
        );

        // ------------------------------------------------------------------
        // CONTROL PANEL (HMI - Human Machine Interface)
        // Touchscreen for setting up and monitoring print jobs.
        // ------------------------------------------------------------------
        CreatePart(
            "Control_Panel",
            PrimitiveType.Cube,
            new Vector3(0.72f, 1.7f, 0.53f),
            new Vector3(0.45f, 0.3f, 0.04f),
            "Control Panel (HMI)",
            "The Human Machine Interface touchscreen. Used to load print jobs, " +
            "configure laser parameters, monitor gas flow, temperature, and " +
            "layer progress. The SLM Build Processor software runs on this panel.",
            screenMaterial
        );

        // ------------------------------------------------------------------
        // POWDER SUPPLY MODULE (right side)
        // Contains the metal powder reservoir that feeds the build chamber.
        // ------------------------------------------------------------------
        CreatePart(
            "Powder_Supply",
            PrimitiveType.Cube,
            new Vector3(1.22f, 1.3f, 0f),
            new Vector3(0.42f, 2.0f, 1.0f),
            "Powder Supply Module",
            "Contains the metal powder reservoir. The powder (e.g. titanium, " +
            "stainless steel, aluminium) is stored here and fed to the build " +
            "platform layer by layer. Powder must be kept dry and uncontaminated. " +
            "This module also houses the powder sieve (PSM).",
            cabinetMaterial
        );

        // ------------------------------------------------------------------
        // OVERFLOW CONTAINER (left side)
        // Collects excess powder pushed off the build platform by the recoater.
        // ------------------------------------------------------------------
        CreatePart(
            "Overflow_Container",
            PrimitiveType.Cube,
            new Vector3(-1.22f, 1.3f, 0f),
            new Vector3(0.42f, 2.0f, 1.0f),
            "Overflow Container",
            "Collects excess metal powder that is pushed off the edges of the " +
            "build platform by the recoater blade after each layer. " +
            "This powder is recycled and sieved before reuse to maintain quality.",
            cabinetMaterial
        );

        // ------------------------------------------------------------------
        // BUILD PLATFORM
        // The surface on which parts are printed. Lowers after each layer.
        // ------------------------------------------------------------------
        CreatePart(
            "Build_Platform",
            PrimitiveType.Cube,
            new Vector3(0f, 0.4f, 0f),
            new Vector3(0.28f, 0.04f, 0.28f),
            "Build Platform",
            "A 280 x 280 mm steel plate that moves downward (Z-axis) after each " +
            "layer is fused. A new powder layer is then spread over it by the recoater. " +
            "Maximum build height: 365 mm. The platform must be preheated before printing.",
            cabinetMaterial
        );

        // ------------------------------------------------------------------
        // RECOATER BLADE
        // Spreads a thin layer of powder across the build platform.
        // ------------------------------------------------------------------
        CreatePart(
            "Recoater_Blade",
            PrimitiveType.Cube,
            new Vector3(0f, 0.55f, 0f),
            new Vector3(0.6f, 0.02f, 0.04f),
            "Recoater Blade",
            "A precision blade that sweeps a thin layer of metal powder " +
            "(typically 20–75 microns thick) across the build platform after each layer. " +
            "The SLM 280 2.0 uses patented bidirectional recoating — it deposits " +
            "powder on both the forward and return strokes, reducing build time by up to 80%.",
            cabinetMaterial
        );

        // ------------------------------------------------------------------
        // LASER SCAN HEAD (optics unit at top of build chamber)
        // Directs the high-power laser beam across the powder bed.
        // ------------------------------------------------------------------
        CreatePart(
            "Laser_Scan_Head",
            PrimitiveType.Cube,
            new Vector3(0f, 0.72f, 0f),
            new Vector3(0.2f, 0.12f, 0.2f),
            "Laser Scan Head",
            "Houses the 3D scan optics that direct the fiber laser beam across " +
            "the powder bed. The SLM 280 2.0 can have up to two 700W fiber lasers, " +
            "each controlled independently. The lasers melt the powder at temperatures " +
            "exceeding 1,500°C. Never operate without protective eyewear near the machine.",
            cabinetMaterial
        );

        // ------------------------------------------------------------------
        // FILTER UNIT (top of machine)
        // Filters smoke and condensate produced during the melting process.
        // ------------------------------------------------------------------
        CreatePart(
            "Filter_Unit",
            PrimitiveType.Cube,
            new Vector3(0f, 2.72f, 0f),
            new Vector3(2.0f, 0.22f, 1.0f),
            "Filter Unit",
            "A permanent filter module that cleans the inert gas circulating " +
            "inside the build chamber. It removes condensate, smoke, and spatter " +
            "produced during laser melting. The filter must be replaced periodically " +
            "to maintain print quality and machine safety.",
            cabinetMaterial
        );

        // ------------------------------------------------------------------
        // INERT GAS INLET (small cylinder, front lower area)
        // ------------------------------------------------------------------
        CreatePart(
            "Gas_Inlet",
            PrimitiveType.Cylinder,
            new Vector3(-0.6f, 0.15f, 0.53f),
            new Vector3(0.08f, 0.12f, 0.08f),
            "Inert Gas Inlet",
            "The inlet port for argon or nitrogen gas. The build chamber is " +
            "flooded with inert gas before printing begins to displace oxygen. " +
            "Oxygen levels must drop below 0.1% to prevent oxidation of the metal powder.",
            cabinetMaterial
        );

        // ------------------------------------------------------------------
        // INERT GAS OUTLET
        // ------------------------------------------------------------------
        CreatePart(
            "Gas_Outlet",
            PrimitiveType.Cylinder,
            new Vector3(0.6f, 0.15f, 0.53f),
            new Vector3(0.08f, 0.12f, 0.08f),
            "Inert Gas Outlet",
            "The outlet port where inert gas exits the build chamber after " +
            "passing through the filter unit. The gas is continuously circulated " +
            "during printing to remove spatter and condensate from the optical path.",
            cabinetMaterial
        );

        // Remove collider from Main_Cabinet so player can reach inside the chamber
        // The visual mesh stays — only the physics blocker is removed
        Transform cabinet = transform.Find("Main_Cabinet");
        if (cabinet != null)
        {
            Collider col = cabinet.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        // ------------------------------------------------------------------
        // LASER CALIBRATION UI on the HMI Control Panel
        // ------------------------------------------------------------------
        BuildCalibrationPanel();

        // ------------------------------------------------------------------
        // GAS BOTTLE (grabbable prop placed near the machine)
        // ------------------------------------------------------------------
        BuildGasBottle();

        // ------------------------------------------------------------------
        // POWDER CARTRIDGE (grabbable prop placed on top of powder supply)
        // ------------------------------------------------------------------
        BuildPowderCartridge();

        // ------------------------------------------------------------------
        // BUILD PLATFORM (grabbable prop — player inserts it into the chamber)
        // ------------------------------------------------------------------
        BuildBuildPlatform();

        // ------------------------------------------------------------------
        // REMOVAL STATION — workbench socket where the finished platform goes
        // ------------------------------------------------------------------
        BuildRemovalStation();

        // ------------------------------------------------------------------
        // QUIZ TABLET — kiosk to the left of the printer, same canvas setup
        // as CalibrationPanel so mouse clicks work in the Editor
        // ------------------------------------------------------------------
        BuildQuizTablet();

        Debug.Log("[PrinterStructureBuilder] SLM 280 2.0 structure built successfully!");
    }

    // -----------------------------------------------------------------------
    // Creates the gas bottle prop and wires the XRSocketInteractor on the inlet
    // -----------------------------------------------------------------------
    // -----------------------------------------------------------------------
    // Builds the front door with a hinge pivot and interactive handle
    // -----------------------------------------------------------------------
    // -----------------------------------------------------------------------
    // Builds the laser calibration HMI panel on the Control_Panel face
    // -----------------------------------------------------------------------
    private void BuildCalibrationPanel()
    {
        Transform panelTransform = transform.Find("Control_Panel");
        if (panelTransform == null)
        {
            Debug.LogWarning("[PrinterStructureBuilder] Control_Panel not found — calibration UI skipped.");
            return;
        }

        // ---- Canvas ----
        // Parented to Control_Panel so it moves with the panel.
        // Rotated 180° Y so the front face (readable side) faces the player (+Z).
        // GraphicRaycaster = mouse clicks in editor.
        // TrackedDeviceGraphicRaycaster = clicks from XR controller rays.
        GameObject canvasGO = new GameObject("CalibrationCanvas");
        canvasGO.transform.SetParent(panelTransform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        canvasGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasGO.transform.localScale    = new Vector3(0.003f, 0.003f, 0.003f);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;          // explicit event camera for mouse raycasting

        var raycaster = canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        raycaster.blockingMask = 0;               // don't let 3-D colliders block UI raycasts
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();   // VR controller ray clicks

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300f, 280f);

        // ---- Background ----
        GameObject bg    = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImg      = bg.AddComponent<Image>();
        bgImg.color      = new Color(0.05f, 0.05f, 0.1f, 0.95f);
        bgImg.raycastTarget = false;              // not interactive — don't intercept clicks
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ---- Title ----
        GameObject titleGO       = new GameObject("TitleText");
        titleGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI title    = titleGO.AddComponent<TextMeshProUGUI>();
        title.text               = "LASER CALIBRATION";
        title.fontSize           = 22;
        title.color              = new Color(0.4f, 0.9f, 1f);
        title.alignment          = TMPro.TextAlignmentOptions.Center;
        title.fontStyle          = TMPro.FontStyles.Bold;
        title.raycastTarget      = false;
        RectTransform titleRect  = titleGO.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0f, 110f);
        titleRect.sizeDelta        = new Vector2(280f, 35f);

        // ---- Status text ----
        GameObject statusGO       = new GameObject("StatusText");
        statusGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI status    = statusGO.AddComponent<TextMeshProUGUI>();
        status.text               = "Press CALIBRATE to begin.";
        status.fontSize           = 14;
        status.color              = Color.white;
        status.alignment          = TMPro.TextAlignmentOptions.Center;
        status.raycastTarget      = false;
        RectTransform statusRect  = statusGO.GetComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0f, 40f);
        statusRect.sizeDelta        = new Vector2(280f, 80f);

        // ---- Progress bar ----
        GameObject sliderGO = new GameObject("ProgressBar");
        sliderGO.transform.SetParent(canvasGO.transform, false);
        Slider slider       = sliderGO.AddComponent<Slider>();
        slider.minValue     = 0f;
        slider.maxValue     = 1f;
        slider.value        = 0f;
        slider.interactable = false;              // display only — player can't drag it
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0f, -30f);
        sliderRect.sizeDelta        = new Vector2(260f, 20f);

        GameObject sliderBg  = new GameObject("Background");
        sliderBg.transform.SetParent(sliderGO.transform, false);
        Image sliderBgImg    = sliderBg.AddComponent<Image>();
        sliderBgImg.color    = new Color(0.15f, 0.15f, 0.2f);
        sliderBgImg.raycastTarget = false;
        RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = Vector2.zero;
        sliderBgRect.anchorMax = Vector2.one;
        sliderBgRect.offsetMin = Vector2.zero;
        sliderBgRect.offsetMax = Vector2.zero;

        GameObject fillArea  = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5f, 0f);
        fillAreaRect.offsetMax = new Vector2(-5f, 0f);

        GameObject fill  = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg    = fill.AddComponent<Image>();
        fillImg.color    = new Color(0.2f, 0.7f, 1f);
        fillImg.raycastTarget = false;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect      = fillRect;
        slider.targetGraphic = sliderBgImg;

        // ---- Calibrate Button ----
        GameObject btnGO = new GameObject("CalibrateButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        Image btnImg     = btnGO.AddComponent<Image>();
        btnImg.color     = new Color(0.1f, 0.4f, 0.8f);
        btnImg.raycastTarget = true;             // must be true — this is what catches the click
        Button btn       = btnGO.AddComponent<Button>();
        btn.targetGraphic   = btnImg;            // explicit — required for hover/click feedback
        btn.interactable    = true;
        RectTransform btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0f, -90f);
        btnRect.sizeDelta        = new Vector2(200f, 45f);

        // Button colour tint block (normal=blue, hover=lighter, pressed=dark)
        ColorBlock cb        = btn.colors;
        cb.normalColor       = new Color(0.1f,  0.4f,  0.8f);
        cb.highlightedColor  = new Color(0.25f, 0.55f, 1.0f);
        cb.pressedColor      = new Color(0.05f, 0.25f, 0.5f);
        cb.selectedColor     = cb.normalColor;
        cb.disabledColor     = new Color(0.3f,  0.3f,  0.3f);
        cb.colorMultiplier   = 1f;
        cb.fadeDuration      = 0.1f;
        btn.colors           = cb;

        GameObject btnLabel  = new GameObject("Label");
        btnLabel.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnText = btnLabel.AddComponent<TextMeshProUGUI>();
        btnText.text         = "CALIBRATE LASER";
        btnText.fontSize     = 18;
        btnText.color        = Color.white;
        btnText.alignment    = TMPro.TextAlignmentOptions.Center;
        btnText.fontStyle    = TMPro.FontStyles.Bold;
        btnText.raycastTarget = false;           // label must NOT intercept clicks — button Image does
        RectTransform btnLabelRect = btnLabel.GetComponent<RectTransform>();
        btnLabelRect.anchorMin = Vector2.zero;
        btnLabelRect.anchorMax = Vector2.one;
        btnLabelRect.offsetMin = Vector2.zero;
        btnLabelRect.offsetMax = Vector2.zero;

        // ---- Wire LaserCalibration script ----
        LaserCalibration cal = canvasGO.AddComponent<LaserCalibration>();
        cal.calibrateButton  = btn;
        cal.progressBar      = slider;
        cal.statusText       = status;
        cal.titleText        = title;

        Debug.Log("[PrinterStructureBuilder] CalibrationCanvas rebuilt cleanly on Control_Panel.");
    }

    // -----------------------------------------------------------------------
    // Builds a quiz kiosk (pole + screen + canvas) to the left of the printer.
    // Canvas setup is identical to CalibrationCanvas — confirmed to work with
    // mouse clicks in the Editor (worldCamera + GraphicRaycaster blockingMask=0).
    // -----------------------------------------------------------------------
    private void BuildQuizTablet()
    {
        // Root empty GO — positioned to the left of the Overflow Container
        GameObject quizRoot = new GameObject("QuizTablet");
        quizRoot.transform.SetParent(transform, false);
        quizRoot.transform.localPosition = new Vector3(-2.8f, 0f, 0.3f);

        // Shared dark material (URP-compatible — Standard shader appears pink in URP)
        Material darkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        darkMat.color = new Color(0.15f, 0.15f, 0.18f);

        // ---- Base ----
        GameObject baseGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseGO.name = "QuizBase";
        baseGO.transform.SetParent(quizRoot.transform, false);
        baseGO.transform.localPosition = Vector3.zero;
        baseGO.transform.localScale    = new Vector3(0.4f, 0.03f, 0.4f);
        DestroyImmediate(baseGO.GetComponent<Collider>());
        baseGO.GetComponent<Renderer>().material = darkMat;

        // ---- Pole ----
        GameObject poleGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        poleGO.name = "QuizPole";
        poleGO.transform.SetParent(quizRoot.transform, false);
        poleGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        poleGO.transform.localScale    = new Vector3(0.04f, 0.55f, 0.04f);
        DestroyImmediate(poleGO.GetComponent<Collider>());
        poleGO.GetComponent<Renderer>().material = darkMat;

        // ---- Screen ----
        float screenCY = 1.1f + 0.2f;   // pole top + half screen height
        GameObject screenGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screenGO.name = "QuizScreen";
        screenGO.transform.SetParent(quizRoot.transform, false);
        screenGO.transform.localPosition = new Vector3(0f, screenCY, 0f);
        screenGO.transform.localScale    = new Vector3(0.55f, 0.40f, 0.04f);
        DestroyImmediate(screenGO.GetComponent<Collider>());
        Material screenMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        screenMat.color = new Color(0.05f, 0.05f, 0.08f);
        screenGO.GetComponent<Renderer>().material = screenMat;

        // ---- Canvas (IDENTICAL setup to CalibrationCanvas) ----
        GameObject canvasGO = new GameObject("QuizCanvas");
        canvasGO.transform.SetParent(quizRoot.transform, false);
        canvasGO.transform.localPosition = new Vector3(-2.8f, screenCY, 0.025f);
        canvasGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // faces player (+Z)
        canvasGO.transform.localScale    = new Vector3(0.003f, 0.003f, 0.003f);

        Canvas canvas      = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;           // required for mouse raycasting

        var raycaster          = canvasGO.AddComponent<GraphicRaycaster>();
        raycaster.blockingMask = 0;                 // don't let 3D colliders block clicks
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();   // VR controller ray clicks

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta     = new Vector2(300f, 280f);

        // ---- Background (raycastTarget = false — must not swallow clicks) ----
        GameObject bgGO     = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg         = bgGO.AddComponent<Image>();
        bgImg.color         = new Color(0.04f, 0.04f, 0.08f, 0.98f);
        bgImg.raycastTarget = false;
        RectTransform bgR   = bgGO.GetComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = Vector2.zero; bgR.offsetMax = Vector2.zero;

        // ---- Header text ----
        TextMeshProUGUI headerTMP = MakeCanvasText(canvasGO.transform, "HeaderText",
            "KNOWLEDGE QUIZ", 18f, TMPro.FontStyles.Bold,
            new Color(0.4f, 0.9f, 1f), 0f, 120f, 280f, 28f);

        // ---- Body text (question / idle description) ----
        TextMeshProUGUI bodyTMP = MakeCanvasText(canvasGO.transform, "BodyText",
            "", 11f, TMPro.FontStyles.Normal,
            Color.white, 0f, 55f, 278f, 80f);
        bodyTMP.enableWordWrapping = true;

        // ---- Four answer buttons (A / B / C / D) ----
        string[] letters = { "A", "B", "C", "D" };
        float[]  btnYs   = { 0f, -28f, -56f, -84f };
        Button[]           answerBtns   = new Button[4];
        TextMeshProUGUI[]  answerLabels = new TextMeshProUGUI[4];
        Image[]            answerImages = new Image[4];

        for (int i = 0; i < 4; i++)
        {
            GameObject btnGO   = new GameObject($"Answer_{letters[i]}");
            btnGO.transform.SetParent(canvasGO.transform, false);

            Image btnImg       = btnGO.AddComponent<Image>();
            btnImg.color       = new Color(0.12f, 0.12f, 0.22f);
            btnImg.raycastTarget = true;           // MUST be true — catches the click

            Button btn         = btnGO.AddComponent<Button>();
            btn.targetGraphic  = btnImg;           // explicit — needed for hover feedback
            btn.interactable   = true;

            ColorBlock cb      = btn.colors;
            cb.normalColor     = new Color(0.12f, 0.12f, 0.22f);
            cb.highlightedColor = new Color(0.20f, 0.40f, 0.80f);
            cb.pressedColor    = new Color(0.08f, 0.25f, 0.55f);
            cb.selectedColor   = cb.normalColor;
            cb.disabledColor   = new Color(0.08f, 0.08f, 0.12f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration    = 0.1f;
            btn.colors = cb;

            RectTransform br   = btnGO.GetComponent<RectTransform>();
            br.anchoredPosition = new Vector2(0f, btnYs[i]);
            br.sizeDelta        = new Vector2(278f, 24f);

            // Label text — raycastTarget = false so it doesn't block clicks
            GameObject lblGO      = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            TextMeshProUGUI lbl   = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text              = $"{letters[i]}.";
            lbl.fontSize          = 10f;
            lbl.color             = Color.white;
            lbl.alignment         = TMPro.TextAlignmentOptions.MidlineLeft;
            lbl.raycastTarget     = false;         // must NOT intercept clicks
            lbl.enableWordWrapping = false;
            RectTransform lr      = lblGO.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(8f, 0f); lr.offsetMax = new Vector2(-4f, 0f);

            answerBtns[i]   = btn;
            answerLabels[i] = lbl;
            answerImages[i] = btnImg;
        }

        // ---- Feedback text ----
        TextMeshProUGUI feedbackTMP = MakeCanvasText(canvasGO.transform, "FeedbackText",
            "", 9f, TMPro.FontStyles.Normal,
            Color.white, 0f, -112f, 278f, 34f);
        feedbackTMP.enableWordWrapping = true;

        // ---- START QUIZ button ----
        GameObject startBtn = MakeCanvasButton(canvasGO.transform, "StartBtn",
            "START QUIZ", 0f, -100f, 200f, 40f,
            new Color(0.10f, 0.50f, 0.15f), new Color(0.15f, 0.70f, 0.22f), 18f);

        // ---- NEXT button ----
        GameObject nextBtn = MakeCanvasButton(canvasGO.transform, "NextBtn",
            "NEXT >>", 0f, -128f, 160f, 30f,
            new Color(0.10f, 0.35f, 0.65f), new Color(0.15f, 0.50f, 0.90f), 14f);

        // ---- TRY AGAIN button ----
        GameObject retryBtn = MakeCanvasButton(canvasGO.transform, "RetryBtn",
            "TRY AGAIN", 0f, -100f, 200f, 40f,
            new Color(0.10f, 0.35f, 0.65f), new Color(0.15f, 0.50f, 0.90f), 16f);

        // ---- Wire QuizTablet script (same pattern as LaserCalibration) ----
        QuizTablet qt        = quizRoot.AddComponent<QuizTablet>();
        qt.headerText        = headerTMP;
        qt.bodyText          = bodyTMP;
        qt.feedbackText      = feedbackTMP;
        qt.answerBtns        = answerBtns;
        qt.answerLabels      = answerLabels;
        qt.answerImages      = answerImages;
        qt.startBtnGO        = startBtn;
        qt.nextBtnGO         = nextBtn;
        qt.retryBtnGO        = retryBtn;

        Debug.Log("[PrinterStructureBuilder] QuizTablet built successfully.");
    }

    // Shared helper — creates a TextMeshProUGUI on a canvas (raycastTarget always false)
    private static TextMeshProUGUI MakeCanvasText(Transform parent, string goName,
        string text, float fontSize, TMPro.FontStyles style, Color color,
        float x, float y, float w, float h)
    {
        GameObject go       = new GameObject(goName);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = fontSize;
        tmp.fontStyle       = style;
        tmp.color           = color;
        tmp.alignment       = TMPro.TextAlignmentOptions.Center;
        tmp.raycastTarget   = false;
        tmp.enableWordWrapping = true;
        RectTransform rt    = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
        return tmp;
    }

    // Shared helper — creates a Button on a canvas using the CalibrationCanvas pattern
    private static GameObject MakeCanvasButton(Transform parent, string goName,
        string labelText, float x, float y, float w, float h,
        Color normalCol, Color hoverCol, float fontSize = 14f)
    {
        GameObject go   = new GameObject(goName);
        go.transform.SetParent(parent, false);

        Image img       = go.AddComponent<Image>();
        img.color       = normalCol;
        img.raycastTarget = true;              // MUST be true — catches the click

        Button btn      = go.AddComponent<Button>();
        btn.targetGraphic = img;               // explicit — required for hover feedback
        btn.interactable  = true;

        ColorBlock cb   = btn.colors;
        cb.normalColor  = normalCol;
        cb.highlightedColor = hoverCol;
        cb.pressedColor = normalCol * 0.6f;
        cb.selectedColor = normalCol;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        RectTransform rt    = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);

        GameObject lblGO    = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text            = labelText;
        lbl.fontSize        = fontSize;
        lbl.fontStyle       = TMPro.FontStyles.Bold;
        lbl.color           = Color.white;
        lbl.alignment       = TMPro.TextAlignmentOptions.Center;
        lbl.raycastTarget   = false;           // must NOT intercept clicks
        RectTransform lr    = lblGO.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;

        return go;
    }

    private void BuildFrontDoor()
    {
        // Hinge pivot at the LEFT edge of the door
        GameObject hinge = new GameObject("Front_Door_Hinge");
        hinge.transform.SetParent(transform);
        hinge.transform.localPosition = new Vector3(-0.6f, 1.1f, 0.53f);
        hinge.transform.localRotation = Quaternion.identity;
        hinge.transform.localScale    = Vector3.one;

        // Door panel — child of hinge, offset so left edge is at hinge pivot
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Front_Door";
        door.transform.SetParent(hinge.transform);
        door.transform.localPosition = new Vector3(0.6f, 0f, 0f);
        door.transform.localScale    = new Vector3(1.2f, 1.6f, 0.06f);
        door.transform.localRotation = Quaternion.identity;

        if (cabinetMaterial != null)
            door.GetComponent<Renderer>().material = cabinetMaterial;

        // Kinematic Rigidbody — required by XRSimpleInteractable
        Rigidbody rb = door.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // XRSimpleInteractable — player grabs the door directly and pushes/pulls it
        door.AddComponent<XRSimpleInteractable>();

        // PrinterPartInfo for the hover label
        PrinterPartInfo info = door.AddComponent<PrinterPartInfo>();
        info.partName        = "Front Door";
        info.partDescription = "Grab and push/pull to open or close. " +
                               "The safety interlock prevents the laser from firing " +
                               "while this door is open.";

        // VRDoor drives the hinge rotation from controller position
        VRDoor vrDoor = door.AddComponent<VRDoor>();
        vrDoor.hingeTransform = hinge.transform;
        vrDoor.closedAngle    =   0f;
        vrDoor.openAngle      = -90f;

        // Handle — visual only, no separate interactable (door panel is the interactable)
        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Door_Handle";
        handle.transform.SetParent(door.transform);
        handle.transform.localPosition = new Vector3(0.38f, -0.06f, 0.55f);
        handle.transform.localScale    = new Vector3(0.05f, 0.1f, 0.05f);
        handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Remove handle collider so it doesn't interfere with the door's collider
        DestroyImmediate(handle.GetComponent<Collider>());

        Renderer hr = handle.GetComponent<Renderer>();
        if (hr != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f));
            mat.SetFloat("_Metallic",   0.95f);
            mat.SetFloat("_Smoothness", 0.9f);
            hr.material = mat;
        }

        Debug.Log("[PrinterStructureBuilder] Front door (VR grab) created.");
    }

    private void BuildGasBottle()
    {
        XRSocketInteractor socket = null;

        // --- Gas Socket on the Gas Inlet ---
        Transform inletTransform = transform.Find("Gas_Inlet");
        if (inletTransform != null)
        {
            socket = inletTransform.gameObject.AddComponent<XRSocketInteractor>();
            socket.interactionLayers = 1 << 1;  // bit 1 = gas bottle only (prevents cross-snapping)

            SphereCollider trigger = inletTransform.gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius    = 0.25f;

            SocketVisualHint.CreateOnSocket(
                socket,
                PrimitiveType.Cylinder,
                new Vector3(0.12f, 0.35f, 0.12f),
                new Color(0.4f, 0.8f, 0.4f, 0.3f),
                new Color(0.3f, 1.0f, 0.5f, 0.75f)
            );

            Debug.Log("[PrinterStructureBuilder] XRSocketInteractor + ghost hint added to Gas_Inlet.");
        }
        else
        {
            Debug.LogWarning("[PrinterStructureBuilder] Could not find Gas_Inlet.");
        }

        // --- Gas Bottle prop ---
        GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bottle.name = "GasBottle";
        bottle.transform.SetParent(transform);
        bottle.transform.localPosition = new Vector3(-1.8f, 0.35f, 0.4f);
        bottle.transform.localScale    = new Vector3(0.12f, 0.35f, 0.12f);
        bottle.transform.localRotation = Quaternion.identity;

        Renderer r = bottle.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.35f, 0.5f, 0.35f));
            mat.SetFloat("_Metallic",   0.8f);
            mat.SetFloat("_Smoothness", 0.6f);
            r.material = mat;
        }

        Rigidbody rb = bottle.AddComponent<Rigidbody>();
        rb.mass        = 2f;
        rb.isKinematic = false;  // Must be non-kinematic so socket can take ownership on release
        rb.useGravity  = true;

        var grab = bottle.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.movementType      = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous;
        grab.interactionLayers = (1 << 0) | (1 << 1);  // bit 0 = controller can grab, bit 1 = gas socket can snap

        // Wire socket reference directly — no tags needed
        GasBottle gasBottle = bottle.AddComponent<GasBottle>();
        gasBottle.gasSocket = socket;

        Debug.Log("[PrinterStructureBuilder] Gas bottle created.");
    }

    // -----------------------------------------------------------------------
    // Creates the powder cartridge prop and socket on the Powder Supply Module
    // -----------------------------------------------------------------------
    private void BuildPowderCartridge()
    {
        XRSocketInteractor socket = null;

        // --- Powder Socket on the Powder Supply Module ---
        Transform supplyTransform = transform.Find("Powder_Supply");
        if (supplyTransform != null)
        {
            GameObject socketAnchor = new GameObject("PowderSocket_Anchor");
            socketAnchor.transform.SetParent(supplyTransform);
            socketAnchor.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            socketAnchor.transform.localRotation = Quaternion.identity;
            socketAnchor.transform.localScale    = Vector3.one;

            socket = socketAnchor.AddComponent<XRSocketInteractor>();
            socket.interactionLayers = 1 << 2;  // bit 2 = powder cartridge only (prevents cross-snapping)

            SphereCollider trigger = socketAnchor.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius    = 0.25f;

            SocketVisualHint.CreateOnSocket(
                socket,
                PrimitiveType.Cube,
                new Vector3(0.18f, 0.22f, 0.18f),
                new Color(0.8f, 0.7f, 0.2f, 0.3f),
                new Color(1.0f, 0.9f, 0.3f, 0.75f)
            );

            Debug.Log("[PrinterStructureBuilder] Powder socket + ghost hint added to Powder_Supply.");
        }
        else
        {
            Debug.LogWarning("[PrinterStructureBuilder] Could not find Powder_Supply — powder socket not added.");
        }

        // --- Powder Cartridge prop (sitting on top of the powder supply) ---
        GameObject cartridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cartridge.name = "PowderCartridge";
        cartridge.transform.SetParent(transform);
        // Placed on the floor next to the supply module, ready to be picked up
        cartridge.transform.localPosition = new Vector3(1.5f, 0.11f, 0.6f);
        cartridge.transform.localScale    = new Vector3(0.18f, 0.22f, 0.18f);
        cartridge.transform.localRotation = Quaternion.identity;

        // Silver-grey with a label-like yellow band to suggest powder inside
        Renderer r = cartridge.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.82f, 0.78f, 0.22f)); // gold/yellow
            mat.SetFloat("_Metallic",   0.5f);
            mat.SetFloat("_Smoothness", 0.4f);
            r.material = mat;
        }

        Rigidbody rb = cartridge.AddComponent<Rigidbody>();
        rb.mass        = 3f;
        rb.isKinematic = false;  // Must be non-kinematic so socket can take ownership on release
        rb.useGravity  = true;

        var grab = cartridge.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.movementType      = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous;
        grab.interactionLayers = (1 << 0) | (1 << 2);  // bit 0 = controller can grab, bit 2 = powder socket can snap

        // Wire socket reference directly — no tags needed
        PowderCartridge powderCartridge = cartridge.AddComponent<PowderCartridge>();
        powderCartridge.powderSocket = socket;

        Debug.Log("[PrinterStructureBuilder] Powder cartridge created beside powder supply.");
    }

    // -----------------------------------------------------------------------
    // Creates the chamber snap socket and wires it to the unified Build_Platform.
    // The socket is anchored to Chamber_Floor of the imported model so it always
    // lands exactly where the platform should sit regardless of model scaling.
    //
    // No primitive "prop" cube is created — IntegrateModelBuildPlatform already
    // made SLM_BuildPlatform the single unified grabbable/simulation/removal object.
    // -----------------------------------------------------------------------
    private void BuildBuildPlatform()
    {
        // --- Chamber snap socket — position tuned by hand on 2026-04-20 ---
        // Stefan confirmed these exact local coordinates land the platform flush on
        // the chamber floor. Do not auto-derive from Chamber_Floor; that was giving
        // a wrong Y. Keep these values unless the printer root rotation changes.
        GameObject socketAnchor = new GameObject("ChamberSocket_Anchor");
        socketAnchor.transform.SetParent(transform);
        socketAnchor.transform.localPosition = new Vector3(0.518f, 0.8525f, -0.0399f);
        socketAnchor.transform.localRotation = Quaternion.Euler(0f, -180f, 0f);
        socketAnchor.transform.localScale    = Vector3.one;

        XRSocketInteractor socket = socketAnchor.AddComponent<XRSocketInteractor>();
        socket.interactionLayers = 1 << 3; // bit 3 = only Build_Platform can snap here

        SphereCollider trigger = socketAnchor.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius    = 0.45f;         // forgiving — player just needs to get close

        // Ghost hint — flat square matching the build platform footprint.
        // Latched: once the player snaps the platform in, the ghost stays
        // hidden for the rest of the print cycle. The print simulation moves
        // the platform out of the socket trigger volume, which would otherwise
        // make the ghost reappear mid-print.
        SocketVisualHint.CreateOnSocket(
            socket,
            PrimitiveType.Cube,
            new Vector3(0.28f, 0.02f, 0.28f),
            new Color(0.3f, 0.6f, 1.0f, 0.3f),   // blue ghost idle
            new Color(0.4f, 0.8f, 1.0f, 0.75f),  // bright blue highlight on proximity
            keepHiddenAfterFirstSnap: true
        );

        // --- Wire the socket into the unified Build_Platform's state machine ---
        Transform bp = transform.Find("Build_Platform");
        if (bp != null)
        {
            BuildPlatform bpComp = bp.GetComponent<BuildPlatform>();
            if (bpComp != null)
            {
                bpComp.chamberSocket = socket;
                Debug.Log("[PrinterStructureBuilder] Chamber socket wired to unified Build_Platform.");
            }
            else
            {
                Debug.LogWarning("[PrinterStructureBuilder] BuildPlatform component missing on Build_Platform — rebuild from imported model.");
            }
        }
        else
        {
            Debug.LogWarning("[PrinterStructureBuilder] Build_Platform not found — socket unwired.");
        }
    }

    // -----------------------------------------------------------------------
    // Walks every Renderer in the printer hierarchy and adds a plain
    // MeshCollider to any GameObject that doesn't already have a Collider.
    //
    // These extra colliders aren't interactable (no XRSimpleInteractable, no
    // PrinterPartInfo) — they exist purely to BLOCK the XR controller ray at
    // wall surfaces so the player can't read part info through walls.
    // Interactable parts (which already have their own colliders set up by
    // AddHoverInfo / WireHoverOnSingleMesh / IntegrateModel*) are skipped.
    // -----------------------------------------------------------------------
    private void AddRayBlockingCollidersToAllMeshes()
    {
        Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true);
        int added = 0;
        foreach (Renderer r in renderers)
        {
            GameObject go = r.gameObject;

            // Skip if it already has any kind of collider
            if (go.GetComponent<Collider>() != null) continue;

            // Skip non-mesh renderers (sprites, UI canvases, particle systems, etc.)
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.convex = false;   // no Rigidbody on these — non-convex is fine and more accurate
            added++;
        }

        Debug.Log($"[PrinterStructureBuilder] Added ray-blocking MeshColliders to {added} previously uncollided mesh(es).");
    }

    // -----------------------------------------------------------------------
    // Builds the removal station workbench and wires the Build_Platform for removal
    // -----------------------------------------------------------------------
    private void BuildRemovalStation()
    {
        // --- Workbench table ---
        GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "RemovalStation_Table";
        table.transform.SetParent(transform);
        // Slightly thicker top (8 cm) and raised so the platform has a real surface to rest on.
        table.transform.localPosition = new Vector3(-2.2f, 0.90f, 0f);
        table.transform.localScale    = new Vector3(0.8f, 0.08f, 0.8f);
        table.transform.localRotation = Quaternion.identity;

        // PHYSICAL TABLETOP — make sure the BoxCollider that CreatePrimitive added is
        // present, solid (not a trigger), and on a layer that collides with the
        // build platform. Without this, the platform falls straight through the table.
        BoxCollider tableCollider = table.GetComponent<BoxCollider>();
        if (tableCollider == null) tableCollider = table.AddComponent<BoxCollider>();
        tableCollider.isTrigger = false;
        table.layer = LayerMask.NameToLayer("Default");

        Renderer tr = table.GetComponent<Renderer>();
        if (tr != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.55f, 0.42f, 0.28f)); // wood-ish brown
            mat.SetFloat("_Metallic",   0.0f);
            mat.SetFloat("_Smoothness", 0.3f);
            tr.material = mat;
        }

        // Table legs (visual only). Legs span from floor (~y=0) to just under the
        // tabletop bottom. Local-Y values are in table-local space (table scaleY = 0.08),
        // so a leg of localScale Y = 10.75 → world height ≈ 0.86 m, and localPosition
        // Y = -5.4 → world center ≈ y = 0.47 m.
        for (int i = 0; i < 4; i++)
        {
            float lx = (i < 2) ? -0.35f : 0.35f;
            float lz = (i % 2 == 0) ? -0.35f : 0.35f;
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = $"TableLeg_{i}";
            leg.transform.SetParent(table.transform);
            leg.transform.localPosition = new Vector3(lx, -5.4f, lz);
            leg.transform.localScale    = new Vector3(0.08f, 10.75f, 0.08f);
            DestroyImmediate(leg.GetComponent<Collider>());
        }

        // No XR socket and no PartRemoval wiring on the workbench — the table is
        // a passive, fully physical surface. The player can rest the build platform
        // (or anything else) on it freely; gravity and the BoxCollider above do the
        // rest.

        Debug.Log("[PrinterStructureBuilder] Removal station table created (physical, no socket).");
    }

    // =======================================================================
    // IMPORTED MODEL INTEGRATION  (added 2026-04-19)
    //
    // Replaces primitive body geometry with the Blender-exported SLM280 model
    // while keeping all existing interactive props (chamber socket, removal
    // table, quiz tablet) intact.
    //
    // REQUIREMENT:
    //   1. The imported SLM280 FBX must be added as a direct CHILD of this
    //      GameObject (drag it from the Hierarchy onto SLM_280_Printer).
    //   2. The model must contain the expected named parts (SLM_FrontDoor,
    //      SLM_Hinge_0.86, SLM_Hinge_1.34, SLM_BuildPlatform, SLM_Monitor,
    //      Powder_Canister1, a gas cylinder).
    //
    // Inspector: right-click the component header → "Build From Imported Model"
    // =======================================================================

    // -----------------------------------------------------------------------
    // Diagnostic: prints the entire SLM280 hierarchy to the Console so we can
    // see what real names exist in the FBX. Useful when the part-info wiring
    // reports 'not found — skipped' for names we expected to exist.
    // -----------------------------------------------------------------------
    [ContextMenu("Dump Model Hierarchy")]
    public void DumpModelHierarchy()
    {
        Transform model = transform.Find("SLM280");
        if (model == null)
        {
            Debug.LogError("[Dump] 'SLM280' not found as a child of this GameObject.");
            return;
        }
        Debug.Log("[Dump] ===== SLM280 hierarchy =====");
        DumpRecursive(model, 0);
        Debug.Log("[Dump] ===== end =====");
    }

    private static void DumpRecursive(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        bool hasMesh  = t.GetComponent<MeshFilter>() != null &&
                        t.GetComponent<MeshFilter>().sharedMesh != null;
        Debug.Log($"[Dump] {indent}{t.name}{(hasMesh ? "  [mesh]" : "")}");
        for (int i = 0; i < t.childCount; i++)
            DumpRecursive(t.GetChild(i), depth + 1);
    }

    [ContextMenu("Build From Imported Model")]
    public void BuildFromImportedModel()
    {
        Transform model = transform.Find("SLM280");
        if (model == null)
        {
            Debug.LogError("[PrinterStructureBuilder] 'SLM280' not found as a direct child. " +
                           "Drag the SLM280 GameObject from the Hierarchy onto this object, then try again.");
            return;
        }

        // 0. Auto-unpack prefab instance. When the FBX is dragged into a scene, Unity
        //    creates a prefab instance whose hierarchy is locked — children cannot be
        //    reparented. Unpacking completely converts it to a normal GameObject tree.
#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabInstance(model.gameObject))
        {
            PrefabUtility.UnpackPrefabInstance(
                model.gameObject,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            Debug.Log("[Integration] Unpacked SLM280 prefab instance — hierarchy is now editable.");
        }
#endif

        // 1. Clear every child EXCEPT the model itself.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != model) DestroyImmediate(child.gameObject);
        }

        // 2. Anchor the model at origin — rotated 180° around Y so the door
        //    (and monitor / canisters / etc.) face the player (+Z).
        //    This also fixes text/canvases appearing mirrored from the back side.
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.Euler(0f, 180f, 0f);
        model.localScale    = Vector3.one;

        // 3. Wire interactive subsystems that depend on model parts.
        IntegrateModelDoor(model);
        IntegrateModelBuildPlatform(model);       // renames SLM_BuildPlatform → Build_Platform, reparents to root
        IntegrateModelPowderCanister(model);      // Canister1 grabbable + Knob/Lid/Label as children
        IntegrateModelGasBottle(model);           // Gas_CylBot2/Cylinder2/CylTop2/ValveCap2 wrapped as grabbable GasBottle
        WireModelPartInfo(model);                 // hover tooltips for overflow canister, cooling unit, hopper, etc.

        // 4. Create a Control_Panel anchor at the monitor, so the existing
        //    BuildCalibrationPanel() (which looks up "Control_Panel") works.
        CreateControlPanelAnchorAtMonitor(model);

        // 5. Gas_Inlet socket anchor — position & scale set to match Stefan's tested values.
        GameObject gasInlet = new GameObject("Gas_Inlet");
        gasInlet.transform.SetParent(transform);
        gasInlet.transform.localPosition = new Vector3(-2.142f, 0f, -0.5f);
        gasInlet.transform.localRotation = Quaternion.identity;
        gasInlet.transform.localScale    = new Vector3(1.5f, 4.5f, 1.5f);

        // 6. Reuse existing (working) builders for things that don't depend on model.
        BuildCalibrationPanel();                  // canvas + LaserCalibration script
        // NOTE: BuildGasBottle() is no longer called — IntegrateModelGasBottle replaces it
        //       using the imported gas cylinder parts. The Gas_Inlet anchor above provides
        //       the socket location; IntegrateModelGasBottle wires the socket component.
        WireGasInletSocket();                     // adds XRSocketInteractor to Gas_Inlet
        BuildBuildPlatform();                     // chamber socket + BuildPlatform_Prop + BuildPlatform.cs
        BuildRemovalStation();                    // workbench + PartRemoval on Build_Platform
        BuildQuizTablet();                        // self-contained kiosk

        // 7. Final pass — add ray-blocking colliders to every visible mesh that
        //    doesn't have one yet (decorative walls, frames, scaffolding...).
        //    Without this, the XR controller ray passes through walls and the
        //    PrinterPartInfo of inner parts triggers, even though the player
        //    physically can't see them. Adding plain (non-interactable) mesh
        //    colliders blocks the ray at the wall surface, so only directly
        //    visible parts can register hover hits.
        AddRayBlockingCollidersToAllMeshes();

        Debug.LogWarning("[PrinterStructureBuilder] NOTE: 'Build From Imported Model' is a one-shot " +
                         "operation. Running it twice will fail because parts have been reparented out " +
                         "of the SLM280 model. To redo it, delete the SLM_280_Printer GameObject and " +
                         "start over with a fresh model.");

        Debug.Log("[PrinterStructureBuilder] Imported model integration complete.");
    }

    // -----------------------------------------------------------------------
    // Recursive name search through the model hierarchy (handles nested groups)
    // -----------------------------------------------------------------------
    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeep(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    // -----------------------------------------------------------------------
    // Reparent every descendant whose name STARTS WITH the given prefix onto
    // a new parent (keeping world position). Used for bulk-catching variants
    // like SLM_WinFrameTop / SLM_WinFrameBot / etc.
    // -----------------------------------------------------------------------
    private void ReparentByPrefix(Transform root, string prefix, Transform newParent)
    {
        // Collect first so we don't mutate the hierarchy we're traversing.
        var matches = new System.Collections.Generic.List<Transform>();
        CollectByPrefix(root, prefix, matches);
        foreach (Transform t in matches)
            t.SetParent(newParent, true);
    }

    private static void CollectByPrefix(Transform t, string prefix,
                                        System.Collections.Generic.List<Transform> list)
    {
        if (t.name.StartsWith(prefix)) list.Add(t);
        for (int i = 0; i < t.childCount; i++)
            CollectByPrefix(t.GetChild(i), prefix, list);
    }

    // -----------------------------------------------------------------------
    // Removes previously-added interaction components so a re-run of
    // BuildFromImportedModel doesn't leave stale interactables / rigidbodies /
    // BoxColliders behind on the model's imported parts.
    // -----------------------------------------------------------------------
    private static void StripInteractionComponents(Transform t)
    {
        if (t == null) return;
        foreach (var c in t.GetComponents<VRDoor>())                 DestroyImmediate(c);
        foreach (var c in t.GetComponents<GasBottle>())              DestroyImmediate(c);
        foreach (var c in t.GetComponents<PowderCartridge>())        DestroyImmediate(c);
        foreach (var c in t.GetComponents<XRGrabInteractable>())     DestroyImmediate(c);
        foreach (var c in t.GetComponents<XRSimpleInteractable>())   DestroyImmediate(c);
        foreach (var c in t.GetComponents<PrinterPartInfo>())        DestroyImmediate(c);
        foreach (var c in t.GetComponents<Rigidbody>())              DestroyImmediate(c);
        // Only remove BoxColliders that we added; MeshColliders from the FBX stay.
        foreach (var c in t.GetComponents<BoxCollider>())            DestroyImmediate(c);
    }

    // -----------------------------------------------------------------------
    // Door integration: wraps SLM_FrontDoor + hinge parts under a pivot empty
    // positioned at the midpoint of SLM_Hinge_0.86 and SLM_Hinge_1.34.
    // -----------------------------------------------------------------------
    private void IntegrateModelDoor(Transform model)
    {
        Transform frontDoor = FindDeep(model, "SLM_FrontDoor");
        Transform handle    = FindDeep(model, "SLM_DoorHandle");
        if (frontDoor == null)
        {
            Debug.LogWarning("[Integration] SLM_FrontDoor not found — door wiring skipped.");
            return;
        }

        Transform hinge1 = FindDeep(model, "SLM_Hinge_0.86");
        Transform hinge2 = FindDeep(model, "SLM_Hinge_1.34");

        Vector3 hingeWorldPos;
        if (hinge1 != null && hinge2 != null)
            hingeWorldPos = (hinge1.position + hinge2.position) * 0.5f;
        else if (hinge1 != null) hingeWorldPos = hinge1.position;
        else if (hinge2 != null) hingeWorldPos = hinge2.position;
        else
        {
            Renderer dr = frontDoor.GetComponent<Renderer>();
            Bounds b    = dr != null ? dr.bounds : new Bounds(frontDoor.position, Vector3.one);
            hingeWorldPos = new Vector3(b.min.x, b.center.y, b.center.z);   // left edge of door
        }

        // Pivot empty — child of SLM_280_Printer (NOT child of model, so it survives model swaps)
        GameObject hingeGO = new GameObject("Front_Door_Hinge");
        hingeGO.transform.SetParent(transform);
        hingeGO.transform.position = hingeWorldPos;
        hingeGO.transform.rotation = Quaternion.identity;
        hingeGO.transform.localScale = Vector3.one;

        // Reparent door panel + handle + glass + hinges + window frames under the pivot.
        // NOTE: SLM_ChFrame* (chamber frames) are NOT door parts — they belong to the
        //       stationary chamber interior and must stay on the model, not the hinge.
        string[] doorPartNames = {
            "SLM_FrontDoor", "SLM_DoorHandle", "SLM_DoorWindowGlass",
            "SLM_Hinge_0.86", "SLM_Hinge_1.34"
        };
        foreach (string pn in doorPartNames)
        {
            Transform t = FindDeep(model, pn);
            if (t != null) t.SetParent(hingeGO.transform, true);   // true = keep world position
        }

        // Also pick up every SLM_WinFrame* variant (window frame pieces vary in naming)
        ReparentByPrefix(model, "SLM_WinFrame", hingeGO.transform);

        // Strip any stale grab/hover components on the door panel from a previous
        // build. Otherwise the old FrontDoor interactable is still clickable and
        // the grab appears to come from the left edge (the panel) instead of the handle.
        StripInteractionComponents(frontDoor);

        // --- The HANDLE is the interactable, not the door panel ---
        // Why: the handle is on the opposite side of the hinge, so grabbing here
        // gives a natural grip point (like a real door). Grabbing the panel close
        // to the hinge felt weird because small hand moves caused big rotations.
        if (handle == null)
        {
            Debug.LogWarning("[Integration] SLM_DoorHandle not found — falling back to SLM_FrontDoor as grab target. " +
                             "Grab will come from the door panel (left edge) rather than the handle. " +
                             "Check the FBX hierarchy for a child named 'SLM_DoorHandle'.");
        }
        Transform grabTarget = (handle != null) ? handle : frontDoor;

        // Strip any stale components on the grab target too (covers re-runs where
        // previous components on the handle would stack up or duplicate).
        StripInteractionComponents(grabTarget);

        if (grabTarget.GetComponent<Collider>() == null)
        {
            MeshFilter mf = grabTarget.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = grabTarget.gameObject.AddComponent<MeshCollider>();
                mc.convex = true;   // required because we attach a Rigidbody below
            }
            else
            {
                grabTarget.gameObject.AddComponent<BoxCollider>();
            }
        }

        Rigidbody rb = grabTarget.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        grabTarget.gameObject.AddComponent<XRSimpleInteractable>();

        PrinterPartInfo info = grabTarget.gameObject.AddComponent<PrinterPartInfo>();
        info.partName        = "Front Door Handle";
        info.partDescription = "Grab the handle and pull to open, push to close. " +
                               "The safety interlock prevents the laser from firing while the door is open.";

        VRDoor vrDoor         = grabTarget.gameObject.AddComponent<VRDoor>();
        vrDoor.hingeTransform = hingeGO.transform;
        vrDoor.closedAngle    =   0f;
        vrDoor.openAngle      =  90f;   // opens OUTWARD (toward player) after model 180° rotation

        Debug.Log($"[Integration] Door wired — grab target is '{grabTarget.name}' (handle on the opposite side from hinges).");
    }

    // -----------------------------------------------------------------------
    // Build platform (UNIFIED): the imported SLM_BuildPlatform is renamed to
    // Build_Platform, reparented to the printer root, and wired as a single
    // grabbable + simulation + removal object. The player picks it up from
    // the storage location, carries it to the chamber, snaps it in, the
    // simulation drives it downward during print, and PartRemoval re-enables
    // grab on completion so the player can lift it back out.
    //
    // No separate "prop" cube exists anymore.
    // -----------------------------------------------------------------------
    private void IntegrateModelBuildPlatform(Transform model)
    {
        Transform platform = FindDeep(model, "SLM_BuildPlatform");
        if (platform == null)
        {
            Debug.LogWarning("[Integration] SLM_BuildPlatform not found — print simulation will be unanchored.");
            return;
        }

        platform.name = "Build_Platform";
        platform.SetParent(transform, true);   // flat child of SLM_280_Printer

        // Storage location: sitting to the right of the printer at hip height,
        // easy to reach without ducking into the chamber.
        platform.localPosition = new Vector3(1.0f, 0.7f, 0.6f);
        platform.localRotation = Quaternion.identity;

        // Collider — needed for XR grab & socket trigger to register hits.
        if (platform.GetComponent<Collider>() == null)
            platform.gameObject.AddComponent<BoxCollider>();

        // Rigidbody — required by XRGrabInteractable. Non-kinematic + gravity so the
        // platform rests naturally when released, and the chamber socket can take
        // ownership of it on snap (socket needs non-kinematic RB at release time).
        Rigidbody rb = platform.GetComponent<Rigidbody>();
        if (rb == null) rb = platform.gameObject.AddComponent<Rigidbody>();
        rb.mass        = 4f;
        rb.isKinematic = false;
        rb.useGravity  = true;

        // XRGrabInteractable — player grabs with right-hand controller and carries in.
        // interactionLayers:
        //   bit 0 = controller can grab
        //   bit 3 = chamber socket accepts this platform
        //   bit 4 = removal-station socket accepts it post-print
        var grab = platform.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = platform.gameObject.AddComponent<XRGrabInteractable>();
        grab.movementType      = XRBaseInteractable.MovementType.Instantaneous;
        grab.interactionLayers = (1 << 0) | (1 << 3) | (1 << 4);
        grab.enabled           = true;

        // Attach the state machine. chamberSocket is wired later in BuildBuildPlatform.
        if (platform.GetComponent<BuildPlatform>() == null)
            platform.gameObject.AddComponent<BuildPlatform>();

        Debug.Log("[Integration] SLM_BuildPlatform → Build_Platform unified (Storage / Snapped / Printing / Completed).");
    }

    // -----------------------------------------------------------------------
    // Powder: make Canister1 the grabbable cartridge, leave Canister2 as
    // static overflow visual. Create a socket anchor above the hopper.
    // -----------------------------------------------------------------------
    private void IntegrateModelPowderCanister(Transform model)
    {
        Transform canister1 = FindDeep(model, "Powder_Canister1");
        if (canister1 == null)
        {
            Debug.LogWarning("[Integration] Powder_Canister1 not found — falling back to primitive cartridge.");
            BuildPowderCartridge();
            return;
        }

        // --- Socket anchor at the top of the hopper (or reasonable fallback) ---
        Vector3 socketPos = transform.position + new Vector3(0f, 2.2f, 0f);   // fallback above printer
        Transform hopperRef = FindDeep(model, "Hopper_Cone")
                           ?? FindDeep(model, "Hopper_Top")
                           ?? FindDeep(model, "PowderHopper");
        if (hopperRef != null)
        {
            Renderer r = hopperRef.GetComponent<Renderer>();
            socketPos = r != null
                ? new Vector3(r.bounds.center.x, r.bounds.max.y + 0.15f, r.bounds.center.z)
                : hopperRef.position + Vector3.up * 0.15f;
        }

        GameObject socketAnchor = new GameObject("PowderSocket_Anchor");
        socketAnchor.transform.SetParent(transform);
        socketAnchor.transform.position = socketPos;

        XRSocketInteractor socket = socketAnchor.AddComponent<XRSocketInteractor>();
        socket.interactionLayers = 1 << 2;   // bit 2 = powder cartridge only

        SphereCollider trigger = socketAnchor.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius    = 0.25f;

        SocketVisualHint.CreateOnSocket(
            socket,
            PrimitiveType.Cube,
            new Vector3(0.18f, 0.22f, 0.18f),
            new Color(0.8f, 0.7f, 0.2f, 0.3f),
            new Color(1.0f, 0.9f, 0.3f, 0.75f)
        );

        // --- Promote Canister1 to be grabbable ---
        canister1.SetParent(transform, true);
        canister1.name = "PowderCartridge";

        // Reparent detail pieces (knob, lid, label) under the canister so they
        // move together when the player grabs and carries it.
        string[] canisterDetails = {
            "Powder_CanisterKnob1",
            "Powder_CanisterLid1",
            "Powder_Label1"
        };
        foreach (string n in canisterDetails)
        {
            Transform t = FindDeep(model, n);
            if (t != null) t.SetParent(canister1, true);   // keep world position
        }

        if (canister1.GetComponent<Collider>() == null)
            canister1.gameObject.AddComponent<BoxCollider>();

        Rigidbody crb   = canister1.gameObject.AddComponent<Rigidbody>();
        crb.mass        = 3f;
        crb.isKinematic = false;
        crb.useGravity  = true;

        var grab = canister1.gameObject.AddComponent<XRGrabInteractable>();
        grab.movementType      = XRBaseInteractable.MovementType.Instantaneous;
        grab.interactionLayers = (1 << 0) | (1 << 2);   // controller + powder socket

        PowderCartridge pc = canister1.gameObject.AddComponent<PowderCartridge>();
        pc.powderSocket = socket;

        Debug.Log("[Integration] Powder_Canister1 wired as grabbable PowderCartridge. " +
                  "Powder_Canister2 kept as static overflow container.");
    }

    // -----------------------------------------------------------------------
    // Gas bottle: take the imported cylinder parts (Gas_CylBot2, Gas_Cylinder2,
    // Gas_CylTop2, Gas_ValveCap2), parent them under a single 'GasBottle' wrapper,
    // and make that wrapper grabbable + wire GasBottle.cs.
    // -----------------------------------------------------------------------
    private void IntegrateModelGasBottle(Transform model)
    {
        string[] partNames = {
            "Gas_CylBot2", "Gas_Cylinder2", "Gas_CylTop2",
            "Gas_ValveCap2", "Gas_Valve2"
        };

        // Collect found parts and compute combined world bounds for pivot
        System.Collections.Generic.List<Transform> found =
            new System.Collections.Generic.List<Transform>();
        Bounds? combined = null;

        foreach (string n in partNames)
        {
            Transform t = FindDeep(model, n);
            if (t == null) continue;
            found.Add(t);

            Renderer r = t.GetComponent<Renderer>();
            if (r != null)
            {
                if (combined == null) combined = r.bounds;
                else { Bounds b = combined.Value; b.Encapsulate(r.bounds); combined = b; }
            }
        }

        if (found.Count == 0)
        {
            Debug.LogWarning("[Integration] No gas cylinder parts found (Gas_CylBot2/Cylinder2/CylTop2/ValveCap2) " +
                             "— falling back to primitive gas bottle.");
            BuildGasBottle();
            return;
        }

        // Pivot at bottom-center of combined bounds (so the bottle stands upright)
        Vector3 pivotPos;
        if (combined != null)
        {
            Bounds b = combined.Value;
            pivotPos = new Vector3(b.center.x, b.min.y, b.center.z);
        }
        else
        {
            pivotPos = found[0].position;
        }

        // --- Create GasBottle wrapper ---
        GameObject bottle = new GameObject("GasBottle");
        bottle.transform.SetParent(transform);
        bottle.transform.position   = pivotPos;
        bottle.transform.rotation   = Quaternion.identity;
        bottle.transform.localScale = Vector3.one;

        // Reparent all found parts (keep world position)
        foreach (Transform t in found)
            t.SetParent(bottle.transform, true);

        // --- Collider sized to bottle bounds (approximate in local space) ---
        BoxCollider col = bottle.AddComponent<BoxCollider>();
        if (combined != null)
        {
            Bounds b = combined.Value;
            col.center = bottle.transform.InverseTransformPoint(b.center);
            col.size   = b.size;
        }
        else
        {
            col.size   = new Vector3(0.25f, 0.9f, 0.25f);
            col.center = new Vector3(0f, 0.45f, 0f);
        }

        // --- Rigidbody + XRGrabInteractable ---
        Rigidbody rb = bottle.AddComponent<Rigidbody>();
        rb.mass        = 2f;
        rb.isKinematic = false;
        rb.useGravity  = true;

        var grab = bottle.AddComponent<XRGrabInteractable>();
        grab.movementType      = XRBaseInteractable.MovementType.Instantaneous;
        grab.interactionLayers = (1 << 0) | (1 << 1);   // controller + gas socket

        // --- Wire GasBottle.cs (socket ref filled in after WireGasInletSocket runs) ---
        GasBottle gb = bottle.AddComponent<GasBottle>();
        // gasSocket assigned later in WireGasInletSocket() via find on Gas_Inlet

        Debug.Log($"[Integration] Imported gas cylinder wired as grabbable GasBottle " +
                  $"({found.Count}/{partNames.Length} parts found).");
    }

    // -----------------------------------------------------------------------
    // Adds XRSocketInteractor + trigger + visual hint to the Gas_Inlet anchor,
    // and wires every existing GasBottle in the hierarchy to use this socket.
    // Called after IntegrateModelGasBottle creates the bottle and Gas_Inlet exists.
    // -----------------------------------------------------------------------
    private void WireGasInletSocket()
    {
        Transform inletTransform = transform.Find("Gas_Inlet");
        if (inletTransform == null)
        {
            Debug.LogWarning("[PrinterStructureBuilder] Gas_Inlet anchor missing — gas socket not wired.");
            return;
        }

        XRSocketInteractor socket = inletTransform.gameObject.AddComponent<XRSocketInteractor>();
        socket.interactionLayers = 1 << 1;   // bit 1 = gas bottle only

        SphereCollider trigger = inletTransform.gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius    = 0.25f;

        SocketVisualHint.CreateOnSocket(
            socket,
            PrimitiveType.Cylinder,
            new Vector3(0.12f, 0.35f, 0.12f),
            new Color(0.4f, 0.8f, 0.4f, 0.3f),
            new Color(0.3f, 1.0f, 0.5f, 0.75f)
        );

        // Find the GasBottle wrapper and connect its socket reference
        Transform bottle = transform.Find("GasBottle");
        if (bottle != null)
        {
            GasBottle gb = bottle.GetComponent<GasBottle>();
            if (gb != null) gb.gasSocket = socket;
        }

        Debug.Log("[PrinterStructureBuilder] Gas_Inlet socket wired to GasBottle.");
    }

    // -----------------------------------------------------------------------
    // Part-info hover wiring: attaches XRSimpleInteractable + PrinterPartInfo to
    // key model parts so players get name + description labels on the controller
    // when their ray hovers over each part. Educational backbone of the VR app.
    // -----------------------------------------------------------------------
    private void WireModelPartInfo(Transform model)
    {
        // Only educationally essential parts get hover labels. Cosmetic housing, frames,
        // bolts, pipes, trolley wheels, shafts, logos and warning stickers are skipped
        // per student-tutorial focus directive.

        // --- Control Monitor (HMI) ---
        AddHoverInfo(model, "SLM_MonitorScreen", "Control Monitor",
            "Human-machine interface — load print jobs, monitor layer progress, and " +
            "run the daily laser-calibration routine from this screen.");

        AddHoverInfo(model, "SLM_Monitor", "Control Monitor",
            "Human-machine interface — load print jobs, monitor layer progress, and " +
            "run the daily laser-calibration routine from this screen.");

        // --- Emergency Stops ---
        AddHoverInfo(model, "SLM_EStopRed", "Emergency Stop",
            "Press to immediately cut power to the laser, scanner and gas flow. " +
            "Use ONLY for real emergencies — each E-Stop requires a controlled " +
            "restart procedure before a new build can begin.");

        AddHoverInfo(model, "SLM_EStopYellow", "Safety Reset",
            "Safety-circuit reset. Pressed after an E-Stop has been cleared and " +
            "the operator has confirmed the machine is safe to re-energise.");

        AddHoverInfo(model, "Cooling_EStop", "Chiller Emergency Stop",
            "Cuts power to the chiller independently of the main machine E-Stop. " +
            "Used when servicing the coolant loop.");

        // --- Signal Tower (status indicator) ---
        AddHoverInfo(model, "Signal_Red", "Status Tower — Alarm",
            "Red light: machine is in a fault state and requires operator intervention. " +
            "Check the monitor for the error code before attempting a reset.");

        AddHoverInfo(model, "Signal_Yellow", "Status Tower — Warning",
            "Yellow light: attention required — typically low gas pressure, low " +
            "powder level, or an upcoming end-of-build condition.");

        AddHoverInfo(model, "Signal_Green", "Status Tower — Running",
            "Green light: machine is actively building or ready to start a job. " +
            "All safety interlocks are satisfied.");

        // --- Viewing Window (laser-safe glass) ---
        AddHoverInfo(model, "SLM_DoorWindowGlass", "Viewing Window",
            "Laser-safe viewport (typically OD7+ for 1070 nm fibre lasers) that lets " +
            "the operator observe each melt layer without risk of eye damage from " +
            "scattered infrared laser radiation.");

        // --- Powder Overflow Container ---
        AddHoverInfo(model, "Powder_Canister2", "Powder Overflow Container",
            "Collects unmelted powder that escapes during recoating plus spent powder " +
            "removed after the build. Contents must be sieved (typically through a 50–75 µm " +
            "mesh) to remove melt spatter before being returned to the fresh-powder supply.");

        // --- Recoater arm (core SLM process element) ---
        AddHoverInfo(model, "Chamber_RecoaterBar", "Recoater Arm",
            "Sweeps across the build platform between exposures, spreading a fresh " +
            "powder layer (30–60 µm thick) from the hopper. Even layer thickness is " +
            "critical — inconsistent coating causes porosity and failed builds.");

        AddHoverInfo(model, "Chamber_RecoaterBlade", "Recoater Blade",
            "Consumable rubber or hard-metal blade on the underside of the recoater " +
            "arm. Inspect for nicks before every build — a damaged blade leaves " +
            "streaks in the powder bed and corrupts layer geometry.");

        // --- Laser optics (scanner / f-theta lens assembly) ---
        AddHoverInfo(model, "Chamber_OpticLens_-0.11", "Laser Scanner Optic",
            "F-theta lens under the galvanometer scanner. Focuses the fibre-laser " +
            "beam onto the powder bed. Keep the protective window clean — spatter " +
            "contamination reduces beam power and causes weld defects.");

        AddHoverInfo(model, "Chamber_OpticLens_0.11", "Laser Scanner Optic",
            "F-theta lens under the galvanometer scanner. Focuses the fibre-laser " +
            "beam onto the powder bed. Keep the protective window clean — spatter " +
            "contamination reduces beam power and causes weld defects.");

        // --- Second gas bottle (scenery, but educational) ---
        AddHoverInfo(model, "Gas_Cylinder1", "Inert Gas Supply (Backup)",
            "Second argon / nitrogen bottle held in reserve. Swapped in when the " +
            "primary bottle is depleted so a build is never interrupted mid-job.");

        AddHoverInfo(model, "Gas_Valve1", "Gas Bottle Valve",
            "Main shutoff on the inert-gas cylinder. Only opened AFTER the regulator " +
            "is fitted and the machine's gas inlet is connected.");

        // --- Keyword fallback: only high-value equipment groups, no pipes/trolleys/wheels ---
        WireByKeyword(model);

        Debug.Log("[PrinterStructureBuilder] Part-info hovers wired on essential tutorial parts.");
    }

    // -----------------------------------------------------------------------
    // Keyword-based fallback: sweep every mesh part in the model and label any
    // that matches a keyword (case-insensitive substring). Parts already labelled
    // by the explicit AddHoverInfo calls above are left alone.
    //
    // This catches variations where the exact name differs from what we expect
    // (e.g. 'SLM280_CoolingUnit' might actually be 'SLM_CoolingBody' in the FBX).
    // -----------------------------------------------------------------------
    private void WireByKeyword(Transform model)
    {
        // Only high-value tutorial equipment gets keyword-matched labels.
        // Deliberately EXCLUDED: Pipe/Hose/Tube, Trolley/Cart/Wheel, cosmetic housing,
        // frames, bolts, logos, PPE icons, warning stickers, shafts, keyboard.
        var rules = new (string[] keys, string name, string desc)[]
        {
            (new[] { "Cooling_" }, "Cooling Unit",
             "Closed-loop water chiller that cools the fiber laser resonator and the " +
             "galvanometer scanner mirrors. Must be running before the laser can fire — " +
             "unchilled optics can suffer thermal damage within seconds."),

            (new[] { "Hopper_" }, "Powder Hopper",
             "Dispenses fresh metal powder to the recoater arm one layer at a time. " +
             "Top-loaded from the powder canister during the preparation step."),

            (new[] { "Scaffold_" }, "Maintenance Scaffold",
             "Working platform for safely accessing the upper hopper ports, gas lines, " +
             "and filter housings at the top of the machine."),

            (new[] { "Chamber_RecArm", "Chamber_Recoater" }, "Recoater Arm",
             "Sweeps across the build platform between exposures, spreading a fresh " +
             "powder layer (30–60 µm thick) from the hopper. Even layer thickness is " +
             "critical — inconsistent coating causes porosity and failed builds."),

            (new[] { "Chamber_Optic" }, "Laser Scanner Optic",
             "F-theta lens and protective window under the galvanometer scanner. " +
             "Focuses the fibre-laser beam onto the powder bed. Spatter contamination " +
             "on the protective glass reduces beam power and causes weld defects."),

            (new[] { "Chamber_Light" }, "Chamber Light",
             "Sealed LED fixture that illuminates the build volume for operator " +
             "inspection through the viewing window. Runs off the same interlock " +
             "circuit as the chamber door."),

            (new[] { "Chamber_Floor" }, "Build Area",
             "The base of the build chamber. The build platform descends through this " +
             "floor one layer thickness at a time as the part is grown upward."),
        };

        // Names to skip — these are either interactive (separate scripts),
        // purely cosmetic, or already explicitly labelled in WireModelPartInfo.
        var skip = new System.Collections.Generic.HashSet<string>
        {
            // Interactive parts with their own scripts
            "SLM_FrontDoor", "SLM_DoorHandle", "SLM_DoorWindowGlass",
            "PowderCartridge", "GasBottle", "Build_Platform",
            "Gas_CylBot2", "Gas_Cylinder2", "Gas_CylTop2", "Gas_ValveCap2", "Gas_Valve2",
            "Powder_Canister1", "Powder_CanisterKnob1", "Powder_CanisterLid1", "Powder_Label1"
        };

        // Keywords that identify a part as PURELY COSMETIC — never label these.
        string[] cosmeticMarkers = new[]
        {
            "Frame", "Body", "Worktop", "Header", "Recess", "LogoPlate",
            "LowerHandle", "PPEIcon", "WarnLabel", "Wall", "Ceiling",
            "Pipe", "Hose", "Tube", "Trolley", "Cart", "Wheel",
            "Shaft", "RackBar", "Shelf", "Bolt", "Hinge", "WinFrame",
            "Pole", "KeyboardTray", "Keyboard"
        };

        Transform[] all = model.GetComponentsInChildren<Transform>();
        int added = 0;
        foreach (Transform t in all)
        {
            if (skip.Contains(t.name)) continue;
            if (t.GetComponent<Renderer>() == null) continue;         // only mesh parts
            if (t.GetComponent<PrinterPartInfo>() != null) continue;  // already labelled above

            // Skip purely cosmetic parts
            bool isCosmetic = false;
            foreach (string cm in cosmeticMarkers)
            {
                if (t.name.IndexOf(cm, System.StringComparison.OrdinalIgnoreCase) >= 0)
                { isCosmetic = true; break; }
            }
            if (isCosmetic) continue;

            foreach (var rule in rules)
            {
                bool match = false;
                foreach (string kw in rule.keys)
                {
                    if (t.name.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    { match = true; break; }
                }
                if (!match) continue;

                WireHoverOnSingleMesh(t, rule.name, rule.desc);
                added++;
                break;   // first matching rule wins
            }
        }
        Debug.Log($"[WireByKeyword] Keyword-based labelling added hover info to {added} additional essential mesh part(s).");
    }

    // -----------------------------------------------------------------------
    // Helper for WireByKeyword: wire hover on a SINGLE mesh GameObject (no subtree walk)
    // -----------------------------------------------------------------------
    private void WireHoverOnSingleMesh(Transform t, string displayName, string description)
    {
        GameObject go = t.gameObject;

        if (go.GetComponent<Collider>() == null)
        {
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = go.AddComponent<MeshCollider>();
                mc.convex = false;
            }
            else
            {
                go.AddComponent<BoxCollider>();
            }
        }

        if (go.GetComponent<XRSimpleInteractable>() == null)
            go.AddComponent<XRSimpleInteractable>();

        PrinterPartInfo info = go.GetComponent<PrinterPartInfo>();
        if (info == null) info = go.AddComponent<PrinterPartInfo>();
        info.partName          = displayName;
        info.partDescription   = description;
        info.highlightMaterial = highlightMaterial;
    }

    // -----------------------------------------------------------------------
    // Helper: finds a named part and attaches XRSimpleInteractable + PrinterPartInfo
    // to every mesh piece inside its subtree (including the part itself if it has
    // a renderer). Each mesh gets its own MeshCollider + interactable so the XR
    // ray reliably registers a hover hit — a single trigger-collider on the
    // parent empty did NOT work because XR rays hit the visible child meshes first.
    // -----------------------------------------------------------------------
    private void AddHoverInfo(Transform model, string partName, string displayName, string description)
    {
        Transform t = FindDeep(model, partName);
        if (t == null)
        {
            Debug.Log($"[WirePartInfo] '{partName}' not found — skipped.");
            return;
        }

        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[WirePartInfo] '{partName}' has no renderers in its subtree — cannot hover.");
            return;
        }

        int wired = 0;
        foreach (Renderer r in renderers)
        {
            GameObject go = r.gameObject;

            // Ensure a collider on the visible mesh
            if (go.GetComponent<Collider>() == null)
            {
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = go.AddComponent<MeshCollider>();
                    mc.convex = false;   // no Rigidbody here, so non-convex is fine
                }
                else
                {
                    // No mesh filter but has a renderer (rare) — fit a BoxCollider to its bounds
                    BoxCollider bc = go.AddComponent<BoxCollider>();
                    Bounds b = r.bounds;
                    bc.center = go.transform.InverseTransformPoint(b.center);
                    Vector3 ls = go.transform.lossyScale;
                    bc.size = new Vector3(
                        b.size.x / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
                        b.size.y / Mathf.Max(0.0001f, Mathf.Abs(ls.y)),
                        b.size.z / Mathf.Max(0.0001f, Mathf.Abs(ls.z))
                    );
                }
            }

            if (go.GetComponent<XRSimpleInteractable>() == null)
                go.AddComponent<XRSimpleInteractable>();

            // Overwrite (later/more-specific calls win over broader earlier ones).
            PrinterPartInfo info = go.GetComponent<PrinterPartInfo>();
            if (info == null) info = go.AddComponent<PrinterPartInfo>();
            info.partName          = displayName;
            info.partDescription   = description;
            info.highlightMaterial = highlightMaterial;

            wired++;
        }

        Debug.Log($"[WirePartInfo] '{partName}' → \"{displayName}\" wired on {wired} mesh piece(s).");
    }

    // -----------------------------------------------------------------------
    // Creates a 'Control_Panel' empty child aligned with the imported monitor,
    // so the existing BuildCalibrationPanel() (which looks up "Control_Panel")
    // can anchor the laser calibration canvas in the right place.
    // -----------------------------------------------------------------------
    private void CreateControlPanelAnchorAtMonitor(Transform model)
    {
        Transform monitor = FindDeep(model, "SLM_MonitorScreen")
                         ?? FindDeep(model, "SLM_Monitor");

        GameObject anchor = new GameObject("Control_Panel");
        anchor.transform.SetParent(transform);

        if (monitor != null)
        {
            Renderer mr = monitor.GetComponent<Renderer>();
            anchor.transform.position = mr != null ? mr.bounds.center : monitor.position;
            anchor.transform.rotation = monitor.rotation;
        }
        else
        {
            // Fallback — near where the primitive Control_Panel used to live
            anchor.transform.localPosition = new Vector3(0.72f, 1.7f, 0.53f);
            Debug.LogWarning("[Integration] SLM_Monitor not found — using fallback position for Control_Panel anchor.");
        }
    }

    // -----------------------------------------------------------------------
    // Helper method — creates one printer part with all required components
    // -----------------------------------------------------------------------
    private void CreatePart(
        string objectName,
        PrimitiveType shape,
        Vector3 localPosition,
        Vector3 localScale,
        string partName,
        string partDescription,
        Material mat = null)
    {
        // Create the primitive
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = objectName;
        part.transform.SetParent(transform);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = Quaternion.identity;

        // Apply material if provided
        if (mat != null)
        {
            Renderer r = part.GetComponent<Renderer>();
            if (r != null) r.material = mat;
        }

        // Add XR Simple Interactable so the VR ray can hover over it
        XRSimpleInteractable interactable = part.AddComponent<XRSimpleInteractable>();

        // Add PrinterPartInfo with the label data
        PrinterPartInfo info = part.AddComponent<PrinterPartInfo>();
        info.partName = partName;
        info.partDescription = partDescription;
        info.highlightMaterial = highlightMaterial;

        // Labels are now handled globally by ControllerLabelDisplay — no per-part canvas needed
    }
}
