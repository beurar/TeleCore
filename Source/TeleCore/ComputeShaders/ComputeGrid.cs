using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class ComputeGrid<T> : IExposable, IDisposable where T : struct
    {
        private ComputeBuffer buffer;
        private Map map;
        private T[] grid;
        private bool isReady = false;

        public T[] Grid => grid;
        
        public int Length => grid.Length;
        public bool IsReady => isReady;

        public Array DataArray => grid;
        public ComputeBuffer DataBuffer => buffer;

        public T this[int i]
        {
            get => grid[i];
            private set => grid[i] = value;
        }

        public T this[IntVec3 c]
        {
            get => grid[c.Index(map)];
            private set => grid[c.Index(map)] = value;
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
            buffer = new ComputeBuffer(grid.Length, Marshal.SizeOf(typeof(T)));
            buffer.SetData(grid);
        }

        private void Constructor(Map map, Func<int, T> factory)
        {
            //
            ApplicationQuitUtility.RegisterQuitEvent(delegate
            {
                buffer.Dispose();
            });
            
            //
            this.map = map;
            grid = new T[map.cellIndices.NumGridCells];
            for (int i = 0; i < grid.Length; i++)
            {
                grid[i] = factory.Invoke(i);
            }
        }

        public void UpdateCPUData()
        {
            if (CheckReadyState()) return;
            TFind.TeleRoot.StartCoroutine(UpdateData_Internal());
        }

        private IEnumerator UpdateData_Internal()
        {
            yield return null;
            DataBuffer.GetData(grid);
            yield return null;
        }

        public void SetValues(IEnumerable<IntVec3> positions, Func<IntVec3, T> valueGetter)
        {
            if (CheckReadyState()) return;
            
            foreach (var c in positions)
            {
                this[c] = valueGetter.Invoke(c);
            }
            if (!IsReady) return;
            buffer.SetData(grid);
        }

        public void SetValue(IntVec3 c, T t)
        {
            if (CheckReadyState()) return;
            
            this[c] = t;
            if (!IsReady) return;
            buffer.SetData(grid);
        }

        public void ResetValues(IEnumerable<IntVec3> positions, T toVal = default)
        {
            if (CheckReadyState()) return;
            
            foreach (var c in positions)
            {
                this[c] = toVal;
            }
            buffer.SetData(grid);
        }

        public void ResetValue(IntVec3 c, T toVal = default)
        {
            if (CheckReadyState()) return;
            
            this[c] = toVal;
            if (!IsReady) return;
            buffer.SetData(grid);
        }

        private bool CheckReadyState()
        {
            if (IsReady) return true;
            
            TLog.Warning("Cannot use ComputeGrid until it is safely initialized!");
            return false;
        }
        
        public void ExposeData()
        {
            
        }
    }
}
