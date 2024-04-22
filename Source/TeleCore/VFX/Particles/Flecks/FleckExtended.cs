using UnityEngine;
using Verse;

namespace TeleCore;

public struct FleckMuzzleFlash : IFleck
{
    public FleckDef def;
    public FleckDrawPosition position;
    public int setupTick;
    public Vector3 spawnPosition;
    
    public void Setup(FleckCreationData creationData)
    {
    }

    public FleckMuzzleFlash(Graphic graphic)
    {
        
    }

    public bool TimeInterval(float deltaTime, Map map)
    {
        return true;
    }

    public void Draw(DrawBatch batch)
    {
    }
}