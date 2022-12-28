using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using JetBrains.Annotations;
using Verse;

namespace TeleCore
{
    public class NetworkSubPartProperties
    {
        //Cached Data
        [Unsaved]
        private NetworkRole networkRole;
        [Unsaved]
        private List<NetworkValueDef> allowedValuesInt = null!;
        [Unsaved]
        private Dictionary<NetworkRole, List<NetworkValueDef>> allowedValuesByRoleInt = null!;

        //
        public bool requiresController;

        //Loaded from XML
        public Type workerType = typeof(NetworkSubPart);
        public NetworkDef networkDef;
        public ContainerProperties containerProps;
        private NetworkValueFilter defFilter;
        //TODO: Shared Container Set Pool - to track capacity sharing
        public List<NetworkDef> shareCapacityWith;
        
        public List<NetworkRoleProperties> networkRoles = new(){ NetworkRole.Transmitter };
        public string subIOPattern;
        
        //
        private class NetworkValueFilter
        {
            public NetworkDef? fromDef;
            public List<NetworkValueDef>? values;
            
            public void LoadDataFromXmlCustom(XmlNode xmlRoot)
            {
                if (xmlRoot.FirstChild.Name == "li")
                {
                    values = DirectXmlToObject.ObjectFromXml<List<NetworkValueDef>>(xmlRoot, true);
                    return;
                }
                
                var fromDefNode = xmlRoot.SelectSingleNode(nameof(fromDef));
                if (fromDefNode != null)
                {
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(fromDef)}", fromDefNode.InnerText);
                }

                //
                var valuesNode = xmlRoot.SelectSingleNode(nameof(values));
                if (valuesNode != null)
                {
                    values = DirectXmlToObject.ObjectFromXml<List<NetworkValueDef>>(valuesNode, true);
                }
            }
        }

        //TODO: Add default All 
        public Dictionary<NetworkRole, List<NetworkValueDef>> AllowedValuesByRole
        {
            get
            {
                if (allowedValuesByRoleInt == null)
                {
                    allowedValuesByRoleInt = new();
                    foreach (var role in networkRoles)
                    {
                        if (role.HasSubValues && role != NetworkRole.Transmitter)
                        {
                            allowedValuesByRoleInt.Add(role, role.subValues);
                            continue;
                        }
                        allowedValuesByRoleInt.Add(role, AllowedValues);
                    }
                }
                return allowedValuesByRoleInt;
            }
        }

        public List<NetworkValueDef> AllowedValues
        {
            get
            {
                if (allowedValuesInt == null)
                {
                    var list = new List<NetworkValueDef>();
                    if (defFilter.fromDef != null)
                    {
                        list.AddRange(defFilter.fromDef.NetworkValueDefs);
                    }
                    if (!defFilter.values.NullOrEmpty())
                    {
                        list.AddRange(defFilter.values);
                    }
                    allowedValuesInt = list.Distinct().ToList();
                }
                return allowedValuesInt;
            }
        }

        public NetworkRole NetworkRole
        {
            get
            {
                
                if (networkRole == 0x0000)
                {
                    networkRole = NetworkRole.Transmitter;
                    foreach (var role in networkRoles)
                    {
                        networkRole |= role;
                    }
                }
                return networkRole;
            }
        }
    }
}
