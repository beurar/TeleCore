using System;
using System.ComponentModel;
using System.Linq;
using RimWorld;
using UnityEngine;

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
                //
                ContainerTransferUtility.TryEqualizeAll(sender.Container, receiver.Container);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
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
        internal static bool StoreEvenly_EQ_Check(INetworkSubPart selfPart, INetworkSubPart otherPart)
        {
            //Only send - we cannot request without special cases (checking path for directionality)
            return selfPart.Container.TotalStored - otherPart.Container.TotalStored >= 2;
        }

        /// <summary>
        /// Determines whether to set owned cached requests dirty.
        /// </summary>
        internal static bool StoreEvenly_EQ_DirtyCheck(INetworkSubPart a, INetworkSubPart b)
        {
            return StoreEvenly_EQ_Check(a, b);
        }
        
        internal static bool PartValidator_AnyWithContainer(INetworkSubPart part)
        {
            return part.HasContainer;
        }

        internal static bool PartValidator_Sender(INetworkSubPart sender, INetworkSubPart receiver, Predicate<INetworkSubPart> extraValidator = null)
        {
            return (receiver.HasContainer && !receiver.Container.Full) && sender.Container.NotEmpty && (extraValidator?.Invoke(receiver) ?? true);
        }
        
        internal static bool PartValidator_Receiver(INetworkSubPart receiver, INetworkSubPart sender, Predicate<INetworkSubPart> extraValidator = null)
        {
            return (sender.HasContainer && sender.Container.NotEmpty) && !receiver.Container.Full && (extraValidator?.Invoke(sender) ?? true);
        }
    }

    //Transaction - What is it?
    //A transaction is the exchange of a value between two actors
    //Any networkSubPart can send a transaction request to another part
    //This request can be to send or receive a value
    
    //The request has a filter to find potential interactable partners first
    public struct TransactionRequest
    {
        //Request
        public readonly INetworkSubPart requester;
        public readonly NetworkRole requesterRole;
        
        //Filter
        public readonly int maxDepth = Int32.MaxValue;
        public readonly NetworkRole requestedRole;
        public readonly Predicate<INetworkSubPart> partValidator = null;
        public readonly Predicate<INetworkSubPart> dirtyChecker = null;

        //Transaction
        public readonly Action<INetworkSubPart> transaction;

        public NetworkContainer Container => requester?.Container;
        //Cannot do a transaction without a container
        public bool IsValid => Container != null;
        
        //Network Transaction
        //1. Request Partner Node(s)
        //2. Do Transaction-Action
        
        public TransactionRequest(INetworkSubPart requester, NetworkRole sendingRole, NetworkRole requestedRole, Action<INetworkSubPart> transaction, Predicate<INetworkSubPart> partValidator = null, Predicate<INetworkSubPart> dirtyChecker = null)
        {
            this.requester = requester;
            this.requesterRole = sendingRole;
            this.requestedRole = requestedRole;
            this.partValidator = partValidator;
            this.dirtyChecker = dirtyChecker;

            this.transaction = transaction;
        }
    }

    internal static void DoTransaction(TransactionRequest request)
    {
        if (!request.IsValid) return;
        
        //Search for potential transaction partners
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

    internal static void ProcessTransferRequest(TransactionRequest request)
    {
        if (request.Container.Empty) return;

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
}