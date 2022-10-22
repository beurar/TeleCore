using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore;

public struct NetworkGraphPathResult
{
    public readonly NetworkGraphPathRequest request;
    public readonly INetworkSubPart[][] allPaths;
    public readonly NetEdge[][] edges;
    public readonly HashSet<INetworkSubPart> allPartsUnique;
    public readonly HashSet<INetworkSubPart> allTargets;

    //
    public readonly INetworkSubPart[] singlePath;
 
    public bool IsValid => allTargets != null && allTargets.Any();

    public static NetworkGraphPathResult Invalid => new NetworkGraphPathResult()
    {
    };

    public NetworkGraphPathResult(NetworkGraphPathRequest request, List<List<INetworkSubPart>> allResults)
    {
        TLog.Debug($"Request: {allResults.Count}");
        this.request = request;
        allPaths = new INetworkSubPart[allResults.Count][];
        allPartsUnique = new();
        allTargets = new HashSet<INetworkSubPart>();
        singlePath = allPaths.First();
        edges = new NetEdge[allResults.Count][];
        for (var i = 0; i < allResults.Count; i++)
        {
            allPaths[i] = allResults[i].ToArray();
            allPartsUnique.AddRange(allPaths[i]);
            allTargets.Add(allPaths[i].Last());

            
            // 01 23 45 = 3+2
            // 01 23 = 2 + 1
            // 01 23 45 67 = 4+3
            var edgeList = new List<NetEdge>();
            for (int a = 0; a < allPaths[i].Length; a++)
            {
                TLog.Message($"a: {a} / {allPaths[i].Length} | -> {a+1}");
                var partFrom = allPaths[i][a];
                var partTo = (a + 1) < allPaths[i].Length ? allPaths[i][a+1] : null;
                if (request.Requester.Network.Graph.TryGetEdge(partFrom, partTo, out var edge))
                {
                    edgeList.Add(edge);
                }
            }
            edges[i] = edgeList.ToArray();
        }
    }

    /*
    public NetworkGraphPathResult(NetworkGraphPathRequest request, List<INetworkSubPart> result)
    {
        this.request = request;
        singlePath = result.ToArray();
        
        //
        allPaths = new INetworkSubPart[1][] {singlePath};
        allPartsUnique = new HashSet<INetworkSubPart>();
        allPartsUnique.AddRange(result);

        allTargets = new HashSet<INetworkSubPart>() { result.Last() };
        singlePath = null;
    }
    */
}