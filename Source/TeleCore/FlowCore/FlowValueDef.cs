using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TeleCore;

public class FlowValueDef : Def
{
    public string labelShort;
    public string valueUnit;
    public Color valueColor;

    public bool sharesCapacity;
    
    //The rate at which value flows between containers
    public float viscosity = 1;
    public float capacityFactor = 1;
    
    //Runtime
    public float FlowRate => 1f / viscosity;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
        {
            yield return error;
        }

        if (viscosity == 0)
        {
            yield return $"{nameof(viscosity)} cannot be 0!";
        }
    }
}