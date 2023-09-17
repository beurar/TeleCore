using Verse;

namespace TeleCore;

public class DiscoveryProperties : Editable
{
    [Unsaved()] 
    private TaggedString cachedUnknownLabelCap = null;

    public DiscoveryDef discoveryDef;
    public string extraDescription;
    public string unknownDescription;
    public string unknownLabel;

    public string UnknownLabelCap
    {
        get
        {
            if (cachedUnknownLabelCap.NullOrEmpty())
                cachedUnknownLabelCap = unknownLabel.CapitalizeFirst();
            return cachedUnknownLabelCap;
        }
    }
}