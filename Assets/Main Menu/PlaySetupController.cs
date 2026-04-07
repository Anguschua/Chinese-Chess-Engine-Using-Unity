using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PlaySetupController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public Button btnPlayAsRed;
    public Button btnPlayAsBlack;
    public Button btnBack;

    void Start()
    {
        EnsureSingletons();

        if (titleText != null)
        {
            if (GameSettings.I.mode == GameMode.PlayAI1)
                titleText.text = "Play vs Minimax AI";
            else if (GameSettings.I.mode == GameMode.PlayAI2)
                titleText.text = "Play vs MCTS AI";
            else
                titleText.text = "Choose Your Side";
        }

        if (btnPlayAsRed != null)
            btnPlayAsRed.onClick.AddListener(PlayAsRed);

        if (btnPlayAsBlack != null)
            btnPlayAsBlack.onClick.AddListener(PlayAsBlack);

        if (btnBack != null)
            btnBack.onClick.AddListener(BackToMainMenu);
    }

    void EnsureSingletons()
    {
        if (GameSettings.I == null)
        {
            var go = new GameObject("GameSettings");
            go.AddComponent<GameSettings>();
        }
    }

    public void PlayAsRed()
    {
        GameSettings.I.SetPlayerSide(Side.Red);
        SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
    }

    public void PlayAsBlack()
    {
        GameSettings.I.SetPlayerSide(Side.Black);
        SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
    }
}