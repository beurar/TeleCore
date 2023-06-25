using TeleCore.Network.IO;

namespace TeleCore.Network.Utility;

public static class IOUtils
{
    internal const char Input = 'I';
    internal const char Output = 'O';
    internal const char TwoWay = '+';
    internal const char Empty = '#';
    internal const char Visual = '=';
    
    internal const string RegexPattern = @"\[[^\]]*\]|.";

    public static bool Matches(this NetworkIOMode innerMode, NetworkIOMode outerMode)
    {
        var innerInput = (innerMode | NetworkIOMode.Input) == NetworkIOMode.Input;
        var outerInput = (outerMode | NetworkIOMode.Input) == NetworkIOMode.Input;
            
        var innerOutput = (innerMode | NetworkIOMode.Output) == NetworkIOMode.Output;
        var outerOutput = (outerMode | NetworkIOMode.Output) == NetworkIOMode.Output;
            
        return (innerInput && outerOutput) || (outerInput && innerOutput);
    }
}