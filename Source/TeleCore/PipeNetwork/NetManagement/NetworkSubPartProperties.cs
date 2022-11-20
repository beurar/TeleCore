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
        //TODO: Shared Container Set Pool - to track capacity sharing
        public List<NetworkDef> shareCapacityWith;
        
        public List<NetworkRoleProperties> networkRoles = new(){ NetworkRole.Transmitter };
        public string subIOPattern;
        
        //
        private class ValueProperties
        {
            public NetworkDef fromDef;
            public List<NetworkValueDef> values;
            
            public void LoadDataFromXmlCustom(XmlNode xmlRoot)
            {
                TLog.Debug($"Loading XML: {xmlRoot.Name}: {xmlRoot.FirstChild.Name}");
                //
                if (xmlRoot.FirstChild.Name == "li")
                {
                    //TLog.Error($"Definition for networkRole not a listing.");
                    values = DirectXmlToObject.ObjectFromXml<List<NetworkValueDef>>(xmlRoot, true);
                    return;
                }
                
                var fromDefNode = xmlRoot.SelectSingleNode(nameof(fromDef));
                if (fromDefNode != null)
                {
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(fromDef)}", fromDefNode.Value);
                }

                //
                var valuesNode = xmlRoot.SelectSingleNode(nameof(values));
                if (valuesNode != null)
                {
                    values = DirectXmlToObject.ObjectFromXml<List<NetworkValueDef>>(valuesNode, true);
                }
            }
        }
        
        private ValueProperties valueProperties;
        
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
                    if (valueProperties.fromDef != null)
                    {
                        list.AddRange(valueProperties.fromDef.NetworkValueDefs);
                    }
                    if (!valueProperties.values.NullOrEmpty())
                    {
                        list.AddRange(valueProperties.values);
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
