namespace TeleCore.Generics;

/// <summary>
/// A key to define a two-way relationship between two objects.
/// Meant for tuples of two object of the same type, to ignore ordering of the tuple.
/// </summary>
public struct TwoWayKey<T>
{
    private T A { get; }
    private T B { get; }

    public static implicit operator TwoWayKey<T>((T, T) tuple)
    {
        return new TwoWayKey<T>(tuple.Item1, tuple.Item2);
    }
    
    private TwoWayKey(T a, T b)
    {
        A = a;
        B = b;
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