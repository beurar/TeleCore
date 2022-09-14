using System;

namespace TeleCore;

[Flags]
public enum NetworkIOMode : byte
{
    Input = 1,
    Output = 2,
    None = 3,
    TwoWay = Input & Output
}