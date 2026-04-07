using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ExperimentSetupController : MonoBehaviour
{
    private static ExperimentSetupController instance;

    [Header("UI")]
    public TMP_Text hintText;

    public Button btnOne;
    public Button btnFive;
    public Button btnTen;

    public Button btnStart;
    public Button btnBack;

    [Header("Optional Visuals")]
    public Image oneButtonImage;
    public Image fiveButtonImage;
    public Image tenButtonImage;

    [Header("Selected Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;

    private int selectedExperimentCount = 1;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        EnsurePersistentManagers();
    }

    private void Start()
    {
        if (GameSettings.I != null && GameSettings.I.experimentCount > 0)
            selectedExperimentCount = GameSettings.I.experimentCount;
        else
            selectedExperimentCount = 1;

        if (btnOne != null)
        {
            btnOne.onClick.RemoveAllListeners();
            btnOne.onClick.AddListener(() => SelectExperimentCount(1));
        }

        if (btnFive != null)
        {
            btnFive.onClick.RemoveAllListeners();
            btnFive.onClick.AddListener(() => SelectExperimentCount(5));
        }

        if (btnTen != null)
        {
            btnTen.onClick.RemoveAllListeners();
            btnTen.onClick.AddListener(() => SelectExperimentCount(10));
        }

        if (btnStart != null)
        {
            btnStart.onClick.RemoveAllListeners();
            btnStart.onClick.AddListener(StartExperiment);
        }

        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(Back);
        }

        RefreshSelectionUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void EnsurePersistentManagers()
    {
        if (GameSettings.I == null)
        {
            GameObject go = new GameObject("GameSettings");
            go.AddComponent<GameSettings>();
        }
    }

    private void SelectExperimentCount(int count)
    {
        selectedExperimentCount = count;

        if (GameSettings.I != null)
            GameSettings.I.SetExperimentCount(count);

        Debug.Log($"[ExperimentSetup] Selected count = {selectedExperimentCount}, GameSettings = {(GameSettings.I != null ? GameSettings.I.experimentCount : -1)}");

        RefreshSelectionUI();
    }

    private void RefreshSelectionUI()
    {
        if (hintText != null)
            hintText.text = $"Selected rounds: {selectedExperimentCount}";

        if (oneButtonImage != null)
            oneButtonImage.color = (selectedExperimentCount == 1) ? selectedColor : normalColor;

        if (fiveButtonImage != null)
            fiveButtonImage.color = (selectedExperimentCount == 5) ? selectedColor : normalColor;

        if (tenButtonImage != null)
            tenButtonImage.color = (selectedExperimentCount == 10) ? selectedColor : normalColor;
    }

    private void StartExperiment()
    {
        if (GameSettings.I == null)
        {
            Debug.LogError("[ExperimentSetup] GameSettings is missing.");
            return;
        }

        GameSettings.I.SetMode(GameMode.Experiment);
        GameSettings.I.SetExperimentCount(selectedExperimentCount);

        Debug.Log($"[ExperimentSetup] Final save count = {GameSettings.I.experimentCount}");

        if (hintText != null)
            hintText.text = $"Loading experiment: {selectedExperimentCount} round(s)...";

        SceneManager.LoadScene(SceneNames.Experiment);
    }

    private void Back()
    {
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
}