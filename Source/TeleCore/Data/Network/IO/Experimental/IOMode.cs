using System;

namespace TeleCore.Network.IO.Experimental;

[Flags]
public enum NetworkIOMode : byte
{
    Input = 1, //0001
    Output = 2, //0010
    Visual = 3, //0011
    TwoWay = Input & Output //0011
}