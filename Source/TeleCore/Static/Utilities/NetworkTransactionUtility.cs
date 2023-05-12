using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Mono.Unix.Native;
using RimWorld;
using TeleCore.FlowCore;
using UnityEngine;
using Verse;

namespace TeleCore.Static.Utilities;

public static class NetworkTransactionUtility
{
    internal static class Actions
    {
        internal static void TransferToOther_Equalize(INetworkSubPart sender, INetworkSubPart receiver)
        {
            if (receiver == null)
            {
                TLog.Warning("Transaction receiver is null.");
                return;
            }

            if (Validators.StoreEvenly_EQ_Check(sender, receiver))
            {
                FlowValueUtils.TryEqualizeAll(sender.Container, receiver.Container);
                //ContainerTransferUtility.TryEqualizeAll(sender.Container, receiver.Container);
            }
        }
        
        internal static void TransferToOther_AnyDesired(INetworkSubPart sender, INetworkSubPart receiver)
        {
            if (receiver == null)
            {
                TLog.Warning("Transaction receiver is null.");
                return;
            }

            //TODO: Make sure to use correct filter settings: Container.Filter
            var usedTypes = sender.Container.AcceptedTypes;
            for (int i = usedTypes.Count - 1; i >= 0; i--)
            {
                var type = usedTypes[i];
                if (!receiver.Parent.AcceptsValue(type)) continue;
                if (sender.Container.TryTransferValue(receiver.Container, type, 1, out var val))
                {
                    receiver.Notify_ReceivedValue();
                    //MoteMaker.ThrowText(receiver.Parent.Thing.DrawPos, sender.Parent.Thing.Map, $"{val}", Color.green);
                }
            }
        }

        internal static void TransferToOtherSpecific(INetworkSubPart sender, INetworkSubPart receiver, NetworkValueDef def)
        {
            if (!receiver.Parent.AcceptsValue(def)) return;
            if (sender.Container.TryTransferValue(receiver.Container, def, 1, out var val))
            {
                receiver.Notify_ReceivedValue();
                //MoteMaker.ThrowText(receiver.Parent.Thing.DrawPos, sender.Parent.Thing.Map, $"{val}", Color.green);
            }
        }
        
        internal static void TransferToOtherPurge(INetworkSubPart sender, INetworkSubPart? receiver)
        {
            if (receiver == null)
            {
                TLog.Warning("Transaction receiver is null.");
                return;
            }

            var temp = StaticListHolder<NetworkValueDef>.RequestSet("TransferPurgeSet");
            temp.AddRange(sender.Container.StoredDefs);
            foreach (var type in temp)
            {
                if(!sender.Container.GetFilterFor(type).canStore)
                    TransferToOtherSpecific(sender, receiver, type);
            }
            temp.Clear();
        }
        
        internal static void TransferToOther_FullFiltered(INetworkSubPart sender, INetworkSubPart? receiver, NetworkRole fromRole, NetworkRole ofRole)
        {
            if (receiver == null)
            {
                TLog.Warning("Transaction receiver is null.");
                return;
            }

            var usedTypes = sender.Props.AllowedValuesByRole[fromRole];
            for (int i = usedTypes.Count - 1; i >= 0; i--)
            {
                var type = usedTypes[i];
                if (!receiver.NeedsValue(type, ofRole)) continue;
                if (sender.Container.TryTransferValue(receiver.Container, type, 1, out var val))
                {
                    receiver.Notify_ReceivedValue();
                    //MoteMaker.ThrowText(receiver.Parent.Thing.DrawPos, sender.Parent.Thing.Map, $"{val}", Color.green);
                }
            }
        }
    }
    
    internal static class Validators
    {
        /// <summary>
        /// Determines whether there should be a equalization between two network parts.
        /// </summary>
        internal static bool StoreEvenly_EQ_Check(INetworkSubPart sender, INetworkSubPart receiver)
        {
            if (!sender.HasContainer || !receiver.HasContainer) return false;
            
            //Only send - we cannot request without special cases (checking path for directionality)
            return sender.Container.StoredPercent - receiver.Container.StoredPercent >= FlowValueUtils.MIN_FLOAT_COMPARE;
        }

