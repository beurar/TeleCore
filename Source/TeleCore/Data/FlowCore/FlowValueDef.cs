using System.Collections.Generic;
using TeleCore.FlowCore;
using UnityEngine;
using Verse;

namespace TeleCore;

public static class FlowValueDefDB<TDef>
    where TDef : FlowValueDef
{
    public static Dictionary<TDef, FlowValueCollectionDef<TDef>> _collections = new();
    public static Dictionary<TDef, FlowValueCollectionDef<TDef>> DB => _collections;

    public static void ResolveDef(TDef def)
    {
        if (_collections.TryGetValue(def, out var collectionDef) == false)
        {
            collectionDef = new FlowValueCollectionDef<TDef>();
            _collections.Add(def, collectionDef);
        }

        collectionDef.Notify_ResolvedFlowValueDef(def);
    }
}

public class FlowValueDef : Def
{
    public float capacityFactor = 1;
        
    public string labelShort;

    public bool sharesCapacity;
    public Color valueColor = Color.white;
    public string valueUnit;

    //The rate at which value flows between containers
    public float viscosity = 1;
    public double friction;

    public FlowValueCollectionDef<TDef> CollectionDef<TDef>() where TDef : FlowValueDef
    {
        return FlowValueDefDB<TDef>.DB[this];
    }
    
    //Runtime
    public float FlowRate => 1f / viscosity;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;

        if (viscosity == 0) yield return $"{nameof(viscosity)} cannot be 0!";
    }

    public override void ResolveReferences()
    {
        base.ResolveReferences();
        if (labelShort.NullOrEmpty()) labelShort = label;

        //
        FlowValueDefDB.ResolveDef(this);
    }
}