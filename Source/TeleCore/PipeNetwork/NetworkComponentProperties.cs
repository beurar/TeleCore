using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class NetworkComponentProperties
    {
        //Cached Data
        private NetworkRole? networkRole;
        private List<NetworkValueDef> allowedValuesInt;

        //Loaded from XML
        public bool storeEvenly = false;

        public Type workerType = typeof(NetworkComponent);
        public NetworkDef networkDef;
        public ContainerProperties containerProps;
        //TODO NetworkRoleProperties
        public List<NetworkRoleProperties> networkRoleProperties = new();
        public List<NetworkRole> networkRoles = new() { NetworkRole.Transmitter };
        public NetworkDef allowValuesFromNetwork;
        private List<NetworkValueDef> allowedValues;

        public List<NetworkValueDef> AllowedValues
        {
            get
            {
                var list = new List<NetworkValueDef>();
                if (allowValuesFromNetwork != null)
                {
                    list.AddRange(allowValuesFromNetwork.NetworkValueDefs);
                }

                if (!allowedValues.NullOrEmpty())
                {
                    list.AddRange(allowedValues);
                }

                return allowedValuesInt ??= list.Distinct().ToList();
            }
        }

        public NetworkRole NetworkRole
        {
            get
            {
                if (networkRole == null)
                {
                    networkRole = NetworkRole.Transmitter;
                    foreach (var role in networkRoles)
                    {
                        networkRole |= role;
                    }
                }
                return networkRole.Value;
            }
        }
    }
}