        internal static bool PartValidator_Sender(INetworkSubPart sender, INetworkSubPart receiver, Predicate<INetworkSubPart> extraValidator = null)
        {
            return (receiver.HasContainer && receiver.Container.FillState != ContainerFillState.Full) && sender.Container.FillState != ContainerFillState.Empty && (extraValidator?.Invoke(receiver) ?? true);
        }
        
        internal static bool PartValidator_Receiver(INetworkSubPart receiver, INetworkSubPart sender, Predicate<INetworkSubPart> extraValidator = null)
        {
            return (sender.HasContainer && sender.Container.FillState != ContainerFillState.Empty) && receiver.Container.FillState != ContainerFillState.Full && (extraValidator?.Invoke(sender) ?? true);
        }
        
        internal static bool PartValidator_AnyWithContainer(INetworkSubPart part)
        {
            return part.HasContainer;
        }
    }

    private static IEnumerable<INetworkSubPart> AdjacentParts(TransactionRequest request)
    {
        var part = request.requester;
        var graph = part.Network.Graph;
        var adjacencyList = graph.GetAdjacencyListEdge(part);
        foreach (var partEdge in adjacencyList)
        {
            var subPart = partEdge.Item1;
            var edge = partEdge.Item2;
            if ((subPart.NetworkRole & request.requestedRole) == 0) continue;
            
            if (edge.IsBiDirectional)
                yield return ResolvePartFinal(subPart, request);

            if (request.FlowDir == ValueFlowDirection.Positive && edge.startNode == part)
                yield return ResolvePartFinal(subPart, request);

            if (request.FlowDir == ValueFlowDirection.Negative && edge.endNode == part)
                yield return ResolvePartFinal(subPart, request);
        }
    }

    private static INetworkSubPart ResolvePartFinal(INetworkSubPart part, TransactionRequest request)
    {
        if ((part.NetworkRole & NetworkRole.Passthrough) == 0) return part;
        
        var graph = part.Network.Graph;   
        var adjacencyList = graph.GetAdjacencyListEdge(part);
        
        foreach (var partEdge in adjacencyList)
        {
            var subPart = partEdge.Item1;
            var edge = partEdge.Item2;
            if ((subPart.NetworkRole & request.requestedRole) == 0) continue;
            
            if (edge.IsBiDirectional)
                 return ResolvePartFinal(subPart, request);

            if (request.FlowDir == ValueFlowDirection.Positive && edge.startNode == part)
                return ResolvePartFinal(subPart, request);

            if (request.FlowDir == ValueFlowDirection.Negative && edge.endNode == part)
                return ResolvePartFinal(subPart, request);
        }
        
        return part;
    }
    

    internal static void DoTransaction(TransactionRequest request)
    {
         if (!request.IsValid) return;
        
        //Search for potential transaction partners
        foreach (var adjacentPart in AdjacentParts(request))
        {
            if (!adjacentPart.HasContainer) continue; //Cant do transaction without containers
            if (!request.partValidator?.Invoke(adjacentPart) ?? false) continue; //Custom Validator check
            if (request.requester.CanInteractWith(adjacentPart)) //Custom interaction check
                request.transaction.Invoke(adjacentPart);
        }

        /*
        var transactionPartnersResult = request.requester.Network.Graph.ProcessRequest(new NetworkGraphPathRequest(request));
        
        if (!transactionPartnersResult.IsValid)
        {
            return;
        }

        //Do transaction-action on partner
        foreach (var targetPart in transactionPartnersResult.allTargets)
        {
            request.transaction.Invoke(targetPart);
        }
        */

        //Debug stuff?
        // foreach (var path in requestResult.allPaths)
        // {
        //     path.edgesOnPath.Do(p => ((NetworkSubPart) p.fromNode).SetFlowDir(p.toNode));
        // }

        //Old implementation (Global network pre-search with fixed targets)
        // foreach (var subPart in Network.PartSet[forRole])
        // {
        //     if (validator(subPart))
        //     {
        //         var result = Network.Graph.GetPart(new NetworkGraphNodeRequest(fromPart, subPart));
        //         if (result == subPart)
        //         {
        //             funcOnPart.Invoke(subPart);
        //         }
        //     }
        // }
    }
}