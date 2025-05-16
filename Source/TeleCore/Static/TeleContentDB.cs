﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace TeleCore;

[StaticConstructorOnStartup]
public static class TeleContentDB
{
    private static readonly List<AssetBundle> assetBundles;
    private static Dictionary<string, Shader> lookupShades;
    private static Dictionary<string, ComputeShader> lookupComputeShades;
    private static Dictionary<string, Material> lookupMats;
    private static Dictionary<string, Texture2D> lookupTextures;
    
    // public static readonly Shader TextureBlend = LoadShader("TextureBlend");
    // public static readonly Shader FlowMapShader = LoadShader("FlowMapShader");
    // public static readonly Shader FlowMapOnBlend = LoadShader("FlowMapOnBlend");
    //public static readonly ComputeShader GasGridCompute = LoadComputeShader("GasGridCompute");
    //public static readonly ComputeShader GlowFlooderCompute = LoadComputeShader("GlowFlooder");

    internal static readonly Texture2D CustomCursor_Drag = LoadTexture("CursorCustom_Drag");
    internal static readonly Texture2D CustomCursor_Rotate = LoadTexture("CursorCustom_Rotate");
    
    static TeleContentDB()
    {
        assetBundles = new List<AssetBundle>();
        foreach (var pack in LoadedModManager.RunningModsListForReading)
        {
            if(pack.IsOfficialMod || pack.IsCoreMod) continue;
            if (pack.assetBundles == null || pack.assetBundles.loadedAssetBundles.Count <= 0) continue;
            foreach (var assetBundle in pack.assetBundles.loadedAssetBundles)
            {
                assetBundles.Add(assetBundle);
            }
        }

        ListAllAssets();
    }

    public static void ListAllAssets()
    {
        foreach (var bundle in assetBundles)
        {
            var names = bundle.GetAllAssetNames();
            TLog.Debug($"Asset bundle '{bundle.name}' with {names.Length}");
            foreach (var assetName in names)
            {
                TLog.Debug("  - " + assetName);
            }
        }
    }
    
    public static ComputeShader LoadComputeShader(string shaderName)
    {
        return LoadAsset<ComputeShader>(shaderName, lookupComputeShades)!;
    }
    
    public static Shader LoadShader(string shaderName)
    {
        return LoadAsset<Shader>(shaderName, lookupShades)!;
    }
    
    public static Material LoadMaterial(string materialName)
    {
        return LoadAsset<Material>(materialName, lookupMats)!;
    }
    
    public static Texture2D LoadTexture(string textureName)
    {
        return LoadAsset<Texture2D>(textureName, lookupTextures)!;
    }

    private static T? LoadAsset<T>(string name, IDictionary<string, T> lookup) where T : Object
    {
        lookup ??= new Dictionary<string, T>();
        if (!UnityData.IsInMainThread)
        {
            TLog.Error("Trying to load asset on other thread than main.");
            return null;
        }
        if (assetBundles.NullOrEmpty()) return null;

        T asset = null;
        string targetName = name.ToLowerInvariant() + ".shader";

        foreach (var assetBundle in assetBundles)
        {
            string assetPath = assetBundle.GetAllAssetNames()
                .FirstOrDefault(p => p.EndsWith($"/{targetName}", System.StringComparison.OrdinalIgnoreCase));

            if (assetPath != null)
            {
                if (!lookup.ContainsKey(name))
                    lookup[name] = asset = assetBundle.LoadAsset<T>(assetPath);
                break;
            }
        }

        if (asset == null)
        {
            TLog.Warning($"Could not load shader asset '{name}' → looking for '{targetName}'");
            return null;
        }

        return asset;
    }
}