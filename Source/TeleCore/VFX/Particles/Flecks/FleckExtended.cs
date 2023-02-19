using UnityEngine;
using Verse;

namespace TeleCore;

//TODO: Make new fleck based on thrown which has speed falloff and flight curves
public struct FleckExtended : IFleck
{
    public FleckThrown baseData;
    public float speedFalloff;

    public void Setup(FleckCreationData creationData)
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