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
    public static class Scribe_Delegate
    {
        private static byte[] _TempBytes;
        public static void Look<TDelegate>(ref TDelegate @delegate, string name) where TDelegate : Delegate
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                TLog.Debug($"Saving delegate... ToTarget: {@delegate.Target.GetType()}");
                _TempBytes = MethodConstructor.Serialize(@delegate);
                DataExposeUtility.ByteArray(ref _TempBytes, name);
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                DataExposeUtility.ByteArray(ref _TempBytes, name);
                @delegate = MethodConstructor.Deserialize<TDelegate>(_TempBytes);
                _TempBytes = null;
            }
        }

        private sealed class MethodConstructor
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

    [Serializable]
    class SerializableDelegate<T> where T : class
    {
        [SerializeField]
        private object _target;
        [SerializeField]
        private string _methodName = "";
        [SerializeField]
        private byte[] _serialData = { };

        static SerializableDelegate()
        {
            if (!typeof(T).IsSubclassOf(typeof(Delegate)))
            {
                throw new InvalidOperationException(typeof(T).Name + " is not a delegate type.");
            }
        }

        public void SetDelegate(T action)
        {
            if (action == null)
            {
                _target = null;
                _methodName = "";
                _serialData = new byte[] { };
                return;
            }

            var delAction = action as Delegate;
            if (delAction == null)
            {
                throw new InvalidOperationException(typeof(T).Name + " is not a delegate type.");
            }


            _target = delAction.Target as UnityEngine.Object;

            if (_target != null)
            {
                _methodName = delAction.Method.Name;
                _serialData = null;
            }
            else
            {
                //Serialize the data to a binary stream
                using (var stream = new MemoryStream())
                {
                    (new BinaryFormatter()).Serialize(stream, action);
                    stream.Flush();
                    _serialData = stream.ToArray();
                }
                _methodName = null;
            }
        }

        public T CreateDelegate()
        {
            if (_serialData.Length == 0 && _methodName == "")
            {
                return null;
            }

            if (_target != null)
            {
                return Delegate.CreateDelegate(typeof(T), _target, _methodName) as T;
            }

            using (var stream = new MemoryStream(_serialData))
            {
                return (new BinaryFormatter()).Deserialize(stream) as T;
            }
        }
    }

}
