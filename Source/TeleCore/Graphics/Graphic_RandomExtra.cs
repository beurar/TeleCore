using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using TeleCore.DefExtensions;
using UnityEngine;
using Verse;

namespace TeleCore;

public class Graphic_RandomExtra : Graphic_Random
{
    public float ParamRandChance => data.shaderParameters.FirstOrDefault(p => p.name == "_TeleFakeParamRandChance").value.x;

    public override Material MatSingle
    {
        get
        {
            if (Rand.Chance(ParamRandChance))
                return BaseContent.ClearMat;
            return base.MatSingle;
        }
    }

    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
    {
        if (newColorTwo != Color.white)
            Log.ErrorOnce("Cannot use Graphic_RandomExtra.GetColoredVersion with a non-white colorTwo.", 9910251);
        return GraphicDatabase.Get<Graphic_RandomExtra>(path, newShader, drawSize, newColor, Color.white, data);
    }

    public override Material MatSingleFor(Thing thing)
    {
        if (Rand.Chance(ParamRandChance))
            return BaseContent.ClearMat;
        if (thing == null) 
            return MatSingle;
        return SubGraphicFor(thing).MatSingle;
    }
    
}