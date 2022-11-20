using UnityEngine;
using Verse;

namespace TeleCore;

public class FlowValueDef : Def
{
    public string labelShort;
    public string valueUnit;
    public Color valueColor;
    //The rate at which value flows between containers
    public float viscosity = 1;
    public float capacityFactor = 1;
    
    //Runtime
    public float FlowRate => 1f / viscosity;
}