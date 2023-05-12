using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace TeleCore
{
    public unsafe class ComputeGrid<T> : IExposable, IDisposable where T : unmanaged
    {
        private ComputeBuffer buffer;
        private Map map;
        private NativeArray<T> gridArray;
        private T* gridPtr;
        
        private bool isReady = false;

        public NativeArray<T> Grid => gridArray;
        
        public int Length => gridArray.Length;
        public bool IsReady => isReady;

        //public Array DataArray => gridArray.ToArray(;
        public ComputeBuffer DataBuffer => buffer;
        
        public T this[int i]
        {
            get => gridArray[i];
            private set => gridArray[i] = value;
        }

        public T this[IntVec3 c]
        {
            get => gridArray[c.Index(map)];
            private set => gridArray[c.Index(map)] = value;
        }

        public ComputeGrid(Map map)
        {
            Constructor(map, (_) => default);
        }

        public ComputeGrid(Map map, Func<int, T> factory)
        {
            Constructor(map, factory);
        }
        
        ~ComputeGrid()
        {
            TLog.Warning("Disposing ComputeGrid by GC, make sure to dispose it manually!");
            Dispose();
        }

        public T GetValueByPtr(int idx)
        {
            return gridPtr[idx];
        }

        /// <summary>
        /// Clear internal <see cref="ComputeBuffer"/>
        /// </summary>
        public void Dispose()
        {
            if (!UnityData.IsInMainThread)
            {
                TLog.Warning("ComputeGrid must be disposed on the main thread!");
                return;
            }
            buffer.Dispose();
        }
        
        //
        public void ThreadSafeInit()
        {
            if (!UnityData.IsInMainThread)
            {
                TLog.Warning("ComputeGrid must be initialized on the main thread!");
                return;
            }
            isReady = true;
            buffer = new ComputeBuffer(Length, Marshal.SizeOf(typeof(T)));
            UpdateBuffer();
        }

        private void Constructor(Map map, Func<int, T> factory)
        {
            //Clear buffer on quit
            ApplicationQuitUtility.RegisterQuitEvent(delegate
            {
                buffer.Dispose();
            });
            
            //
            this.map = map;
            
            //
            gridArray = new NativeArray<T>(map.cellIndices.NumGridCells, Allocator.Persistent);
            gridPtr = (T*)gridArray.GetUnsafePtr();
            
            for (int i = 0; i < gridArray.Length; i++)
            {
                gridPtr[i] = factory.Invoke(i);
            }
        }

        public void PullBufferFromGPU()
        {
            if (CheckReadyState(ReadyStateMode.UpdateCPU)) return;
            TLog.Debug("Requesting GPU Data---");
            AsyncGPUReadback.RequestIntoNativeArray(ref gridArray, DataBuffer, UpdateInternalCallBack);
            
            //TFind.TeleRoot.StartCoroutine(UpdateData_Internal());
        }

        public void UpdateBuffer()
        {
            if (CheckReadyState(ReadyStateMode.UpdateBuffer)) return;
            buffer.SetData(gridArray);
        }

        private IEnumerator UpdateData_Internal()
        {
            yield return null;
            yield return null;
        }

        private unsafe void GetPtr()
        {
            AsyncGPUReadback.RequestIntoNativeArray(ref gridArray, DataBuffer, UpdateInternalCallBack);
            //(T*) DataBuffer.GetNativeBufferPtr();
            //DataBuffer.GetData(gridArray);
            //gridPtr = (T*) DataBuffer.GetNativeBufferPtr();
        }

        private void UpdateInternalCallBack(AsyncGPUReadbackRequest request)
        {
            TLog.Message($"Received Data From GPU: {!request.hasError}");
        }
        
        private enum ReadyStateMode
        {
            UpdateCPU,
            UpdateBuffer,
            Setter,
        }
        
        public void SetValues(IEnumerable<IntVec3> positions, Func<IntVec3, T> valueGetter)
        {
            if (CheckReadyState(ReadyStateMode.Setter)) return;
            
            //
            foreach (var c in positions)
            {
                gridPtr[c.Index(map)] = valueGetter.Invoke(c);
            }
            buffer.SetData(gridArray);
        }

        public void SetValue(IntVec3 c, T t)
        {
            if (CheckReadyState(ReadyStateMode.Setter)) return;
            
            //
            gridPtr[c.Index(map)] = t;
            buffer.SetData(gridArray);
        }

        /// <summary>
        /// Sets data to the grid array and does not invoke the ComputeBuffer update.
        /// </summary>
        public void SetValues_Array(IEnumerable<IntVec3> positions, Func<IntVec3, T> valueGetter)
        {
            foreach (var c in positions)
            {
                gridPtr[c.Index(map)] = valueGetter.Invoke(c);
            }
        }
        
        /// <summary>
        /// Sets data to the grid array and does not invoke the ComputeBuffer update.
        /// </summary>
        public void SetValue_Array(IntVec3 c, T t)
        {
            gridPtr[c.Index(map)] = t;
        }
        
        public void ResetValues(IEnumerable<IntVec3> positions, T toVal = default)
        {
            if (CheckReadyState(ReadyStateMode.Setter)) return;
            
            foreach (var c in positions)
            {
                gridPtr[c.Index(map)] = toVal;
            }
            buffer.SetData(gridArray);
        }

        public void ResetValue(IntVec3 c, T toVal = default)
        {
            if (CheckReadyState(ReadyStateMode.Setter)) return;
            
            gridPtr[c.Index(map)] = toVal;
            if (!IsReady) return;
            buffer.SetData(gridArray);
        }

        private bool CheckReadyState(ReadyStateMode mode)
        {
            if (IsReady) return true;

            string msg = "";
            switch (mode)
            {
                case ReadyStateMode.UpdateCPU:
                    msg = "Tried updating the current Grid-Array from GPU data.";
                    break;
                case ReadyStateMode.Setter:
                    msg = "Tried setting new data into buffer - Try setting values in the grid first and updating later.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            TLog.Warning($"Cannot access Compute-Data without safely initializing first!\n{msg}");
            return false;
        }
        
        public void ExposeData()
        {
            
        }
    }
}
