using System;

namespace TeleCore;

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

    public NetworkContainer? Container => requester?.Container;
    
    //Cannot do a transaction without a container
    public bool IsValid => Container != null;
    
    public ValueFlowDirection FlowDir => ValueFlowDirection.Positive;

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