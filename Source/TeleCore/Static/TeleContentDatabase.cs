using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace TeleCore.Static;

[StaticConstructorOnStartup]
public static class TRContentDatabase
{
    private static AssetBundle bundleInt;
    private static Dictionary<string, Shader> lookupShades;
    private static Dictionary<string, ComputeShader> lookupComputeShades;
    private static Dictionary<string, Material> lookupMats;
    private static Dictionary<string, Texture2D> lookupTextures;

    //Shaders
    public static readonly Shader TextureBlend = LoadShader("TextureBlend");
    public static readonly Shader FlowMapShader = LoadShader("FlowMapShader");
    public static readonly Shader FlowMapOnBlend = LoadShader("FlowMapOnBlend");

    public static readonly ComputeShader GasGridCompute = LoadComputeShader("GasGridCompute");
    public static readonly ComputeShader GlowFlooderCompute = LoadComputeShader("GlowFlooder");

    internal static readonly Texture2D
        CustomCursor_Drag =
            LoadTexture("CursorCustom_Drag"); //ContentFinder<Texture2D>.Get("UI/Cursors/CursorCustom_Drag", true);

    internal static readonly Texture2D
        CustomCursor_Rotate =
            LoadTexture("CursorCustom_Rotate"); //ContentFinder<Texture2D>.Get("UI/Cursors/CursorCustom_Rotate", true);

    public static AssetBundle TeleCoreBundle
    {
        get
        {
            if (bundleInt == null)
            {
                var pathPart = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    pathPart = "StandaloneOSX";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    pathPart = "StandaloneWindows64";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    pathPart = "StandaloneLinux64";

                var mainBundlePath = Path.Combine(TeleCoreMod.Mod.Content.RootDir, $@"UnityAssets\{pathPart}\telecore");
                bundleInt = AssetBundle.LoadFromFile(mainBundlePath);
                TLog.Message($"AssetBundle Loaded: {bundleInt != null}");
            }

            return bundleInt;
        }
    }

    //Materials
    //public static readonly Shader AlphaShader = LoadShader("AlphaShader");
    //public static readonly Material AlphaShaderMaterial = LoadMaterial("ShaderMaterial");

    public static ComputeShader LoadComputeShader(string shaderName)
    {
        if (lookupComputeShades == null)
            lookupComputeShades = new Dictionary<string, ComputeShader>();
        if (!lookupComputeShades.ContainsKey(shaderName))
            lookupComputeShades[shaderName] = TeleCoreBundle.LoadAsset<ComputeShader>(shaderName);

        var shader = lookupComputeShades[shaderName];
        if (shader == null)
        {
            TLog.Warning($"Could not load shader '{shaderName}'");
            return null;
        }

        return shader;
    }

    public static Shader LoadShader(string shaderName)
    {
        if (lookupShades == null)
            lookupShades = new Dictionary<string, Shader>();
        if (!lookupShades.ContainsKey(shaderName))
            lookupShades[shaderName] = TeleCoreBundle.LoadAsset<Shader>(shaderName);

        var shader = lookupShades[shaderName];
        if (shader == null)
        {
            TLog.Warning($"Could not load shader '{shaderName}'");
            return ShaderDatabase.DefaultShader;
        }

        return shader;
    }

    public static Material LoadMaterial(string materialName)
    {
        if (lookupMats == null)
            lookupMats = new Dictionary<string, Material>();
        if (!lookupMats.ContainsKey(materialName))
            lookupMats[materialName] = TeleCoreBundle.LoadAsset<Material>(materialName);

        var mat = lookupMats[materialName];
        if (mat == null)
        {
            TLog.Warning($"Could not load material '{materialName}'");
            return BaseContent.BadMat;
        }

        return mat;
    }

    public static Texture2D LoadTexture(string textureName)
    {
        if (lookupTextures == null)
            lookupTextures = new Dictionary<string, Texture2D>();
        if (!lookupTextures.ContainsKey(textureName))
            lookupTextures[textureName] = TeleCoreBundle.LoadAsset<Texture2D>(textureName);

        var texture = lookupTextures[textureName];
        if (texture == null)
        {
            TLog.Warning($"Could not load Texture2D '{textureName}'");
            return BaseContent.BadTex;
        }

        return texture;
    }
}