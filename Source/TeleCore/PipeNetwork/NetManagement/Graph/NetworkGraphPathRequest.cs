using System;
using System.Linq;

namespace TeleCore;

public struct NetworkGraphNodeRequest
{
    private readonly INetworkSubPart _requster = null;
    private readonly INetworkSubPart _target;
    private readonly NetworkRole _role;
    private readonly Predicate<INetworkSubPart> _validator;

    public INetworkSubPart Requester => _requster;

    public NetworkGraphNodeRequest(INetworkSubPart requester, NetworkRole role, Predicate<INetworkSubPart> validator)
    {
        _requster = requester;
        _role = role;
        _target = null;
        _validator = validator;
    }

    public NetworkGraphNodeRequest(INetworkSubPart requester, INetworkSubPart target)
    {
        _requster = requester;
        _target = target;
        _role = 0;
        _validator = null;
    }

    public bool Fits(INetworkSubPart part)
    {
        if (_target != null && part != _target) return false;
        if (_validator != null && !_validator(part)) return false;
        if (_role > 0 && _role.AllFlags().All(part.NetworkRole.AllFlags().Contains)) return false;
        return true;
    }

    public override string ToString()
    {
        return $"Hash: {GetHashCode()}\n" +
               $"Requester: {_requster}" + 
               $"Role: {_role}" + 
               $"Target: {_target}" + 
               $"Validator: {_validator} | {_validator?.GetHashCode()}";
        return base.ToString();
    }

    //TODO: check validator for methodinfo rather than object reference equality
    public override bool Equals(object obj)
    {
        if (obj is NetworkGraphNodeRequest request)
        {
            return request._requster == _requster &&
                   request._target == _target &&
                   request._role == _role &&
                   (request._validator?.Equals(_validator) ?? false);
        }
        return false;
    }
}
