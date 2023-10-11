using System.Collections.Generic;
using System.IO;
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
        
        LoadFrom(TeleCoreMod.Mod);
    }

    private static void LoadFrom(Mod mod)
    {
        var path = GetCurrentSystemPath(mod);
        if (!Directory.Exists(path)) return;
        var files = Directory.GetFiles(path);
        if (files.NullOrEmpty()) return;
        foreach (var file in files)
        {
            var bundle = AssetBundle.LoadFromFile(file);
            if (bundle == null)
            {
                TLog.Warning($"Could not load AssetBundle at: {file}");
                return;
            }
            
            TLog.DebugSuccess($"Successfully loaded AssetBundle: {Path.GetFileName(file)}");
            
            assetBundles.Add(bundle);
            foreach (var name in bundle.GetAllAssetNames())
            {
                TLog.Debug($"Loaded: {name}");
            }
        }
    }
    
    private static string GetCurrentSystemPath(Mod mod)
    {
        var pathPart = "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            pathPart = "StandaloneOSX";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            pathPart = "StandaloneWindows";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            pathPart = "StandaloneLinux64";

        return Path.Combine(mod.Content.RootDir, $@"Resources\Bundles\{pathPart}");
    }

    public static ComputeShader LoadComputeShader(string shaderName)
    {
        if(assetBundles.NullOrEmpty()) return null;
        
        lookupComputeShades ??= new Dictionary<string, ComputeShader>();
        foreach (var assetBundle in assetBundles)
        {
            if (!lookupComputeShades.ContainsKey(shaderName))
                lookupComputeShades[shaderName] = assetBundle.LoadAsset<ComputeShader>(shaderName);

        }

        if (!lookupComputeShades.TryGetValue(shaderName, out var shader) || shader == null)
        {
            TLog.Warning($"Could not load shader '{shaderName}'");
            return null;
        }

        return shader;
    }

    public static Shader LoadShader(string shaderName)
    {
        if(assetBundles.NullOrEmpty()) return null;
        
        lookupShades ??= new Dictionary<string, Shader>();

        foreach (var assetBundle in assetBundles)
        {
            if (!lookupShades.ContainsKey(shaderName))
                lookupShades[shaderName] = assetBundle.LoadAsset<Shader>(shaderName);
        }

        if (!lookupShades.TryGetValue(shaderName, out var shader) || shader == null)
        {
            TLog.Warning($"Could not load shader '{shaderName}'");
            return ShaderDatabase.DefaultShader;
        }

        return shader;
    }

    public static Material LoadMaterial(string materialName)
    {
        lookupMats ??= new Dictionary<string, Material>();

        foreach (var assetBundle in assetBundles)
            if (!lookupMats.ContainsKey(materialName))
                lookupMats[materialName] = assetBundle.LoadAsset<Material>(materialName);

        if (!lookupMats.TryGetValue(materialName, out var mat) || mat == null)
        {
            TLog.Warning($"Could not load material '{materialName}'");
            return BaseContent.BadMat;
        }

        return mat;
    }
    
    public static Texture2D LoadTexture(string textureName)
    {
        if(assetBundles.NullOrEmpty()) return null;
        
        if (lookupTextures == null)
            lookupTextures = new Dictionary<string, Texture2D>();
        
        
        //TODO:
        if (!lookupTextures.ContainsKey(textureName))
        {
            //lookupTextures[textureName] = TeleCoreBundle.LoadAsset<Texture2D>(textureName);
        }

        var texture = lookupTextures[textureName];
        if (texture == null)
        {
            TLog.Warning($"Could not load Texture2D '{textureName}'");
            return BaseContent.BadTex;
        }

        return texture;
    }
}