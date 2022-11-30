using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Profile;
using XmlNode = System.Xml.XmlNode;

namespace TeleCore.Loading;

public static class XMLPatches
{
    [HarmonyPatch(typeof(LoadedLanguage), nameof(LoadedLanguage.LoadData))]
    public static class LoadDataPatch
    {
        public static void Postfix(LoadedLanguage __instance)
        {
            TLog.Debug($"Loaded: {__instance}");
        }
    }
    
    /*
    [HarmonyPatch]
    internal static class MonoMethod_MakeGenericMethod
    {
        [HarmonyTargetMethod]
        static MethodInfo TargetMethod()
        {
            var type = AccessTools.TypeByName("MonoMethod");
            return AccessTools.Method(type, "MakeGenericMethod", new[] {typeof(Type[])});
        }
        
        [HarmonyPostfix]
        public static void Postfix(Type[] methodInstantiation, ref MethodInfo __result)
        {
            if (__result.Name == nameof(DirectXmlToObject.ListFromXmlReflection))
            {
                TLog.Debug($"Creating GenericMethod [{__result.DeclaringType}]{__result.Name}: ({__result.GetGenericArguments().ToStringSafeEnumerable()} ..|.. {methodInstantiation.ToStringSafeEnumerable()})");
                var genericType = __result.GetGenericArguments()[0];
                if (DirectXmlToObject.listFromXmlMethods.TryGetValue(genericType, out var func))
                {
                    MethodInfo method = typeof(MonoMethod_MakeGenericMethod).GetMethod("ListRootChanger", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    Type[] genericArguments = genericType.GetGenericArguments();
                    //method.MakeGenericMethod(genericArguments)
                    TeleCoreMod.TeleCore.Patch(func.Method, null, new HarmonyMethod(method.MakeGenericMethod(genericArguments)));   
                    //TeleCoreMod.TeleCore.Patch(func.Method, null, new HarmonyMethod(typeof(MonoMethod_MakeGenericMethod), nameof(ListRootChanger), new Type[]{typeof(XmlNode)}));   
                }
            }
        }
        
        public static void ListRootChanger<T>(XmlNode listRootNode)
        {
            TLog.Debug("Postfixing list node");
            return;
        }
    }
    */
}