using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace TeleCore
{
    public class NetworkPartSet
    {
        private NetworkDef def;
        private INetworkSubPart parent;
        private string cachedString;

        private INetworkSubPart controller;
        private readonly HashSet<INetworkSubPart> fullSet;
        private readonly Dictionary<NetworkRole, HashSet<INetworkSubPart>> structuresByRole;
        
        public HashSet<INetworkSubPart> this[NetworkRole role]
        {
            get => structuresByRole.TryGetValue(role, out var value) ? value : null;
        }

        public HashSet<INetworkSubPart> FullSet => fullSet;
        public INetworkSubPart Controller => controller;

        public NetworkPartSet(NetworkDef def, INetworkSubPart parent)
        {
            this.def = def;
            this.parent = parent;

            fullSet = new HashSet<INetworkSubPart>();
            structuresByRole = new Dictionary<NetworkRole, HashSet<INetworkSubPart>>();
            foreach (NetworkRole role in Enum.GetValues(typeof(NetworkRole)))
            {
                structuresByRole.Add(role, new HashSet<INetworkSubPart>());
            }
        }

        public void Notify_ParentDestroyed()
        {
            foreach (INetworkSubPart part in fullSet)
            {
                part.DirectPartSet.RemoveComponent(parent);
            }
        }

        //
        public bool AddNewComponent(INetworkSubPart part)
        {
            if (part == null) return false;
            if (!AddComponent(part)) return false;
            if (parent != null)
            {
                part.DirectPartSet.AddComponent(parent);
            }
            return true;
        }

        private bool AddComponent(INetworkSubPart part)
        {
            if (part.NetworkDef != def) return false;
            if (FullSet.Contains(part)) return false;
            FullSet.Add(part);

            //Special Case
            if (part.NetworkRole.HasFlag(NetworkRole.Controller))
            {
                controller = part;
            }

            //
            foreach (var flag in part.NetworkRole.AllFlags())
            {
                structuresByRole[flag].Add(part);
            }

            //
            UpdateString();
            return true;
        }

        public void RemoveComponent(INetworkSubPart part)
        {
            if (!FullSet.Contains(part)) return;

            //Special Case
            if (part.NetworkRole.HasFlag(NetworkRole.Controller))
            {
                controller = null;
                return;
            }

            //
            foreach (var flag in part.NetworkRole.AllFlags())
            {
                structuresByRole[flag].Remove(part);
            }

            //
            FullSet.Remove(part);
            UpdateString();
        }

        private void UpdateString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CONTROLLER: {Controller?.Parent.Thing}");
            foreach (NetworkRole role in Enum.GetValues(typeof(NetworkRole)))
            {
                sb.AppendLine($"{role}: ");
                foreach (var part in structuresByRole[role])
                {
                    sb.AppendLine($"    - {part.Parent.Thing}");
                }
            }
            sb.AppendLine($"Total Count: {FullSet.Count}");
            cachedString = sb.ToString();
        }

        public override string ToString()
        {
            if (cachedString == null)
                UpdateString();
            return cachedString;
        }
    }
}
