using System;
using System.Collections.Generic;

[Serializable]
public class ReplayRecord
{
    public string replayId;
    public string displayName;
    public string dateTimeText;
    public List<ReplayMoveData> moves = new List<ReplayMoveData>();
}