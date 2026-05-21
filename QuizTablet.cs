using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Quiz logic for the kiosk tablet next to the SLM printer.
/// All UI references are assigned by PrinterStructureBuilder.BuildQuizTablet()
/// at edit-time (before Play), exactly like LaserCalibration gets its references.
/// Start() wires the onClick listeners — buttons already exist in the hierarchy.
/// </summary>
public class QuizTablet : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // UI references — assigned by PrinterStructureBuilder in edit mode
    // -----------------------------------------------------------------------
    [Header("Assigned by PrinterStructureBuilder — do not edit manually")]
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI feedbackText;

    public Button[]           answerBtns   = new Button[4];
    public TextMeshProUGUI[]  answerLabels = new TextMeshProUGUI[4];
    public Image[]            answerImages = new Image[4];

    public GameObject startBtnGO;
    public GameObject nextBtnGO;
    public GameObject retryBtnGO;

    // -----------------------------------------------------------------------
    // Questions
    // -----------------------------------------------------------------------
    private class QuizQ
    {
        public string   question;
        public string[] options;
        public int      correct;
        public string   explanation;
    }

    // Quiz bank — refocused on essential SETUP and SAFETY topics. Material-of-a-part
    // and pure-trivia questions (laser wattage, build volume, layer thickness) were
    // dropped because they don't help a student learn how to operate the machine
    // safely. Every question below ties to either a step of the operating workflow
    // or a real failure / safety hazard the operator must understand.
    private readonly List<QuizQ> questions = new List<QuizQ>
    {
        // ---- Q1 — build-platform preparation (setup) ----
        // Replaces the old "What does SLM stand for?" question because the
        // acronym was never expanded anywhere a student would see it in-game.
        // This question is grounded in the Platform contextual error note
        // ("DIRTY SURFACE — residue from a previous build reduces adhesion of
        // the first layer") and connects directly to Q10 (recoater crash from
        // a warped part) — together they teach the chain:
        //   dirty platform → poor first-layer adhesion → warping → recoater crash.
        new QuizQ {
            question    = "Why must the build platform be cleaned of residue between jobs?",
            options     = new[] {
                "To match the colour of the new metal powder",
                "Residue reduces first-layer adhesion, letting the part warp upward and risk a recoater crash",
                "To improve laser focus on the platform surface",
                "So the platform can rotate freely during printing"
            },
            correct     = 1,
            explanation = "The first layer must bond solidly to a clean steel platform. Old residue weakens that bond — the part lifts, curls upward, and can collide with the recoater blade on the next sweep."
        },

        // ---- Q2 — why we need inert gas (safety reasoning) ----
        new QuizQ {
            question    = "Why is the build chamber filled with Argon or Nitrogen before printing?",
            options     = new[] {
                "To cool down the laser optics",
                "To reduce oxygen so the molten powder cannot oxidise or ignite",
                "To increase the laser absorption rate of the metal",
                "To pre-heat the metal powder before scanning"
            },
            correct     = 1,
            explanation = "Inert gas displaces oxygen so the melt pool stays clean. Reactive metals like Titanium can ignite if O2 levels are too high."
        },

        // ---- Q3 — O2 threshold before starting (safety) ----
        new QuizQ {
            question    = "What maximum oxygen level must the chamber reach before a print can start?",
            options     = new[] { "Below 5%", "Below 1%", "Below 0.1%", "Exactly 0%" },
            correct     = 2,
            explanation = "O2 must drop below 0.1% before the laser is allowed to fire. Above that, parts oxidise and reactive powders become a fire hazard."
        },

        // ---- Q4 — pre-print interlock (setup) ----
        new QuizQ {
            question    = "Which step MUST be completed before you press Start Print?",
            options     = new[] {
                "Remove the powder cartridge for inspection",
                "Manually rotate the build platform 90°",
                "Seal the chamber door so the interlock activates",
                "Run a test exposure with the door open"
            },
            correct     = 2,
            explanation = "The door interlock is the primary safety system. The laser cannot fire and the inert atmosphere cannot be maintained until the chamber is fully sealed."
        },

        // ---- Q5 — the recoater's role (setup) ----
        new QuizQ {
            question    = "What is the role of the recoater?",
            options     = new[] {
                "Focuses and aims the laser beam",
                "Spreads a thin, even layer of fresh powder over the build area between exposures",
                "Removes the finished part from the chamber",
                "Monitors the oxygen level inside the chamber"
            },
            correct     = 1,
            explanation = "Between every laser exposure the recoater sweeps a 20–80 µm layer of fresh powder across the build area. Uneven coverage causes porosity and failed parts."
        },

        // ---- Q6 — platform descent (setup / process understanding) ----
        new QuizQ {
            question    = "After each layer has been fused, what happens to the build platform?",
            options     = new[] {
                "It stays in position",
                "It rises by one layer thickness",
                "It rotates 90°",
                "It descends by one layer thickness"
            },
            correct     = 3,
            explanation = "The platform drops one layer height so the recoater has room to spread the next powder layer on top."
        },

        // ---- Q7 — powder cross-contamination (setup / quality) ----
        new QuizQ {
            question    = "Why must you NEVER mix different metal powders in the same cartridge?",
            options     = new[] {
                "It makes the recoater move too slowly",
                "Even small cross-contamination changes the alloy and causes part failure or unsafe melt behaviour",
                "It increases the time needed to vacuum the chamber",
                "It changes the colour of the laser beam"
            },
            correct     = 1,
            explanation = "Even ~1% of the wrong alloy alters mechanical properties and can produce dangerous melt-pool reactions. Different powders must be processed on separate, fully purged machines."
        },

        // ---- Q8 — opening door during print (safety) ----
        new QuizQ {
            question    = "What is the biggest safety risk of opening the chamber door during a print?",
            options     = new[] {
                "The recoater suddenly stops moving",
                "The build platform jumps upward",
                "Incoming oxygen contaminates the layer and can ignite reactive powders such as Titanium",
                "The laser switches to a longer wavelength"
            },
            correct     = 2,
            explanation = "A sudden O2 influx contaminates the current layer; with reactive powders it can also cause a flash fire. The interlock must never be bypassed."
        },

        // ---- Q9 — Class 4 laser safety ----
        new QuizQ {
            question    = "The SLM 280 2.0 uses a 700 W Class 4 fibre laser. What must the operator NEVER do?",
            options     = new[] {
                "Replace the powder cartridge",
                "Operate the machine with the door open or the interlock bypassed",
                "Inspect the recoater blade between builds",
                "Calibrate the laser through the HMI"
            },
            correct     = 1,
            explanation = "Class 4 laser radiation can cause permanent eye damage even from reflections. The sealed chamber is what makes the system safe to be next to — never operate with it open."
        },

        // ---- Q10 — recoater crash (setup / failure mode) ----
        new QuizQ {
            question    = "What is the most common cause of a recoater crash during a build?",
            options     = new[] {
                "The chamber door was left slightly open",
                "A part has warped upward and now protrudes above the powder layer surface",
                "The build platform was not preheated",
                "The laser power was set too low"
            },
            correct     = 1,
            explanation = "Thermal stress curls poorly-supported parts off the platform. The recoater blade then collides with them — the most common SLM failure mode. Good support strategy prevents most cases."
        },
    };

    // -----------------------------------------------------------------------
    // Runtime state
    // -----------------------------------------------------------------------
    private int  currentQ = 0;
    private int  score    = 0;
    private bool answered = false;

    // -----------------------------------------------------------------------
    // Unity lifecycle — wire onClick listeners, then show idle screen.
    // Buttons already exist in hierarchy (built in edit mode by PrinterStructureBuilder).
    // -----------------------------------------------------------------------
    void Start()
    {
        // Wire answer buttons
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            if (answerBtns[i] != null)
                answerBtns[i].onClick.AddListener(() => OnAnswerClicked(idx));
        }

        // Wire control buttons
        if (startBtnGO != null)
        {
            var b = startBtnGO.GetComponent<Button>();
            if (b != null) b.onClick.AddListener(OnStartClicked);
        }
        if (nextBtnGO != null)
        {
            var b = nextBtnGO.GetComponent<Button>();
            if (b != null) b.onClick.AddListener(OnNextClicked);
        }
        if (retryBtnGO != null)
        {
            var b = retryBtnGO.GetComponent<Button>();
            if (b != null) b.onClick.AddListener(OnRetryClicked);
        }

        ShowIdle();
    }

    // -----------------------------------------------------------------------
    // Idle screen
    // -----------------------------------------------------------------------
    private void ShowIdle()
    {
        if (headerText  != null) headerText.text  = "KNOWLEDGE QUIZ";
        if (bodyText    != null) bodyText.text    =
            "Test your understanding of SLM metal 3D printing.\n\n10 multiple-choice questions.";
        if (feedbackText != null) feedbackText.text = "";

        SetAnswersVisible(false);
        if (nextBtnGO  != null) nextBtnGO.SetActive(false);
        if (retryBtnGO != null) retryBtnGO.SetActive(false);
        if (startBtnGO != null) startBtnGO.SetActive(true);
    }

    // -----------------------------------------------------------------------
    // Show question
    // -----------------------------------------------------------------------
    private void ShowQuestion(int index)
    {
        answered = false;
        QuizQ q  = questions[index];

        if (headerText  != null) headerText.text  = $"Question {index + 1} / {questions.Count}   Score: {score}";
        if (bodyText    != null) bodyText.text    = q.question;
        if (feedbackText != null) feedbackText.text = "";

        string[] letters = { "A", "B", "C", "D" };
        for (int i = 0; i < 4; i++)
        {
            if (answerLabels[i] != null)
                answerLabels[i].text = $"{letters[i]}.  {q.options[i]}";

            if (answerBtns[i] != null)
            {
                answerBtns[i].interactable = true;
                ColorBlock cb = answerBtns[i].colors;
                cb.normalColor      = new Color(0.12f, 0.12f, 0.22f);
                cb.highlightedColor = new Color(0.20f, 0.40f, 0.80f);
                cb.disabledColor    = new Color(0.08f, 0.08f, 0.12f);
                answerBtns[i].colors = cb;
            }
            if (answerImages[i] != null)
                answerImages[i].color = new Color(0.12f, 0.12f, 0.22f);
        }

        if (startBtnGO != null) startBtnGO.SetActive(false);
        if (nextBtnGO  != null) nextBtnGO.SetActive(false);
        if (retryBtnGO != null) retryBtnGO.SetActive(false);
        SetAnswersVisible(true);
    }

    // -----------------------------------------------------------------------
    // Results screen
    // -----------------------------------------------------------------------
    private void ShowResults()
    {
        bool   passed = score >= 7;
        string grade  = score == questions.Count ? "Perfect!"
                      : score >= 9               ? "Excellent"
                      : score >= 7               ? "Passed"
                      :                            "Not yet";

        if (headerText != null)  headerText.text  = "RESULTS";
        if (bodyText   != null)  bodyText.text    =
            $"{grade}\n\nScore: {score} / {questions.Count}\n\n" +
            (passed ? "Solid understanding of SLM operation!" : "Review the printer and try again.");
        if (feedbackText != null) feedbackText.text = "";

        SetAnswersVisible(false);
        if (startBtnGO != null) startBtnGO.SetActive(false);
        if (nextBtnGO  != null) nextBtnGO.SetActive(false);
        if (retryBtnGO != null) retryBtnGO.SetActive(true);
    }

    // -----------------------------------------------------------------------
    // Button callbacks
    // -----------------------------------------------------------------------
    private void OnStartClicked()
    {
        currentQ = 0;
        score    = 0;
        ShowQuestion(0);
    }

    private void OnAnswerClicked(int chosen)
    {
        if (answered) return;
        answered = true;

        QuizQ  q       = questions[currentQ];
        bool   correct = chosen == q.correct;
        if (correct) score++;

        string[] letters = { "A", "B", "C", "D" };

        for (int i = 0; i < 4; i++)
        {
            if (answerBtns[i] != null) answerBtns[i].interactable = false;

            Color col = (i == q.correct)         ? new Color(0.08f, 0.50f, 0.12f)   // green
                      : (i == chosen && !correct) ? new Color(0.50f, 0.07f, 0.07f)  // red
                      :                             new Color(0.08f, 0.08f, 0.12f);  // dim

            if (answerImages[i] != null) answerImages[i].color = col;
            if (answerBtns[i]   != null)
            {
                ColorBlock cb    = answerBtns[i].colors;
                cb.disabledColor = col;
                answerBtns[i].colors = cb;
            }
        }

        if (feedbackText != null)
            feedbackText.text = correct
                ? $"[OK] Correct!  {q.explanation}"
                : $"[X] Wrong. Correct: {letters[q.correct]}.  {q.explanation}";

        bool isLast = currentQ >= questions.Count - 1;
        if (nextBtnGO != null)
        {
            nextBtnGO.SetActive(true);
            var lbl = nextBtnGO.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.text = isLast ? "SEE RESULTS" : "NEXT >>";
        }
    }

    private void OnNextClicked()
    {
        currentQ++;
        if (currentQ >= questions.Count)
            ShowResults();
        else
            ShowQuestion(currentQ);
    }

    private void OnRetryClicked() => ShowIdle();

    // -----------------------------------------------------------------------
    private void SetAnswersVisible(bool visible)
    {
        for (int i = 0; i < 4; i++)
            if (answerBtns[i] != null)
                answerBtns[i].gameObject.SetActive(visible);
        if (feedbackText != null) feedbackText.gameObject.SetActive(visible);
    }
}
