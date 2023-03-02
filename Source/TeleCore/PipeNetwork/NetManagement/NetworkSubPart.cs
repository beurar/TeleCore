using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using TeleCore.Static.Utilities;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class NetworkSubPart : IExposable, INetworkSubPart, INetworkRequester, IContainerHolderNetwork
    {
        //
        private Gizmo_NetworkInfo networkInfoGizmo;

        //
        protected NetworkCellIO cellIO;
        protected NetworkContainer container;
        protected NetworkPartSet directPartSet;

        //Role Workers
        private NetworkRequestWorker _requesterInt;
        
        private NetworkDef internalDef;
        private int lastReceivedTick;
        private int receivingTicks;
        private bool drawNetworkInfo = false;

        //

        //DEBUG
        protected bool DebugNetworkCells = false;

        //
        public NetworkSubPartProperties Props { get; private set; }
        public Thing Thing => Parent.Thing;
        public bool ShowStorageGizmo { get; }
        public bool ShowStorageForThingGizmo => false;

        public NetworkDef NetworkDef => internalDef;
        public NetworkRole NetworkRole => Props.NetworkRole;
        public PipeNetwork Network { get; set; }
        public INetworkSubPart Part { get; set; }
        public INetworkStructure Parent { get; private set; }
        public INetworkSubPart NetworkPart => this;

        //States
        public bool IsMainController => Network?.NetworkController == Parent;
        public bool IsNetworkNode => NetworkRole != NetworkRole.Transmitter;// || IsJunction; //|| IsPipeEndPoint;
        public bool IsNetworkEdge => !IsNetworkNode;
        public bool IsJunction => NetworkRole == NetworkRole.Transmitter && DirectPartSet[NetworkRole.Transmitter]?.Count > 2;
        public bool IsPipeEndPoint => NetworkRole == NetworkRole.Transmitter && DirectPartSet[NetworkRole.Transmitter]?.Count == 1;
        public bool IsReceiving => receivingTicks > 0;
        public bool NetworkActive => Network.IsWorking || !Props.requiresController;
        
        //
        public bool HasContainer => Props.containerConfig != null;
        public bool HasConnection => DirectPartSet[NetworkRole.Transmitter]?.Count > 0;
        public bool HasLeak => false;

        //Sub Role Handling
        public Dictionary<NetworkRole, List<NetworkValueDef>> ValuesByRole => Props.AllowedValuesByRole;

        //
        public NetworkPartSet DirectPartSet => directPartSet;

        public NetworkCellIO CellIO
        {
            get
            {
                if (cellIO != null) return cellIO;
                if (Props.subIOPattern == null) return Parent.GeneralIO;
                return cellIO ??= new NetworkCellIO(Props.subIOPattern, Parent.Thing);
            }
        }

        //
        public Gizmo_NetworkInfo NetworkGizmo => networkInfoGizmo ??= new Gizmo_NetworkInfo(this);

        //Container
        public string ContainerTitle => "_Obsolete_";
        public NetworkContainerSet ContainerSet => Network.ContainerSet;

        //BaseContainer<NetworkValueDef, IContainerHolder<NetworkValueDef>> IContainerHolder<NetworkValueDef>.Container => Container;
        
        public NetworkContainer Container
        {
            get => container;
            private set => container = value;
        }

        //Role Workers
        public NetworkRequestWorker Requester => _requesterInt;

        #region Constructors

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

        #endregion

        public virtual void ExposeData()
        {
            Scribe_Defs.Look(ref internalDef, "internalDef");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                //TODO: Clean this up, shouldnt cast to comp
                Props = ( (Comp_NetworkStructure)Parent).Props.networks.Find(p => p.networkDef == internalDef);
            }

            Scribe_Deep.Look(ref _requesterInt, nameof(_requesterInt));
            
            if (Props.containerConfig != null)
            {
                //Scribe_Values.Look(ref requestedCapacityPercent, "requesterPercent");
                //Scribe_Values.Look(ref requestedCpacityRange, "requestedCpacityRange");
                Scribe_Deep.Look(ref container, "container", this, Props.AllowedValues);
            }
        }

        public void SubPartSetup(bool respawningAfterLoad)
        {
            //Generate components
            directPartSet = new NetworkPartSet(NetworkDef, this);

            RolePropertySetup(respawningAfterLoad);
            GetDirectlyAjdacentNetworkParts();
            
            if (respawningAfterLoad) return; // IGNORING EXPOSED CONSTRUCTORS
            if (HasContainer)
                Container = new NetworkContainer(Props.containerConfig, this);
        }

        private void GetDirectlyAjdacentNetworkParts()
        {
            for (var c = 0; c < CellIO.OuterConnnectionCells.Length; c++)
            {
                var connectionCell = CellIO.OuterConnnectionCells[c];
                List<Thing> thingList = connectionCell.GetThingList(Thing.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (PipeNetworkMaker.Fits(thingList[i], NetworkDef, out var subPart))
                    {
                        if (ConnectsTo(subPart, out _, out _))
                        {
                            directPartSet.AddNewComponent(subPart);
                            subPart.DirectPartSet.AddNewComponent(this);
                        }
                    }
                }
            }
        }

        public void PostDestroy(DestroyMode mode, Map previousMap)
        {
            DirectPartSet.Notify_ParentDestroyed();
            Container?.Notify_ParentDestroyed(mode, previousMap);
            //Network.Notify_RemovePart(this);
        }

        private void RolePropertySetup(bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;
            
            if (NetworkRole.HasFlag(NetworkRole.Requester))
            {
                _requesterInt = new NetworkRequestWorker(this);
            }
        }

        //
        public void NetworkTick()
        {
            var parent = Parent;
            var isPowered = parent.IsPowered;
            if (isPowered)
            {
                if (receivingTicks > 0 && lastReceivedTick < Find.TickManager.TicksGame)
                    receivingTicks--;

                if (!NetworkActive) return;
                ProcessValues();
                parent.NetworkPartProcessorTick(this);
            }
            parent.NetworkPostTick(this, isPowered);
        }

        //Network 
        //Process current stored values according to rules of the network role
        private void ProcessValues()
        {
            if (NetworkRole.HasFlag(NetworkRole.Passthrough))
            {
                PassthroughTick();
            }
            
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

            //
            if (NetworkRole.HasFlag(NetworkRole.Requester))
            {
                RequesterTick();
            }
        }

        protected virtual void PassthroughTick()
        {
            NetworkTransactionUtility.DoTransaction(new TransactionRequest(this,
                NetworkRole.Producer, NetworkRole.Storage,
                part => NetworkTransactionUtility.Actions.TransferToOther_Equalize(this, part),
                part => NetworkTransactionUtility.Validators.PartValidator_Sender(this, part)));
        }
        
        protected virtual void ProducerTick()
        {
            NetworkTransactionUtility.DoTransaction(new TransactionRequest(this,
                NetworkRole.Producer, NetworkRole.Storage,
                part => NetworkTransactionUtility.Actions.TransferToOther_AnyDesired(this, part),
                part => NetworkTransactionUtility.Validators.PartValidator_Sender(this, part)));
        }

        protected virtual void StorageTick()
        {
            if (Container.ContainsForbiddenType)
            {
                ClearForbiddenTypesSubTick();
            }
            
            if (Container.Config.storeEvenly && Network.HasGraph)
            {
                NetworkTransactionUtility.DoTransaction(new TransactionRequest(this,
                    NetworkRole.Storage, NetworkRole.Storage,
                    part => NetworkTransactionUtility.Actions.TransferToOther_Equalize(this, part),
                    part => NetworkTransactionUtility.Validators.StoreEvenly_EQ_Check(this, part)));
                return;
            }
            
            //
            NetworkTransactionUtility.DoTransaction(new TransactionRequest(this,
                NetworkRole.Storage, NetworkRole.Consumer,
                part => NetworkTransactionUtility.Actions.TransferToOther_AnyDesired(this, part),
                part => NetworkTransactionUtility.Validators.PartValidator_Sender(this, part)));
        }

        protected virtual void ConsumerTick()
        {
        }

        /// <summary>
        /// When using the Requester NetworkRole, will pull values in according to <see cref="RequestedCpacityRange"/>.
        /// </summary>
        protected virtual void RequesterTick()
        {
            

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

        private void ClearForbiddenTypesSubTick()
        {
            if (Container.FillState == ContainerFillState.Empty) return;
            NetworkTransactionUtility.DoTransaction(new TransactionRequest(this,
                NetworkRole.Storage, NetworkRole.Consumer,
                part => NetworkTransactionUtility.Actions.TransferToOtherPurge(this, part),
                part => NetworkTransactionUtility.Validators.PartValidator_Sender(this, part,
                    ePart => FlowValueUtils.CanExchangeForbidden(Container, ePart.Container))));
            // Container.AllStoredTypes.Any(v => !ePart.Container.GetFilterFor(v).canStore)
            // Container.FilterSettings.Any(pair => !pair.Value.canStore && ePart.Container.CanHoldValue(pair.Key)))));
        }
        
        private void DoNetworkAction(INetworkSubPart fromPart, INetworkSubPart previous, NetworkRole ofRole, Action<INetworkSubPart> funcOnPart, Predicate<INetworkSubPart> validator)
        {
            var adjacencyList = fromPart.Network.Graph.GetAdjacencyList(this);
            if (adjacencyList == null) return;
            
            foreach (var subPart in adjacencyList)
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
            var adjacencyList = part.Network.Graph.GetAdjacencyList(this);
            if (adjacencyList == null) return;
            
            foreach (var subPart in adjacencyList)
            {
                if(subPart == previousPart) continue;
                
                if (subPart.IsJunction)
                {
                    SubTransfer(part, subPart, usedTypes, ofRole);
                    continue;
                }

                if (!subPart.NetworkRole.HasFlag(ofRole)) continue;
                if (Container.FillState == ContainerFillState.Empty || subPart.Container.FillState == ContainerFillState.Full) continue;
                for (int i = usedTypes.Count - 1; i >= 0; i--)
                {
                    var type = usedTypes[i];
                    if (!subPart.NeedsValue(type, ofRole)) continue;
                    if (Container.TryTransferTo(subPart.Container, type, 1, out _))
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
                if (Container.TryTransferTo(part.Container, type, 1, out _))
                {
                    part.Notify_ReceivedValue();
                }
            }
        }

        //Data Notifiers
        public virtual void Notify_ContainerStateChanged(NotifyContainerChangedArgs<NetworkValueDef> args)
        {
            
        }

        public void Notify_ReceivedValue()
        {
            lastReceivedTick = Find.TickManager.TicksGame;
            receivingTicks++;
            Parent.Notify_ReceivedValue();
        }

        public void Notify_StateChanged(string signal)
        {
            if (signal is "FlickedOn" or "FlickedOff")
            {
                //...
            }
        }

        public void Notify_SetConnection(NetEdge edge, IntVec3Rot ioCell)
        {
        }

        public void Notify_NetworkDestroyed()
        {
        }

        //
        public void SendFirstValue(INetworkSubPart other)
        {
            Container.TryTransferTo(other.Container, Container.AllStoredTypes.FirstOrDefault(), 1, out _);
        }

        //
        public bool CanInteractWith(INetworkSubPart other)
        {
            return Parent.CanInteractWith(this, other);
        }
        
        public bool ConnectsTo(INetworkSubPart other)
        {
            return ConnectsTo(other, out _, out _);
        }
        
        public bool ConnectsTo(INetworkSubPart other, out IntVec3 intersectingCell, out NetworkIOMode IOMode)
        {
            intersectingCell = IntVec3.Invalid;
            IOMode = NetworkIOMode.None;
            if (other == this) return false;
            if (!NetworkDef.Equals(other.NetworkDef)) return false;
            if (!Parent.CanConnectToOther(other.Parent)) return false;
            return CellIO.ConnectsTo(other.CellIO, out intersectingCell, out IOMode);
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
        
        public bool CanTransmit(NetEdge netEdge)
        {
            return NetworkRole.HasFlag(NetworkRole.Transmitter);
        }

        public bool AcceptsValue(NetworkValueDef value)
        {
            if (HasContainer && Parent.AcceptsValue(value))
            {
                if (Container.CanReceiveValue(value))
                {
                    return true;
                }
            }
            return false;
        }
        
        public bool NeedsValue(NetworkValueDef value, NetworkRole forRole)
        {
            if (Props.AllowedValuesByRole.TryGetValue(forRole, out var values) && values.Contains(value))
            {
                return AcceptsValue(value);
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

                if (Network == null) yield break;

                yield return new Command_Action
                {
                    defaultLabel = $"View Adjacency List",
                    defaultDesc = Network.Graph.GetAdjacencyList(this)?.ToStringSafeEnumerable(),
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
                    defaultLabel = $"Draw Results",
                    action = delegate { Network.DrawGraphCachedResults = !Network.DrawGraphCachedResults; }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"Draw AdjacencyList",
                    action = delegate { Network.DrawAdjacencyList = !Network.DrawAdjacencyList; }
                };
                
                
                yield return new Command_Action
                {
                    defaultLabel = $"Draw FlowDirections",
                    action = delegate { Debug_DrawFlowDir = !Debug_DrawFlowDir; }
                };
            }
        }

        //Readouts and UI
        public virtual string NetworkInspectString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[{NetworkDef}]ID: {Network?.ID}");
            sb.AppendLine($"Has Network: {Network != null}");
            sb.AppendLine($"Network Valid: {Network?.IsValid}");
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
            return sb.ToString().TrimEndNewlines();
        }
        
        internal static bool Debug_DrawFlowDir = false;

        private Rot4 FlowDir
        {
            get;
            set;
        } = Rot4.Invalid;
        
        internal void SetFlowDir(INetworkSubPart other)
        {
            /*
            if (Network.Graph.TryGetEdge(this, other, out var edge))
            {
                FlowDir = edge.fromToDir;
            }
            */
        }
        
        public void Draw()
        {
            DrawNetworkInfo();
            if (DebugNetworkCells)
            {
                GenDraw.DrawFieldEdges(Network.NetworkCells, Color.cyan);
            }

            //Render Flow Debug
            if (Debug_DrawFlowDir && FlowDir != Rot4.Invalid)
            {
                var matrix = new Matrix4x4();
                matrix.SetTRS(Parent.Thing.DrawPos, 0f.ToQuat(), new Vector3(1, AltitudeLayer.MetaOverlays.AltitudeFor(), 1));
                Graphics.DrawMesh(MeshPool.plane10, matrix, TeleContent.IOArrowRed, 0);
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
