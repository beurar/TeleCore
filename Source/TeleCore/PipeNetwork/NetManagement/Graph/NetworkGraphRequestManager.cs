using System.Collections.Generic;
using TeleCore.Static.Utilities;
using Verse;

namespace TeleCore;

public class NetworkGraphRequestManager
{
    private readonly NetworkGraph parent;

    //Caching
    private readonly Dictionary<NetworkGraphPathRequest, NetworkGraphPathResult> _cachedRequestResults;
    private readonly Dictionary<INetworkSubPart, List<NetworkGraphPathRequest>> _nodesOnCachedResult;
    private readonly HashSet<NetworkGraphPathRequest> _dirtyRequests;

    public NetworkGraphRequestManager(NetworkGraph graph)
    {
        parent = graph;
        _cachedRequestResults = new();
        _nodesOnCachedResult = new();
        _dirtyRequests = new();
    }

    public void Notify_NodeStateChanged(INetworkSubPart part)
    {
        if (_nodesOnCachedResult.TryGetValue(part, out var requests))
        {
            _dirtyRequests.AddRange(requests);
        }
    }

    private void CheckRequestDirty(NetworkGraphPathRequest request)
    {
        if (_dirtyRequests.Contains(request))
        {
            TLog.Debug("Request is dirty.. removing");
            //If request has been cached
            if (_cachedRequestResults.TryGetValue(request, out var cachedResult))
            {
                //Remove request from all nodes associated
                foreach (var var in cachedResult.allPartsUnique)
                {
                    var list = _nodesOnCachedResult[var];
                    list.Remove(request);


                    //
                    if (list.Count == 0)
                    {
                        TLog.Debug($"Clearing last request binding from {var}");
                        _nodesOnCachedResult.Remove(var);
                    }
                }

                _cachedRequestResults.Remove(request);
                _dirtyRequests.Remove(request);
            }
        }
    }
    
    private NetworkGraphPathResult CreateAndCacheRequest(NetworkGraphPathRequest request)
    {
        TLog.Debug($"Processing new request...\n{request}");

        List<List<INetworkSubPart>> result = GenGraph.Dijkstra(parent, request);
        if (result == null)
        {
            TLog.Warning($"Found no valid path for request: {request}");
            return new NetworkGraphPathResult();
        }
        var requestResult = new NetworkGraphPathResult(request, result);
        _cachedRequestResults.Add(request, requestResult);

        //
        foreach (var part in requestResult.allPartsUnique)
        {
            if (!_nodesOnCachedResult.TryGetValue(part, out var list))
            {
                list = new List<NetworkGraphPathRequest>() { request };
                _nodesOnCachedResult.Add(part, list);
            }
            list.Add(request);
        }

        return requestResult;
    }

    public NetworkGraphPathResult ProcessRequest(NetworkGraphPathRequest request)
    {
        //Check dirty result
        CheckRequestDirty(request);

        //Get existing result
        if (_cachedRequestResults.TryGetValue(request, out var value))
        {
            TLog.Debug("Found cached request... returning");
            return value;
        }

        //
        return CreateAndCacheRequest(request);
    }
}