using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore;

public struct NetworkGraphPath
{
    public readonly INetworkSubPart start;
    public readonly INetworkSubPart end;
    public readonly INetworkSubPart[] fullPath;
    public readonly HashSet<NetEdge> edgesOnPath;

    public IEnumerable<INetworkSubPart> PathWithoutEnds => fullPath.Except(start).Except(end);

    public NetworkGraphPath(IList<INetworkSubPart> fullPath)
    {
        start = fullPath.First();
        end = fullPath.Last();
        this.fullPath = new INetworkSubPart[fullPath.Count];
        
        //
        var edgeSet = new HashSet<NetEdge>();
        int i = 0;
        foreach (var subPart in fullPath)
        {
            this.fullPath[i] = subPart;
            var partTo = (i + 1) < fullPath.Count ? fullPath[i + 1] : null;
            if (subPart.Network.Graph.TryGetEdge(subPart, partTo, out var edge))
            {
                edgeSet.Add(edge);
            }

            i++;
        }
        edgesOnPath = edgeSet;
    }

    public override string ToString()
    {
        return $"{start} -[{fullPath.Length}]-> {end}";
    }
}

public struct NetworkGraphPathResult
{
    public readonly NetworkGraphPathRequest request;
    public readonly NetworkGraphPath[] allPaths;

    //
    public readonly HashSet<INetworkSubPart> allPartsUnique;
    public readonly HashSet<INetworkSubPart> allTargets;

    public bool IsValid => allTargets != null && allTargets.Any();

    public static NetworkGraphPathResult Invalid => new NetworkGraphPathResult()
    {
    };

    public NetworkGraphPathResult(NetworkGraphPathRequest request, List<List<INetworkSubPart>> allResults)
    {
        TLog.Debug($"Making Results: {allResults.Count}");
        //
        this.request = request;
        allPaths = new NetworkGraphPath[allResults.Count];
        
        //
        allPartsUnique = new();
        allTargets = new HashSet<INetworkSubPart>();
        for (var i = 0; i < allResults.Count; i++)
        {
            allPaths[i] = new NetworkGraphPath(allResults[i]);
            allPartsUnique.AddRange(allPaths[i].fullPath);
            allTargets.Add(allPaths[i].end);
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
    internal void Debug_Draw()
    {
        foreach (var path in allPaths)
        {
            GenDraw.DrawFieldEdges(path.start.Parent.Thing.Position.ToSingleItemList(), Color.red);
            GenDraw.DrawFieldEdges(path.PathWithoutEnds.Select(c => c.Parent.Thing.Position).ToList(), Color.green);
            GenDraw.DrawFieldEdges(path.end.Parent.Thing.Position.ToSingleItemList(), Color.blue);
        }
    }

    public override string ToString()
    {
        return $"[{IsValid}] Paths: {allPaths.Length} | Parts: {allPartsUnique.Count} | Targets: {allTargets.Count}";
    }
}