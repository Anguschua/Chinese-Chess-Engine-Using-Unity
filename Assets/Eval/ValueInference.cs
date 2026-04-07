using System;
using UnityEngine;
using Unity.InferenceEngine;

// Not Used
public class NNInference : MonoBehaviour
{
    [Header("Assign the imported ONNX model asset")]
    public ModelAsset modelAsset;

    private Model runtimeModel;
    private Worker worker;

    public const int Channels = 15;
    public const int Rows = 10;
    public const int Cols = 9;

    void Awake()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

    public float EvaluateBoard(float[] inputData)
    {
        int expected = Channels * Rows * Cols;
        if (inputData == null || inputData.Length != expected)
            throw new ArgumentException($"Input must have length {expected}.");

        using Tensor<float> inputTensor = new Tensor<float>(
            new TensorShape(1, Channels, Rows, Cols),
            inputData
        );

        worker.Schedule(inputTensor);

        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
        using Tensor<float> gpuTensor = outputTensor.ReadbackAndClone() as Tensor<float>;

        float[] output = gpuTensor.DownloadToArray();
        return output[0];
    }
}