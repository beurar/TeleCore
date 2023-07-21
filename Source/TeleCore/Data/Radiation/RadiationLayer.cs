using TeleCore.Primitive;

namespace TeleCore.Radiation;

public class RadiationLayer
{
    private RadiationTypeDef _def;
    private bool[] _affectedCells;
    private byte[] _radiation;
}

public struct RadiationValue
{
    
}

public struct RadiationStack
{
    public DefValueStack<RadiationTypeDef, byte> Radiation;
    
}

