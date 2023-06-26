using System;
using UnityEngine;

namespace TeleCore.Network.IO;

[Flags]
public enum NetworkIOMode : byte
{
    None = 0,
    Input = 1,                  //0001
    Output = 2,                 //0010
    Visual = 8,                 //1000
    TwoWay = Input | Output,    //0011
    
    ForRender = Visual | Input | Output
}