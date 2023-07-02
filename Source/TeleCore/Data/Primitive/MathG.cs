using System;
using System.Collections.Generic;
using System.Linq;

namespace TeleCore.Primitive;

public static class MathG
{
    public static T Clamp<T>(T value, T min, T max) where T : struct
    {
        return NumericLibrary<T>.Clamp(value, min, max);
    }
    
    public static T Min<T>(T a, T b) where T : struct
    {
        return NumericLibrary<T>.Min(a, b);
    }
    
    public static T Max<T>(T a, T b) where T : struct
    {
        return NumericLibrary<T>.Max(a, b);
    }

    public static T Sum<T>(this IEnumerable<T> values) where T : struct
    {
        return NumericLibrary<T>.Sum(values);
    }

    public static T Abs<T>(Numeric<T> value) where T : struct
    {
        return NumericLibrary<T>.Abs(value);
    }
}