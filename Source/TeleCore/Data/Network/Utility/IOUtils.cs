using TeleCore.Network.IO;

namespace TeleCore.Network.Utility;

public static class IOUtils
{
    public static bool Matches(this NetworkIOMode innerMode, NetworkIOMode outerMode)
    {
        var innerInput = (innerMode | NetworkIOMode.Input) == NetworkIOMode.Input;
        var outerInput = (outerMode | NetworkIOMode.Input) == NetworkIOMode.Input;
            
        var innerOutput = (innerMode | NetworkIOMode.Output) == NetworkIOMode.Output;
        var outerOutput = (outerMode | NetworkIOMode.Output) == NetworkIOMode.Output;
            
        return (innerInput && outerOutput) || (outerInput && innerOutput);
    }
}