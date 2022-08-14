using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    [StaticConstructorOnStartup]
    public static class TeleContentDB
    {
        private static List<AssetBundle> assetBundles;
        private static Dictionary<string, Shader> lookupShades;
        private static Dictionary<string, ComputeShader> lookupComputeShades;
        private static Dictionary<string, Material> lookupMats;

        static TeleContentDB()
        {
            assetBundles = new();

            //Load AssetBundles
            var path = GetCurrentSystemPath;
            if (!File.Exists(path)) return;
            var files = Directory.GetFiles(path);
            if (files.NullOrEmpty()) return;
            foreach (var file in Directory.GetFiles(GetCurrentSystemPath))
            {
                //Try Load
                AssetBundle bundle = AssetBundle.LoadFromFile(file);
                if (bundle == null)
                {
                    TLog.Warning($"Could not load AssetBundle at: {file}");
                }
                assetBundles.Add(bundle);
            }
        }

        private static string GetCurrentSystemPath
        {
            get
            {
                string pathPart = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    pathPart = "StandaloneOSX";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    pathPart = "StandaloneWindows";
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    pathPart = "StandaloneLinux64";

                return Path.Combine(TeleCoreMod.Mod.Content.RootDir, $@"Resources\Bundles\{pathPart}");
            }
        }

        public static ComputeShader LoadComputeShader(string shaderName)
        {
            lookupComputeShades ??= new Dictionary<string, ComputeShader>();

            foreach (var assetBundle in assetBundles)
            {
                if (!lookupComputeShades.ContainsKey(shaderName))
                    lookupComputeShades[shaderName] = assetBundle.LoadAsset<ComputeShader>(shaderName);
            }

            if (!lookupComputeShades.TryGetValue(shaderName, out var shader) || shader == null)
            {
                Log.Warning($"Could not load shader '{shaderName}'");
                return null;
            }
            return shader;
        }

        public static Shader LoadShader(string shaderName)
        {
            lookupShades ??= new Dictionary<string, Shader>();

            foreach (var assetBundle in assetBundles)
            {
                if (!lookupShades.ContainsKey(shaderName))
                    lookupShades[shaderName] = assetBundle.LoadAsset<Shader>(shaderName);
            }

            if (!lookupShades.TryGetValue(shaderName, out var shader) || shader == null)
            {
                Log.Warning($"Could not load shader '{shaderName}'");
                return ShaderDatabase.DefaultShader;
            }
            return shader;
        }

        public static Material LoadMaterial(string materialName)
        {
            lookupMats ??= new Dictionary<string, Material>();

            foreach (var assetBundle in assetBundles)
            {
                if (!lookupMats.ContainsKey(materialName))
                    lookupMats[materialName] = assetBundle.LoadAsset<Material>(materialName);
            }

            if (!lookupMats.TryGetValue(materialName, out var mat) || mat == null)
            {
                Log.Warning($"Could not load material '{materialName}'");
                return BaseContent.BadMat;
            }
            return mat;
        }
    }
}
