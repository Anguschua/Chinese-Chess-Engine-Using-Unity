using System;
using UnityEngine;
using Unity.InferenceEngine;

public class PolicyInference : MonoBehaviour
{
    public ModelAsset modelAsset;
    public bool preferGPU = false;

    private Model runtimeModel;
    private Worker worker;

    public const int Channels = 15;
    public const int Rows = 10;
    public const int Cols = 9;
    public const int PolicySize = 90 * 90; // 8100

    void Awake()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, preferGPU ? BackendType.GPUCompute : BackendType.CPU);
        Debug.Log($"[PolicyInference] Backend={(preferGPU ? "GPUCompute" : "CPU")}");
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

    public float[] EvaluatePolicy(float[] inputData)
    {
        int expected = Channels * Rows * Cols;
        if (inputData == null || inputData.Length != expected)
            throw new ArgumentException($"Policy input must have length {expected}.");

        using (Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, Channels, Rows, Cols), inputData))
        {
            worker.Schedule(inputTensor);
            Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
            using (Tensor<float> cpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>)
            {
                float[] output = cpuTensor.DownloadToArray();

                if (output.Length != PolicySize)
                    Debug.LogWarning($"[PolicyInference] Expected {PolicySize} outputs, got {output.Length}");

                return output;
            }
        }
    }
}