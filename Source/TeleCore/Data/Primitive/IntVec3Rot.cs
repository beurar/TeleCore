using Verse;

namespace TeleCore.Primitive;

/// <summary>
///     An IntVec3 with a relative direction attached.
/// </summary>
public readonly struct IntVec3Rot
{
    public static implicit operator IntVec3(IntVec3Rot vec)
    {
        return vec.Pos;
    }

    public static implicit operator IntVec3Rot(IntVec3 vec)
    {
        return new IntVec3Rot(vec, Rot4.Invalid);
    }

    public Rot4 Dir { get; }

    public IntVec3 Pos { get; }

    public IntVec3Rot Reverse => new(Pos + Dir.Opposite.FacingCell, Dir.Opposite);
    public static IntVec3Rot Invalid => new(IntVec3.Invalid, Rot4.Invalid);

    public IntVec3Rot(IntVec3 pos, Rot4 dir)
    {
        Dir = dir;
        Pos = pos;
    }

    public override string ToString()
    {
        return $"{Pos}[{Dir}]";
    }

    public override bool Equals(object obj)
    {
        if (obj is IntVec3 otherVec) return Pos.Equals(otherVec);
        return false;
    }
}