using System.Collections.Generic;

namespace TeleCore.Network.PressureSystem;

public class FlowContent
{
    public FlowValueStack prevContent;
    public FlowValueStack content;
    public int maxContent;
}

public class NetBox
{
    private NetworkSubPart part;
    private Dictionary<NetworkSubPart, FlowContent> parts;
}

public class NetworkModel
{
    private NetworkGraph _graph;
    
}