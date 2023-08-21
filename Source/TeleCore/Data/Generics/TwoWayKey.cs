using System.Diagnostics;
using TeleCore.Network.Data;

namespace TeleCore.Generics;

/// <summary>
/// A key to define a two-way relationship between two objects.
/// Meant for tuples of two object of the same type, to ignore ordering of the tuple.
/// </summary>
[DebuggerDisplay("{A} <> {B}")]
public struct TwoWayKey<T>
{
    public T A { get; }
    public T B { get; }

    public static implicit operator TwoWayKey<T>((T, T) tuple)
    {
        return new TwoWayKey<T>(tuple.Item1, tuple.Item2);
    }
    
    public TwoWayKey(T a, T b)
    {
        var stringA = a.ToString();
        var stringB = b.ToString();
        var compare = string.CompareOrdinal(stringA, stringB);
        if (compare > 0)
        {
            A = a;
            B = b;
        }
        else
        {
            A = b;
            B = a;
        }
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (A == null ? 0 : A.GetHashCode());
            hash = hash * 31 + (B == null ? 0 : B.GetHashCode());
            
            return hash;
        }
    }
}