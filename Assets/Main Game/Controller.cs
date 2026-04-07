using UnityEngine;

public abstract class Controller : MonoBehaviour
{
    [Header("Shared / Optional")]
    public bool verboseLogs = false;

    protected void Log(string msg)
    {
        if (verboseLogs) Debug.Log($"[{GetType().Name}] {msg}");
    }

    public virtual void Init() { }
}
