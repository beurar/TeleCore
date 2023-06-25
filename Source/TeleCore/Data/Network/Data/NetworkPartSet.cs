using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeleCore.Defs;
using Verse;

namespace TeleCore.Network.Data;

public class NetworkPartSetExtended : NetworkPartSet
{
    private readonly INetworkPart? _holder;
    private INetworkPart? _controller;
    private string? _cachedString;
    
    //stores references to all ticking parts
    private readonly HashSet<INetworkPart> _tickSet;
    
    public ICollection<INetworkPart> TickSet => _tickSet;
    
    public NetworkPartSetExtended(NetworkDef def) : base(def)
    {
    }

    public override void Dispose()
    {
        _tickSet.Clear();
    }

    protected override void OnPartAdded(INetworkPart part)
    {
        if (!part.IsEdge)
        {
            _tickSet.Add(part);
        }

        UpdateString();
    }

    protected override void OnPartRemoved(INetworkPart part)
    {
        _tickSet.Remove(part);
        UpdateString();
    }

    private void UpdateString()
    {
        var sb = new StringBuilder();
        if(_controller != null)
            sb.AppendLine($"CONTROLLER: {_controller.Parent.Thing}");
        foreach (NetworkRole role in Enum.GetValues(typeof(NetworkRole)))
        {
            sb.AppendLine($"{role}: ");
            foreach (var part in _partsByRole[role])
            {
                sb.AppendLine($"    - {part.Parent.Thing}");
            }
        }
        sb.AppendLine($"Total Count: {_fullSet.Count}");
        _cachedString = sb.ToString();
    }

    public override string ToString()
    {
        if (_cachedString == null)
            UpdateString();
        return _cachedString;
    }
}

public class NetworkPartSet : IDisposable
{
    protected readonly NetworkDef _def;
    protected readonly HashSet<INetworkPart> _fullSet;
    protected readonly Dictionary<NetworkRole, HashSet<INetworkPart>> _partsByRole;
    
    public ICollection<INetworkPart> FullSet => _fullSet;

    //
    public INetworkPart PartByPos(IntVec3 pos)
    {
        return _fullSet.FirstOrFallback(p => p.Thing.OccupiedRect().Contains(pos));
    }

    public HashSet<INetworkPart>? this[NetworkRole role]
    {
        get => _partsByRole.TryGetValue(role, out var value) ? value : null;
    }
        
    // public INetworkPart? this[IntVec3 pos]
    // {
    //     get => structuresByPosition.TryGetValue(pos, out var value) ? value : null;
    // }
    
    public NetworkPartSet(NetworkDef def)
    {
        _def = def;
        _fullSet = new HashSet<INetworkPart>();
        _partsByRole = new Dictionary<NetworkRole, HashSet<INetworkPart>>();
    }

    public virtual void Dispose()
    {
        _fullSet.Clear();
        _partsByRole.Clear();
    }
    
    #region Registration
    
    public bool AddComponent(INetworkPart? part)
    {
        if (part == null) return false;
        if (part.Config.networkDef != _def) return false;
        if (_fullSet.Contains(part)) return false;
        
        _fullSet.Add(part);
        foreach (var flag in part.Config.role.AllFlags())
        {
            if (!_partsByRole[flag].Add(part))
            {
                TLog.Warning($"Trying to add existing item: {part} for role {flag}.");
            }
        }
        
        OnPartAdded(part);
        return true;
    }
    
    public void RemoveComponent(INetworkPart part)
    {
        if (!_fullSet.Contains(part)) return;
        
        foreach (var flag in part.Config.role.AllFlags())
        {
            _partsByRole[flag].Remove(part);
        }
        
        _fullSet.Remove(part);
        OnPartRemoved(part);
    }

    protected virtual void OnPartAdded(INetworkPart part)
    {
    }
    
    protected virtual void OnPartRemoved(INetworkPart part)
    {
    }

    #endregion

}