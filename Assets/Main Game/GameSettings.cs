using UnityEngine;

public enum GameMode
{
    None,
    PlayAI1,
    PlayAI2,
    Experiment
}

public class GameSettings : MonoBehaviour
{
    public static GameSettings I;

    [Header("General")]
    public GameMode mode = GameMode.None;

    [Header("Play Settings")]
    public Side playerSide = Side.Red;
    public Side aiSide = Side.Black;

    [Header("Experiment Settings")]
    public int experimentCount = 1;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayerSide(Side side)
    {
        playerSide = side;
        aiSide = (side == Side.Red) ? Side.Black : Side.Red;
    }

    public void SetMode(GameMode newMode)
    {
        mode = newMode;
    }

    public void SetExperimentCount(int count)
    {
        experimentCount = count;
    }

    public int getExperimentCount()
    {
        return experimentCount;
    }
}