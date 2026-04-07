using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text titleText;

    [Header("Table Values")]
    public TMP_Text modeValueText;
    public TMP_Text turnValueText;
    public TMP_Text selectedValueText;
    public TMP_Text timeValueText;
    public TMP_Text memoryValueText;

    [Header("Buttons")]
    public Button btnUndo;
    public Button btnRestart;
    public Button btnBack;

    private MainController mainController;

    void Awake()
    {
        mainController = FindObjectOfType<MainController>();

        if (mainController == null)
        {
            Debug.LogError("[GameUIController] MainController not found in scene.");
            return;
        }

        if (btnUndo != null) btnUndo.onClick.AddListener(mainController.UI_Undo);
        if (btnRestart != null) btnRestart.onClick.AddListener(mainController.UI_Restart);
        if (btnBack != null) btnBack.onClick.AddListener(mainController.UI_BackToMenu);
    }

    public void SetTitleValue(string text)
    {
        if (titleText != null) titleText.text = text;
    }

    public void SetModeValue(string text)
    {
        if (modeValueText != null) modeValueText.text = text;
    }

    public void SetTurn(Side side)
    {
        if (turnValueText != null) turnValueText.text = side.ToString();
    }

    public void SetSelectedValue(string text)
    {
        if (selectedValueText != null) selectedValueText.text = text;
    }

    public void SetTimeValue(string text)
    {
        if (timeValueText != null) timeValueText.text = text;
    }

    public void SetMemoryValue(string text)
    {
        if (memoryValueText != null) memoryValueText.text = text;
    }

    public void SetAIMetrics(long ms, long memBytes, int nodes = -1)
    {
        float memMB = memBytes / (1024f * 1024f);
        SetTimeValue(nodes >= 0 ? $"{ms} ms ({nodes} nodes)" : $"{ms} ms");
        SetMemoryValue($"{memMB:F2} MB");
    }

    public void SetButtons(bool canUndo)
    {
        if (btnUndo != null) btnUndo.interactable = canUndo;
    }
}