using System;
using System.Collections;
using System.Collections.Generic;
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
        private static byte[] _TempBytes;
        
        public static explicit operator TDelegate(ScribeDelegate<TDelegate> e) => e.@delegate;

        public ScribeDelegate(){}

        public ScribeDelegate(TDelegate action)
        {
            this.@delegate = action;
        }

        public object Target => @delegate.Target;

        private object[] _universal;
        private List<LookMode> _lookModes = null;
        private List<Type> _types = null;

        private MethodInfo _method;
        
        public void ExposeData()
        {
            var isSaving = Scribe.mode == LoadSaveMode.Saving;
            var isLoading = Scribe.mode == LoadSaveMode.LoadingVars;

            Type declaringType = null;
            FieldInfo[] fields = null;
            
            //

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                TLog.Debug("SAVING DELEGATE");
                
                //
                TLog.Debug($"Saving delegate... ToTarget: {@delegate.Target.GetType()}");
                _TempBytes = MethodConstructor.Serialize(@delegate);
                DataExposeUtility.ByteArray(ref _TempBytes, "delegateBytes");
                
                //
                declaringType = @delegate.Method.DeclaringType;
                fields = declaringType.GetFields();
                _universal = new object[fields.Length];
                TLog.Debug($"Saving Type: {declaringType} with fields: {fields.ToStringSafeEnumerable()}");
                
                //
                var typesLooks = LookModes(@delegate.Target, fields);
                _lookModes = typesLooks.Item2;
                _types = typesLooks.Item1;
                Scribe_Collections.Look(ref _lookModes, "lookModes", LookMode.Value);
                Scribe_Collections.Look(ref _types, "types", LookMode.Value);
             
                //
                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    Type unType = _types[i];
                    
                    object val =  field.GetValue(@delegate.Target);
                    TryScribe(ref val, ref unType, field, _lookModes[i]);
                }
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                TLog.Debug("LOADING DELEGATE");
                
                //
                DataExposeUtility.ByteArray(ref _TempBytes, "delegateBytes");
                _method = MethodConstructor.Deserialize(_TempBytes);
                _TempBytes = null;
                
                //
                declaringType = _method.DeclaringType;
                fields = declaringType.GetFields();
                _universal = new object[fields.Length];
                TLog.Debug($"Loading Type: {declaringType} with fields: {fields.ToStringSafeEnumerable()}");
                
                //
                Scribe_Collections.Look(ref _lookModes, "lookModes", LookMode.Value);
                Scribe_Collections.Look(ref _types, "types", LookMode.Value);
                TLog.Debug($"Got lookModes: {_lookModes.ToStringSafeEnumerable()}");
                
                //
                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    Type unType = _types[i];
                    
                    object val = null;
                    TryScribe(ref val, ref unType, field, _lookModes[i]);
                    _universal[i] = val;
                }
            }

            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                TLog.Debug("RESOLVING DELEGATE");
                
                declaringType = _method.DeclaringType;
                fields = declaringType.GetFields();

                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    Type unType = _types[i];
                    
                    object val = _universal[i];
                    TryScribe(ref val, ref unType, field, _lookModes[i]);
                    _universal[i] = val;
                }
            }
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                TLog.Debug("POSTLOADING DELEGATE");
                
                declaringType =_method.DeclaringType;
                fields = declaringType.GetFields();

                object newObj = Activator.CreateInstance(_method.DeclaringType);
                for (var s = 0; s < fields.Length; s++)
                {
                    var field = fields[s];
                    field.SetValue(newObj, _universal[s]);
                    TLog.Debug($"Setting value while loading: {field.Name}: {_universal[s]}");
                }
                
                @delegate = _method.CreateDelegate(typeof(TDelegate), newObj) as TDelegate;

                //
                _types = null;
                _lookModes = null;
            }
        }

        private (List<Type>, List<LookMode>) LookModes(object target, FieldInfo[] infos)
        {
            LookMode[] looks = new LookMode[infos.Length];
            Type[] types = new Type[infos.Length];
            for (var l = 0; l < infos.Length; l++)
            {
                var val = infos[l].GetValue(target);
                types[l] = val.GetType();
                if (val is ILoadReferenceable lr)
                {
                    looks[l] = LookMode.Reference;
                }

                if (val is Def)
                {
                    looks[l] = LookMode.Def;
                }

                if (types[l].IsValueType)
                {
                    looks[l] = LookMode.Value;
                }
            }
            return (types.ToList(),looks.ToList());
        }
        
        private void TryScribe(ref object val, ref Type valType, FieldInfo field, LookMode mode)
        { 
            TLog.Message($"Trying to scribe {val} of {valType} in {field} with {mode}");
            switch (mode)
            {
                case LookMode.Value:
                    Scribe_Values.Look(ref val, field.Name);
                    break;
                case LookMode.Reference:
                    ILoadReferenceable tempRef = null;
                    if(Scribe.mode == LoadSaveMode.Saving)
                        tempRef = (ILoadReferenceable)val;
                    Scribe_References.Look(ref tempRef, field.Name);
                    val = tempRef;
                    break;
                case LookMode.Def:
                    Def valDef = null;
                    if(Scribe.mode == LoadSaveMode.Saving)
                        valDef = (Def)val;
                
                    Scribe_Defs.Look(ref valDef, field.Name);
                    val = valDef;
                    break;
                default:
                    Scribe_Universal.Look(ref val, field.Name, mode, ref valType);
                    break;
            }
        }
    }
    
    internal sealed class MethodConstructor
    {
        public static byte[] Serialize(Delegate d)
        {
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
    }
}
