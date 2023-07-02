using UnityEngine;
using Verse;

namespace TeleCore;

public class Mote_Beam : TeleMote
{
    private Vector3 end;
    private Vector3 start;

    public override void ExposeData()
    {
        base.ExposeData();
    }

    public void SetConnections(Vector3 start, Vector3 end)
    {
        this.start = start;
        this.end = end;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
    }

    public override void Tick()
    {
        base.Tick();
    }

    public override void Draw()
    {
        if (AttachedMat == null) return;
        materialProps ??= new MaterialPropertyBlock();

        var alpha = Alpha;
        /*if (shouldMove && AgeSecs >= props.mote.fadeInTime)
            end2 = Vector3.Lerp(puller, finalEnd, alpha);
        */
        var diff = end - start;
        if (alpha <= 0f) return;
        var color = instanceColor;
        color.a *= alpha;
        materialProps.SetColor("_Color", color);

        var z = diff.MagnitudeHorizontal();
        var pos = (start + end) / 2f;
        pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
        var scale = new Vector3(1f, 1f, z);
        var quat = Quaternion.LookRotation(diff);
        Matrix4x4 matrix = default;
        matrix.SetTRS(pos, quat, scale);
        Graphics.DrawMesh(MeshPool.plane10, matrix, AttachedMat, 0, null, 0, materialProps);
    }
}