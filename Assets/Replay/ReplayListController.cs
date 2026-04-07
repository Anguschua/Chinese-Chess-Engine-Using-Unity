using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class ReplayListController : MonoBehaviour
{
    [Header("UI")]
    public Button btnBack;

    [System.Serializable]
    public class ReplayButtonSlot
    {
        public Button button;
        public TMP_Text label;
    }

    [Header("10 replay buttons, top = most recent")]
    public ReplayButtonSlot[] replayButtons;

    private void Start()
    {
        RefreshList();
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(BackToMenu);
        }
    }

    public void RefreshList()
    {
        List<ReplayRecord> replays = BattleHistoryStore.GetAll();

        for (int i = 0; i < replayButtons.Length; i++)
        {
            int index = i;

            if (replayButtons[i] == null || replayButtons[i].button == null)
                continue;

            replayButtons[i].button.onClick.RemoveAllListeners();

            if (i < replays.Count && replays[i] != null)
            {
                ReplayRecord replay = replays[i];

                replayButtons[i].button.interactable = true;

                if (replayButtons[i].label != null)
                    replayButtons[i].label.text = replay.displayName;

                replayButtons[i].button.onClick.AddListener(() =>
                {
                    ReplaySelectionStore.SelectedReplay = BattleHistoryStore.GetReplayAt(index);
                    SceneManager.LoadScene(SceneNames.ReplayScene, LoadSceneMode.Single);
                });
            }
            else
            {
                replayButtons[i].button.interactable = false;

                if (replayButtons[i].label != null)
                    replayButtons[i].label.text = "Empty";
            }
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
    }
}