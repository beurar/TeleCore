using Verse.AI;

namespace TeleCore;

public struct PawnPathResult
{
    private PawnPath _result;
    private string _reason;

    public PawnPath Path => _result;
    public string Reason => _reason;
            
    public PawnPathResult(PawnPath result, string reason = null)
    {
        _result = result;
        _reason = reason;
    }
}