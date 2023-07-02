using Verse;

namespace TeleCore;

public class DiscoveryProperties
{
    [Unsaved()] private TaggedString cachedUnknownLabelCap = null;

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

public class DiscoveryDef : Def
{
    //public WikiEntryDef wikiEntry;

    public void Discover()
    {
        TFind.Discoveries.Discover(this);
    }
}

public interface IDiscoverable
{
    DiscoveryDef DiscoveryDef { get; }
    bool Discovered { get; }

    string DiscoveredLabel { get; }
    string UnknownLabel { get; }
    string DiscoveredDescription { get; }
    string UnknownDescription { get; }
    string DescriptionExtra { get; }
}