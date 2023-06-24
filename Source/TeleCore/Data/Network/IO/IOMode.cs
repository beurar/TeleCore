using System;

namespace TeleCore.Network.IO;

[Flags]
public enum NetworkIOMode : byte
{
    Input = 1,                  //0001
    Output = 2,                 //0010
    TwoWay = Input & Output     //0011
}