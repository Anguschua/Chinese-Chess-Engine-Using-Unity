public struct EngineSearchResult
{
    public NewBoardController.XQMove bestMove;
    public long timeMs;
    public long memoryBytes;
    public int nodes;

    public EngineSearchResult(NewBoardController.XQMove bestMove, long timeMs, long memoryBytes, int nodes)
    {
        this.bestMove = bestMove;
        this.timeMs = timeMs;
        this.memoryBytes = memoryBytes;
        this.nodes = nodes;
    }
}