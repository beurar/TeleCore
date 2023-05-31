using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeleCore.Network;

//Transaction - What is it?
//A transaction is the exchange of a value between two actors
//Any networkSubPart can send a transaction request to another part
//This request can be to send or receive a value
    
//The request has a filter to find potential interactable partners first
public readonly struct TransactionRequest
{
    public readonly NetworkSubPart Requester;
    public readonly NetworkRole RequestedRole;
    public readonly TransactionKind TransactionKind;
    public readonly List<FlowValueDef> Filter;
    public readonly Predicate<INetworkSubPart> PartValidator = null;
    public readonly Action<INetworkSubPart> Transaction;

    public NetworkContainer? Container => Requester?.Container;
    
    public ValueFlowDirection FlowDir => ValueFlowDirection.Positive;
    
    //Cannot do a transaction without a container
    public bool IsValid => Container != null;
    
    public void Attempt()
    {
        if (!IsValid) return;
         
        var graph = Requester.Network.Graph;
        var adjacencyList = graph.GetAdjacencyListEdge(Requester);
        foreach (var partEdge in adjacencyList)
        {
            var subPart = partEdge.Item1;
            var edge = partEdge.Item2;
            if ((subPart.NetworkRole & RequestedRole) == 0) continue;

            if (edge.IsBiDirectional
                || FlowDir == ValueFlowDirection.Positive && edge.startNode == Requester
                || FlowDir == ValueFlowDirection.Negative && edge.endNode == Requester)
            {
                SendPackage(subPart);
            }
        }
    }

    private void SendPackage(INetworkSubPart part)
    {
        if (part == null) return;
        if (!part.HasContainer) return; //Cant do transaction without containers
        new TransactionPackage(this, part, ).Send();
    }
    
    public void FinalizeTransaction(INetworkSubPart networkSubPart)
    {
        if (!PartValidator?.Invoke(networkSubPart) ?? false) return; //Custom Validator check
        if (Requester.CanInteractWith(networkSubPart)) //Custom interaction check
            Transaction.Invoke(networkSubPart);
    }

    public void GenerateTransactions(NetworkSubPart part, IList<NetworkSubPart> adjacencyList, NetworkRole forRole)
    {
        if (forRole == NetworkRole.Storage)
        {
            var container = part.Container;
            var adjCount = adjacencyList.Count;
            var stackPart = container.ValueStack / adjCount;

            float totalExcess = 0;
            float totalDeficit = 0;

            // Calculate the total excess and deficit amounts
            foreach (var adjPart in adjacencyList)
            {
                float difference = adjPart.Container.TotalStored - stackPart.TotalValue.AsT0;
                if (difference > 0)
                    totalExcess += difference;
                else
                    totalDeficit += Math.Abs(difference);
            }

            foreach (var adjPart in adjacencyList)
            {
                float difference = adjPart.Container.TotalStored - stackPart.TotalValue.AsT0;
                float ratio = difference > 0 ? difference / totalExcess : Math.Abs(difference) / totalDeficit;

                foreach (var value in container.ValueStack)
                {
                    int amountToDistribute = Mathf.RoundToInt(value.ValueInt * ratio);
                    var result = difference > 0 ? adjPart.Container.TryAddValue(value, amountToDistribute) : adjPart.Container.TryRemoveValue(value, amountToDistribute);
                    if (result)
                    {
                        if (difference > 0)
                            container.TryRemoveValue(value, result.ActualAmount);
                        else
                            container.TryAddValue(value, result.ActualAmount);
                    }
                }
            }
        }
    }
}