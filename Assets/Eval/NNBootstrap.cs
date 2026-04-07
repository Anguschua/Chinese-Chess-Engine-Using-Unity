using UnityEngine;

// Not Used
public class NNBootstrap : MonoBehaviour
{
    public NNInference valueInference;

    void Awake()
    {
        Evaluator.nnInference = valueInference;
    }
}