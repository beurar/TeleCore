using System;
using System.Collections.Generic;
using TeleCore.FlowCore;
using TeleCore.Network.Data;
using TeleCore.Network.IO;
using TeleCore.Primitive;

namespace TeleCore.Network.Flow
{
    /// <summary>
    /// Logical container for fluid volumes inside a network.
    /// </summary>
    public class NetworkVolume : FlowVolume<NetworkValueDef>
    {
        /* ──────────────  Convenience API  ────────────── */

        /// <summary>Current stored amount of <paramref name="def"/>.</summary>
        public double Get(NetworkValueDef def)
        {
            return StoredValueOf(def);          // helper from base class
        }

        /// <summary>Maximum storable amount of <paramref name="def"/> in this volume.</summary>
        public double GetCapacity(NetworkValueDef def)
        {
            return CapacityOf(def);             // overridden below
        }

        /// <summary>Total of *all* value types currently stored.</summary>
        public double Total
        {
            get
            {
                double sum = 0d;
                foreach (var def in _config.AllowedValues)        // relies on config list
                    sum += StoredValueOf(def);
                return sum;
            }
        }

        /* ──────────────  Capacity / overflow logic  ────────────── */

        public override double CapacityOf(NetworkValueDef def)
        {
            if (_config.shareCapacity)
                return _config.capacity;
            return base.CapacityOf(def);
        }

        public override bool IsFull(NetworkValueDef def)
        {
            if (_config.shareCapacity)
                return StoredValueOf(def) >= CapacityOf(def);
            return base.IsFull(def);
        }

        protected override double ExcessFor(NetworkValueDef def, double amount)
        {
            if (_config.shareCapacity)
                return Math.Max(StoredValueOf(def) + amount - CapacityOf(def), 0.0);
            return base.ExcessFor(def, amount);
        }

        public override double MaxCapacity
        {
            get
            {
                if (_config.shareCapacity)
                    return CapacityPerType * _config.AllowedValues.Count;
                return base.MaxCapacity;
            }
        }

        /* ──────────────  Ctors  ────────────── */

        public NetworkVolume() : base() { }

        public NetworkVolume(FlowVolumeConfig<NetworkValueDef> config) : base(config) { }
    }
}
