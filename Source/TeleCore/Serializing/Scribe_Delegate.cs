using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using HarmonyLib;
using MonoMod.Utils;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class ScribeDelegate<TDelegate> : IExposable where TDelegate : Delegate
    {
        public TDelegate @delegate;
        public string targetID;
        private static byte[] _TempBytes;

        public ScribeDelegate(TDelegate action)
        {
            this.@delegate = action;
        }

        public object Target => @delegate.Target;
        
        public void ExposeData()
        {
            var isSaving = Scribe.mode == LoadSaveMode.Saving;
            
            if (isSaving && @delegate.Target is ILoadReferenceable referenceable)
                targetID = referenceable.GetUniqueLoadID();

            Scribe_Values.Look(ref targetID, nameof(targetID));
            
            if (isSaving)
            {
                TLog.Debug($"Saving delegate... ToTarget: {@delegate.Target.GetType()}");
                _TempBytes = MethodConstructor.Serialize(@delegate);
                DataExposeUtility.ByteArray(ref _TempBytes, "delegateBytes");
            }
            
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                DataExposeUtility.ByteArray(ref _TempBytes, "delegateBytes");
                @delegate = MethodConstructor.Deserialize<TDelegate>(_TempBytes);
                _TempBytes = null;
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Scribe.loader.crossRefs.loadedObjectDirectory.ObjectWithLoadID<object>(targetID);
            }
        }
    }
    
    internal sealed class MethodConstructor
    {
        public static byte[] Serialize(Delegate d)
        {
            TLog.Debug($"Serializing: {d}");
            return Serialize(d.Method);
        }

        public static byte[] Serialize(MethodInfo method)
        {
            using MemoryStream stream = new MemoryStream();
            new BinaryFormatter().Serialize(stream, method);
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }

        public static MethodInfo Deserialize(byte[] data)
        {
            using MemoryStream stream = new MemoryStream(data);
            var method = (MethodInfo)new BinaryFormatter().Deserialize(stream);
            return method;
        }

        public static TDelegate Deserialize<TDelegate>(byte[] data) where TDelegate : class
        {
            var method = Deserialize(data);
            TLog.Message($"{method.DeclaringType.DeclaringType}");
            return method.CreateDelegate(typeof(TDelegate), Activator.CreateInstance(method.DeclaringType)) as TDelegate;
        }
    }
}
