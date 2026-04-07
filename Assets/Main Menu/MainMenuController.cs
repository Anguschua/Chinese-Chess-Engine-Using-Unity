using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button btnPlayAI1;
    public Button btnPlayAI2;
    public Button btnExperiment;
    public Button btnBattleHistory;

    void Start()
    {
        EnsureGameSettings();

        if (btnPlayAI1 != null) btnPlayAI1.onClick.AddListener(() =>
        {
            GameSettings.I.mode = GameMode.PlayAI1;
            SceneManager.LoadScene(SceneNames.PlaySetup);
        });

        if (btnPlayAI2 != null) btnPlayAI2.onClick.AddListener(() =>
        {
            GameSettings.I.mode = GameMode.PlayAI2;
            SceneManager.LoadScene(SceneNames.PlaySetup);
        });

        if (btnExperiment != null) btnExperiment.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(SceneNames.ExperimentSetup);
        });

        if (btnBattleHistory != null) btnBattleHistory.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(SceneNames.ReplayList);
        });
    }

    void EnsureGameSettings()
    {
        if (GameSettings.I == null)
        {
            var go = new GameObject("GameSettings");
            go.AddComponent<GameSettings>();
        }
    }
}