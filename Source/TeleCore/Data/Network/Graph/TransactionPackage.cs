using System;
using System.Collections.Generic;
using TeleCore.FlowCore;
using TeleCore.Static.Utilities;

namespace TeleCore.Network;

public enum TransactionKind
{
    Basic,
    //Storage
    Equalize, //Equalizes between Storages
    Purge,    //Purges Storage
    //Produce
    Provide,
    //Consumer/Requester
    Request
}

public readonly struct TransactionPackage
{
    public readonly TransactionRequest Request;
    public readonly NetworkSubPart AnticipatedDestination;
    public readonly DefValueStack<NetworkValueDef> ValueStack;

    public TransactionPackage(TransactionRequest request, NetworkSubPart dest, DefValueStack<NetworkValueDef> stack)
    {
        Request = request; 
        AnticipatedDestination = dest;
        ValueStack = stack;
    }

    public void Send()
    {
        AnticipatedDestination.Notify_ReceivePackage(this);
    }
    
    public void SplitPackage(List<NetworkSubPart> newTargets)
    {
        var stackSplit = ValueStack / newTargets.Count;
        var pkgs = new TransactionPackage[newTargets.Count];
        for (var i = 0; i < newTargets.Count; i++)
        {
            var target = newTargets[i];
            pkgs[i] = new TransactionPackage(Request, target, stackSplit);
        }

        for (var j = 0; j < pkgs.Length; j++)
        {
            pkgs[j].Send();
        }
    }

    public void Consume()
    {
        Request.FinalizeTransaction(AnticipatedDestination);
    }
}