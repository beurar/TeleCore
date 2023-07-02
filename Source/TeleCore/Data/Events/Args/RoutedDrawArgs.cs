using UnityEngine;
using Verse;

namespace TeleCore.Data.Events;

public struct RoutedDrawArgs
{
    public Graphic graphic;
    public Mesh mesh;

    public Vector3 drawPos;
    public float altitude;
    public float rotation;

    //drawMesh, new Vector3(drawPos.x, _altitude, drawPos.z), rotationQuat, _drawMat, 0, null, 0, _materialProperties   
}