using UnityEngine;
using Verse;

namespace TeleCore;

public class Mote_MuzzleFlash : TeleMote
{
    private Vector3 lookVector;

    public void SetLookDirection(Vector3 exactPos, Vector3 target)
    {
        exactPosition = exactPos;
        lookVector = target;
    }

    public override void Draw()
    {
        if (AttachedMat == null) return;
        materialProps ??= new MaterialPropertyBlock();

        //
        var alpha = Alpha;
        if (alpha <= 0f) return;
        var color = instanceColor;
        color.a *= alpha;
        materialProps.SetColor("_Color", color);

        var diff = lookVector - exactPosition;
        var quat = Quaternion.LookRotation(diff);
        Matrix4x4 matrix = default;
        matrix.SetTRS(exactPosition, quat, exactScale);
        Graphics.DrawMesh(MeshPool.plane10, matrix, AttachedMat, 0, null, 0, materialProps);
    }
}