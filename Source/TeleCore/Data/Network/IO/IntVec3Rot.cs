using Verse;

namespace TeleCore.Data.Network.IO;

/// <summary>
/// An IntVec3 with a relative direction attached.
/// </summary>
public readonly struct IntVec3Rot
{
    private readonly Rot4 direction;
    private readonly IntVec3 vec;

    //public static implicit operator IntVec3(IntVec3Rot vec) => vec.vec;
    public static implicit operator IntVec3(IntVec3Rot vec) => vec.vec;
    public static implicit operator IntVec3Rot(IntVec3 vec) => new (vec, Rot4.Invalid);

    public Rot4 Direction => direction;
    public IntVec3 IntVec => vec;

    public IntVec3Rot Reverse => new IntVec3Rot(vec + direction.Opposite.FacingCell, direction.Opposite);
    
    public IntVec3Rot(IntVec3 vec, Rot4 direction)
    {
        this.direction = direction;
        this.vec = vec;
    }

    public override string ToString()
    {
        return $"{vec}[{direction}]";
    }

    public override bool Equals(object obj)
    {
        if (obj is IntVec3 otherVec)
        {
            return vec.Equals(otherVec);
        }
        return false;
    }
}

