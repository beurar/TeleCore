using System.Collections.Generic;
using TeleCore.Primitive;

namespace TeleCore.Radiation;

public class RadiationLayer
{
    private RadiationTypeDef _def;
    private List<IRadiationSource> _sources = new List<IRadiationSource>();
    
    private bool[] _affectedCells;
    private byte[] _radiation;
}

public interface IRadiationSource
{
}

public struct RadiationValue
{
    
}

public struct RadiationStack
{
    public DefValueStack<RadiationTypeDef, byte> Radiation;
    
}

