using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class NetworkSubPart : INetworkSubPart, IContainerHolderStructure, IExposable
    {
        //
        private Gizmo_NetworkInfo networkInfoGizmo;

        //
        protected NetworkCellIO cellIO;
        protected NetworkContainer container;
        protected NetworkPartSet directPartSet;
        protected AdjacentNodePartSet adjacencySet;

        private NetworkDef internalDef;
        
        //
        private int lastReceivedTick;
        private int receivingTicks;

        //Settings
        private Dictionary<NetworkValueDef, (bool, float)> requestedTypes;
        private float requestedCapacityPercent = 0.5f;
        private RequesterMode requesterMode = RequesterMode.Automatic;

        //
        private bool drawNetworkInfo = false;

        //DEBUG
        protected bool DebugNetworkCells = false;

        //
        public NetworkSubPartProperties Props { get; private set; }
        public Thing Thing => Parent.Thing;
        
        public NetworkDef NetworkDef => internalDef;
        public NetworkRole NetworkRole => Props.NetworkRole;
        public PipeNetwork Network { get; set; }
        public INetworkStructure Parent { get; private set; }
        public INetworkSubPart NetworkPart => this;

        //States
        public bool IsMainController => Network?.NetworkController == Parent;
        public bool IsNetworkNode => NetworkRole != NetworkRole.Transmitter || IsJunction;
        public bool IsNetworkEdge => !IsNetworkNode;
        public bool IsJunction => DirectPartSet[NetworkRole.Transmitter]?.Count > 2;
        public bool IsActive => Network.IsWorking;

        public bool IsReceiving => receivingTicks > 0;

        public bool HasContainer => Props.containerProps != null;
        public bool HasConnection => DirectPartSet[NetworkRole.Transmitter]?.Count > 0;
        public bool HasLeak => false;

        //Sub Role Handling
        public Dictionary<NetworkRole, List<NetworkValueDef>> ValuesByRole => Props.AllowedValuesByRole;

        //
        public NetworkPartSet DirectPartSet => directPartSet;
        public AdjacentNodePartSet AdjacencySet => adjacencySet;
        public NetworkCellIO CellIO
        {
            get
            {
                if (cellIO != null) return cellIO;
                if (Props.networkIOPattern == null) return Parent.GeneralIO;
                return cellIO ??= new NetworkCellIO(Props.networkIOPattern, Parent.Thing);
            }
        }

        //
        public Gizmo_NetworkInfo NetworkGizmo => networkInfoGizmo ??= new Gizmo_NetworkInfo(this);

        //Container
        public string ContainerTitle => "_Obsolete_";
        public ContainerProperties ContainerProps => Props.containerProps;
        public NetworkContainerSet ContainerSet => null; //Network.ContainerSet;

        public NetworkContainer Container
        {
            get => container;
            private set => container = value;
        }

        //Requester
        public Dictionary<NetworkValueDef, (bool, float)> RequestedTypes => requestedTypes;

        public float RequestedCapacityPercent
        {
            get => requestedCapacityPercent;
            set => requestedCapacityPercent = Mathf.Clamp01(value);
        }

        public RequesterMode RequesterMode
        {
            get => requesterMode;
            set => requesterMode = value;
        }

        public NetworkSubPart(){}

        public NetworkSubPart(INetworkStructure parent)
        {
            Parent = parent;
        }
        
        public NetworkSubPart(INetworkStructure parent, NetworkSubPartProperties properties)
        {
            Parent = parent;
            Props = properties;
            internalDef = properties.networkDef;
        }

        public virtual void ExposeData()
        {
            Scribe_Defs.Look(ref internalDef, "internalDef");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Props = ((Comp_NetworkStructure) Parent).Props.networks.Find(p => p.networkDef == internalDef);
            }
            Scribe_Values.Look(ref requestedCapacityPercent, "requesterPercent");
            Scribe_Deep.Look(ref container, "container", this, Props.AllowedValues);
        }

        public void SubPartSetup(bool respawningAfterLoad)
        {
            //Generate components
            directPartSet = new NetworkPartSet(NetworkDef, this);
            adjacencySet = new AdjacentNodePartSet(this);

            RolePropertySetup();
            GetDirectlyAjdacentNetworkParts();
            if (respawningAfterLoad) return; // IGNORING EXPOSED CONSTRUCTORS
            if (HasContainer)
                Container = new NetworkContainer(this, Props.AllowedValues);
        }

        private void GetDirectlyAjdacentNetworkParts()
        {
            for (var c = 0; c < CellIO.ConnectionCells.Length; c++)
            {
                var connectionCell = CellIO.ConnectionCells[c];
                List<Thing> thingList = connectionCell.GetThingList(Thing.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (PipeNetworkMaker.Fits(thingList[i], NetworkDef, out var subPart))
                    {
                        directPartSet.AddNewComponent(subPart);
                        subPart.DirectPartSet.AddNewComponent(this);
                    }
                }
            }
        }

        public void PostDestroy(DestroyMode mode, Map previousMap)
        {
            AdjacencySet.Notify_ParentDestroyed();
            DirectPartSet.Notify_ParentDestroyed();
            Container?.Notify_ParentDestroyed(mode, previousMap);
            //Network.Notify_RemovePart(this);
        }

        private void RolePropertySetup()
        {
            if (NetworkRole.HasFlag(NetworkRole.Requester))
            {
                requestedTypes = new Dictionary<NetworkValueDef, (bool, float)>();
                foreach (var allowedValue in Props.AllowedValuesByRole[NetworkRole.Requester])
                {
                    requestedTypes.Add(allowedValue, (true, 0));
                }
            }
        }

        //
        public void NetworkTick()
        {
            var isPowered = Parent.IsPowered;
            if (isPowered)
            {
                if (receivingTicks > 0 && lastReceivedTick < Find.TickManager.TicksGame)
                    receivingTicks--;

                if (!IsActive) return;
                ProcessValues();
                Parent.NetworkPartProcessor(this);
            }

            Parent.NetworkPostTick(isPowered);
        }

        //Network 
        //Process current stored values according to rules of the network role
        private void ProcessValues()
        {
            //Producers push to Storages
            if (NetworkRole.HasFlag(NetworkRole.Producer))
            {
                ProducerTick();
            }

            //Storages push to Consumers
            if (NetworkRole.HasFlag(NetworkRole.Storage))
            {
                StorageTick();
            }

            //Consumers slowly use up own container
            if (NetworkRole.HasFlag(NetworkRole.Consumer))
            {
                ConsumerTick();
            }

            if (NetworkRole.HasFlag(NetworkRole.Requester))
            {
                RequesterTick();
            }
        }

        protected virtual void ProducerTick()
        {
            TransferToOthers(NetworkRole.Producer, NetworkRole.Storage);
        }

        protected virtual void StorageTick()
        {
            if (Container.ContainsForbiddenType)
            {
                ClearForbiddenTypes();
            }
            if (ContainerProps.storeEvenly)
            {
                TransferToOthers(NetworkRole.Storage, NetworkRole.Storage);
                return;
            }
            TransferToOthers(NetworkRole.Storage, NetworkRole.Consumer);
        }

        protected virtual void ConsumerTick()
        {
        }

        protected virtual void RequesterTick()
        {
            if (RequesterMode == RequesterMode.Automatic)
            {
                //Resolve..
                var maxVal = RequestedCapacityPercent * Container.Capacity;
                foreach (var valType in Props.AllowedValuesByRole[NetworkRole.Requester])
                {
                    var valTypeValue = Container.ValueForType(valType) + Network.TotalValueFor(valType, NetworkRole.Storage);
                    if (valTypeValue > 0)
                    {
                        var setValue = Mathf.Min(maxVal, valTypeValue);
                        var tempVal = requestedTypes[valType];
                        tempVal.Item2 = setValue;
                        RequestedTypes[valType] = tempVal;
                        maxVal = Mathf.Clamp(maxVal - setValue, 0, maxVal);
                    }
                }
            }

            TLog.Debug($"Doing requester tick for {this}: {Container.StoredPercent} > {RequestedCapacityPercent}");
            if (Container.StoredPercent >= RequestedCapacityPercent) return;
            foreach (var requestedType in RequestedTypes)
            {
                //If not requested, skip
                if (!requestedType.Value.Item1) continue;
                TLog.Debug($"Requesting: {requestedType.Value.Item1}: {requestedType.Value.Item2}");
                TLog.Debug($"--: {Container.PercentOf(requestedType.Key)} < {requestedType.Value.Item2} ");
                if (Container.PercentOf(requestedType.Key) < requestedType.Value.Item2)
                {
                    TLog.Debug("Executing actual request... ");
                    DoNetworkAction(this, NetworkRole.Storage, part =>
                    {
                        var container = part.Container;
                        if (container.Empty) return;
                        if (container.ValueForType(requestedType.Key) <= 0) return;
                        if (container.TryTransferTo(Container, requestedType.Key, 1))
                        {
                            Notify_ReceivedValue();
                        }
                    }, (_) => true);
                }
            }

            /*
            if (RequesterMode == RequesterMode.Automatic)
            {
                if (Network.TryGetNodePath(this, NetworkRole.Storage))
                {

                }

                //Resolve..
                var maxVal = RequestedCapacityPercent * Container.Capacity;
                foreach (var valType in Props.AllowedValuesByRole[NetworkRole.Requester])
                {
                    var valTypeValue = Container.ValueForType(valType) + Network.TotalValueFor(valType, NetworkRole.Storage);
                    if (valTypeValue > 0)
                    {
                        var setValue = Mathf.Min(maxVal, valTypeValue);
                        var tempVal = requestedTypes[valType];
                        tempVal.Item2 = setValue;
                        RequestedTypes[valType] = tempVal;
                        maxVal = Mathf.Clamp(maxVal - setValue, 0, maxVal);
                    }
                }
            }


            if (Container.StoredPercent >= RequestedCapacityPercent) return;
            foreach (var requestedType in RequestedTypes)
            {
                //If not requested, skip
                if (!requestedType.Value.Item1) continue;
                if (Container.ValueForType(requestedType.Key) < requestedType.Value.Item2)
                {
                    foreach (var component in Network.PartSet[NetworkRole.Storage])
                    {
                        var container = component.Container;
                        if (container.Empty) continue;
                        if (container.ValueForType(requestedType.Key) <= 0) continue;
                        if (container.TryTransferTo(Container, requestedType.Key, 1))
                        {
                            Notify_ReceivedValue();
                        }
                    }
                }
            }
            */
        }

        private void DoNetworkAction(INetworkSubPart fromPart, NetworkRole forRole, Action<INetworkSubPart> funcOnPart, Predicate<INetworkSubPart> validator)
        {
            var requestResult = Network.Graph.ProcessRequest(new NetworkGraphPathRequest(fromPart, forRole, validator, 1));
            if (!requestResult.IsValid) return;
            foreach (var targetPart in requestResult.allTargets)
            {
                funcOnPart.Invoke(targetPart);
                FleckMaker.ThrowDustPuff(targetPart.Parent.Thing.TrueCenter(), Find.CurrentMap, 1);
                TLog.Message($"Trying to manipulate {targetPart} from {fromPart}");
            }
            
            /*
            foreach (var subPart in Network.PartSet[forRole])
            {
                if (validator(subPart))
                {
                    var result = Network.Graph.GetPart(new NetworkGraphNodeRequest(fromPart, subPart));
                    if (result == subPart)
                    {
                        funcOnPart.Invoke(subPart);
                    }
                }
            }
            */
        }

        private void DoNetworkAction(INetworkSubPart fromPart, INetworkSubPart previous, NetworkRole ofRole, Action<INetworkSubPart> funcOnPart, Predicate<INetworkSubPart> validator)
        {
            foreach (var subPart in fromPart.Network.Graph.AdjacencyLists[this])
            {
                if (subPart == previous) continue;

                if (subPart.IsJunction)
                {
                    DoNetworkAction(subPart, fromPart, ofRole, funcOnPart, validator);
                    continue;
                }
                
                if (!subPart.NetworkRole.HasFlag(ofRole)) continue;
                if(!validator(subPart)) continue;
                funcOnPart(subPart);
            }
        }

        //
        private void SubTransfer(INetworkSubPart previousPart, INetworkSubPart part, List<NetworkValueDef> usedTypes, NetworkRole ofRole)
        {
            foreach (var subPart in part.Network.Graph.AdjacencyLists[this])
            {
                if(subPart == previousPart) continue;
                
                if (subPart.IsJunction)
                {
                    SubTransfer(part, subPart, usedTypes, ofRole);
                    continue;
                }

                if (!subPart.NetworkRole.HasFlag(ofRole)) continue;
                if (Container.Empty || subPart.Container.Full) continue;
                for (int i = usedTypes.Count - 1; i >= 0; i--)
                {
                    var type = usedTypes[i];
                    if (!subPart.NeedsValue(type, ofRole)) continue;
                    if (Container.TryTransferTo(subPart.Container, type, 1))
                    {
                        subPart.Notify_ReceivedValue();
                    }
                }
            }
        }
        
        private void Processor_Transfer(INetworkSubPart part, NetworkRole fromRole, NetworkRole ofRole)
        {
            if (part == null)
            {
                TLog.Warning("Target part of path is null");
                return;
            }
            
            var usedTypes = Props.AllowedValuesByRole[fromRole];
            for (int i = usedTypes.Count - 1; i >= 0; i--)
            {
                var type = usedTypes[i];
                if (!part.NeedsValue(type, ofRole)) continue;
                if (Container.TryTransferTo(part.Container, type, 1))
                {
                    part.Notify_ReceivedValue();
                }
            }
        }
        
        private void TransferToOthers(NetworkRole fromRole, NetworkRole ofRole)
        {
            if (Container.Empty) return;
            DoNetworkAction(this, ofRole, (part) =>
            {
                if (part == null)
                {
                    TLog.Warning("Target part of path is null");
                    return;
                }
            
                var usedTypes = Props.AllowedValuesByRole[fromRole];
                for (int i = usedTypes.Count - 1; i >= 0; i--)
                {
                    var type = usedTypes[i];
                    if (!part.NeedsValue(type, ofRole)) continue;
                    if (Container.TryTransferTo(part.Container, type, 1))
                    {
                        part.Notify_ReceivedValue();
                    }
                }
            }, (part) => part.HasContainer && !part.Container.Full && !Container.Empty);

            //var path = Network.Graph.GetRequestPath(new NetworkGraphNodeRequest(this, ofRole));
            //SubTransfer(null, this, usedTypes, ofRole);

            /*
            foreach (var component in Network.PartSet[ofRole])
            {
                if (Container.Empty || component.Container.Full) continue;
                if (evenly && component.Container.StoredPercent > Container.StoredPercent) continue;

                for (int i = usedTypes.Count - 1; i >= 0; i--)
                {
                    var type = usedTypes[i];
                    if (!component.NeedsValue(type, ofRole)) continue;
                    if (Container.TryTransferTo(component.Container, type, 1))
                    {
                        component.Notify_ReceivedValue();
                    }
                }
            }
            */
        }

        private void ClearForbiddenTypes()
        {
            if (Container.Empty) return;
            foreach (var component in Network.PartSet[NetworkRole.Storage])
            {
                if (component.Container.Full) continue;
                for (int i = Container.AllStoredTypes.Count - 1; i >= 0; i--)
                {
                    var type = Container.AllStoredTypes.ElementAt(i);
                    if (Container.AcceptsType(type)) continue;
                    if (!component.NeedsValue(type, NetworkRole.Storage)) continue;
                    if (Container.TryTransferTo(component.Container, type, 1))
                    {
                        component.Notify_ReceivedValue();
                    }
                }
            }
        }

        //Data Notifiers
        public virtual void Notify_ContainerFull()
        {
        }

        public virtual void Notify_ContainerStateChanged()
        {
        }

        public void Notify_ReceivedValue()
        {
            lastReceivedTick = Find.TickManager.TicksGame;
            receivingTicks++;
            Parent.Notify_ReceivedValue();
        }

        public void Notify_SetConnection(INetworkSubPart otherPart, NetEdge withEdge)
        {
            AdjacencySet.Notify_SetEdge(otherPart, withEdge);
        }

        public void Notify_NetworkDestroyed()
        {
            AdjacencySet.Notify_Clear();
        }

        //
        public void SendFirstValue(INetworkSubPart other)
        {
            Container.TryTransferTo(other.Container, Container.AllStoredTypes.FirstOrDefault(), 1);
        }

        //
        public bool ConnectsTo(INetworkSubPart other)
        {
            if (other == this) return false;
            return NetworkDef == other.NetworkDef && Parent.CanConnectToOther(other.Parent);
        }

        /*
        private bool CompatibleWith(INetworkSubPart other)
        {
            if (other.Network == null)
            {
                TLog.Error($"{other.Parent.Thing} is not part of any Network - this should not be the case.");
                return false;
            }
            return other.Network.NetworkRank == Network.NetworkRank;
        }
        */
        public bool CanTransmit()
        {
            //TODO:
            //NetworkRole.GraphTransmitter;
            return NetworkRole.HasFlag(NetworkRole.Transmitter);
        }
        
        public bool NeedsValue(NetworkValueDef value, NetworkRole forRole)
        {
            if (Props.AllowedValuesByRole.TryGetValue(forRole, out var values) && values.Contains(value))
            {
                return Parent.AcceptsValue(value);
            }
            return false;
        }

        //Gizmos
        public virtual IEnumerable<Gizmo> GetPartGizmos()
        {
            yield return NetworkGizmo;

            /*
            if (HasContainer)
            {
                foreach (var containerGizmo in Container.GetGizmos())
                {
                    yield return containerGizmo;
                }
            }
            */

            /*
            foreach (var networkGizmo in GetSpecialNetworkGizmos())
            {
                yield return networkGizmo;
            }
            */

            if (DebugSettings.godMode)
            {
                if (IsMainController)
                {
                    yield return new Command_Action()
                    {
                        defaultLabel = "Show Entire Network",
                        action = delegate
                        {
                            DebugNetworkCells = !DebugNetworkCells;
                        }
                    };
                }

                yield return new Command_Action
                {
                    defaultLabel = $"View Adjacent Structures",
                    defaultDesc = DirectPartSet.ToString(),
                    action = delegate { }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"View {NetworkDef.defName} Set",
                    defaultDesc = AdjacencySet.ToString(),
                    action = delegate { }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"View Entire {NetworkDef.defName} Set",
                    defaultDesc = Network?.PartSet?.ToString() ?? "N/A",
                    action = delegate { drawNetworkInfo = !drawNetworkInfo; }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"Draw Graph",
                    action = delegate { Network.DrawInternalGraph = !Network.DrawInternalGraph; }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"Draw AdjacencyList",
                    action = delegate { DebugDrawGraphAdjacency = !DebugDrawGraphAdjacency; }
                };
            }
        }

        //Readouts and UI
        public virtual string NetworkInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[{NetworkDef}]ID: {Network?.ID}");
            sb.AppendLine($"Has Network: {Network != null}");
            if (IsNetworkNode)
            {
                sb.AppendLine("Is Node");
            }
            if (IsNetworkEdge)
            {
                sb.AppendLine("Is Edge");
            }
            if (IsJunction)
            {
                sb.AppendLine("Is Junction");
            }

            sb.AppendLine($"[Transmitters] {DirectPartSet[NetworkRole.Transmitter]?.Count}");
            return sb.ToString();
        }

        private static bool DebugDrawGraphAdjacency = false;

        public void Draw()
        {
            DrawNetworkInfo();
            if (DebugNetworkCells)
            {
                GenDraw.DrawFieldEdges(Network.NetworkCells, Color.cyan);
            }

            if (Find.Selector.IsSelected(Parent.Thing))
            {
                if (DebugDrawGraphAdjacency)
                {
                    GenDraw.DrawFieldEdges(Network.Graph.AdjacencyLists[this].Select(c => c.Parent.Thing.Position).ToList(), Color.red);
                }
                CellIO.DrawIO();
            }
        }

        private void DrawNetworkInfo()
        {
            /*
            if (!drawNetworkInfo) return;
            Rect sizeRect = new Rect(UI.screenWidth / 2 - (756/2),UI.screenHeight/2 - (756/2), 756, 756);
            Find.WindowStack.ImmediateWindow(GetHashCode(), sizeRect, WindowLayer.GameUI, () =>
            {
                int row = 0;
                float curY = 0;

                foreach (var container in Network.ContainerSet[NetworkRole.All])
                {
                    Widgets.Label(new Rect(0, curY, 150, 20), $"{keyValue.Key}: ");
                    int column = 0;
                    curY += 20;
                    foreach (var container in keyValue.Value)
                    {
                        Rect compRect = new Rect(column * 100 + 5, curY, 100, 100);
                        Widgets.DrawBox(compRect);
                        string text = $"{container.Parent.Thing.def}:\n";

                        TWidgets.DrawTiberiumReadout(compRect, container);
                        column++;
                    }
                    row++;
                    curY += 100 + 5;
                }
                
                foreach (var structures in Network.PartSet.StructuresByRole)
                {
                    Widgets.Label(new Rect(0, curY, 150, 20), $"{structures.Key}: ");
                    int column = 0;
                    curY += 20;
                    foreach (var component in structures.Value)
                    {
                        Rect compRect = new Rect(column * 100 + 5, curY, 100, 100);
                        Widgets.DrawBox(compRect);
                        string text = $"{component.Parent.Thing.def}:\n";
                        switch (structures.Key)
                        {
                            case NetworkRole.Producer:
                                text = $"{text}Producing:";
                                break;
                            case NetworkRole.Storage:
                                text = $"{text}";
                                break;
                            case NetworkRole.Consumer:
                                text = $"{text}";
                                break;
                            case NetworkRole.Requester:
                                text = $"{text}";
                                break;
                        }
                        Widgets.Label(compRect, $"{text}");
                        column++;
                    }
                    row++;
                    curY += 100 + 5;
                }
                
            } );
        */
        }

        //
        public override string ToString()
        {
            return Parent.Thing.ToString();
        }
    }
}
