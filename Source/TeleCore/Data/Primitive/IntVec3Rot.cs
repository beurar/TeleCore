using Verse;

namespace TeleCore.Primitive;

/// <summary>
/// An IntVec3 with a relative direction attached.
/// </summary>
public readonly struct IntVec3Rot
{
    private readonly Rot4 _dir;
    private readonly IntVec3 _pos;

    public static implicit operator IntVec3(IntVec3Rot vec) => vec._pos;
    public static implicit operator IntVec3Rot(IntVec3 vec) => new (vec, Rot4.Invalid);

    public Rot4 Dir => _dir;
    public IntVec3 Pos => _pos;

    public IntVec3Rot Reverse => new IntVec3Rot(_pos + _dir.Opposite.FacingCell, _dir.Opposite);
    
    public IntVec3Rot(IntVec3 pos, Rot4 dir)
    {
        this._dir = dir;
        this._pos = pos;
    }

    public override string ToString()
    {
        return $"{_pos}[{_dir}]";
    }

    public override bool Equals(object obj)
    {
        if (obj is IntVec3 otherVec)
        {
            return _pos.Equals(otherVec);
        }
        return false;
    }
}
