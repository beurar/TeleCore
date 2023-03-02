using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using RimWorld;
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
            var usedTypes = sender.Props.AllowedValues;
            for (int i = usedTypes.Count - 1; i >= 0; i--)
            {
                var type = usedTypes[i];
                if (!receiver.Parent.AcceptsValue(type)) continue;
                if (sender.Container.TryTransferTo(receiver.Container, type, 1, out var val))
                {
                    receiver.Notify_ReceivedValue();
                    MoteMaker.ThrowText(receiver.Parent.Thing.DrawPos, sender.Parent.Thing.Map, $"{val}", Color.green);
                }
            }
        }

        internal static void TransferToOtherSpecific(INetworkSubPart sender, INetworkSubPart receiver, NetworkValueDef def)
        {
            if (!receiver.Parent.AcceptsValue(def)) return;
            if (sender.Container.TryTransferTo(receiver.Container, def, 1, out var val))
            {
                receiver.Notify_ReceivedValue();
                MoteMaker.ThrowText(receiver.Parent.Thing.DrawPos, sender.Parent.Thing.Map, $"{val}", Color.green);
            }
        }
        
        internal static void TransferToOtherPurge(INetworkSubPart sender, INetworkSubPart? receiver)
        {
            if (receiver == null)
            {
                TLog.Warning("Transaction receiver is null.");
                return;
            }
            
            foreach (var type in sender.Container.AllStoredTypes)
            {
                if(!sender.Container.GetFilterFor(type).canStore)
                    TransferToOtherSpecific(sender, receiver, type);
            }
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
                if (sender.Container.TryTransferTo(receiver.Container, type, 1, out var val))
                {
                    receiver.Notify_ReceivedValue();
                    MoteMaker.ThrowText(receiver.Parent.Thing.DrawPos, sender.Parent.Thing.Map, $"{val}", Color.green);
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

    private static IEnumerable<INetworkSubPart> AdjacentParts(INetworkSubPart part, ValueFlowDirection flowDir)
    {
        var graph = part.Network.Graph;
        var adjacencyList = graph.GetAdjacencyList(part);
        if (adjacencyList == null) yield break;
        foreach (var subPart in adjacencyList)
        {
            if (graph.GetAnyEdgeBetween(part, subPart, out var edge))
            {
                if (edge.IsBiDirectional)
                    yield return subPart;

                if (flowDir == ValueFlowDirection.Positive && edge.startNode == part)
                    yield return subPart;
                
                if (flowDir == ValueFlowDirection.Negative && edge.endNode == part)
                    yield return subPart;
            }
        }
    }

    internal static void DoTransaction(TransactionRequest request)
    {
         if (!request.IsValid) return;
        
        //Search for potential transaction partners
        foreach (var adjacentPart in AdjacentParts(request.requester, request.FlowDir))
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