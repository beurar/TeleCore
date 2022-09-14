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
}
