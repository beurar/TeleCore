using System;

namespace TeleCore;

public class FXArgs : EventArgs
{
    public int index;
    public string layerTag;
    public bool needsPower;
}

public class FXEffecterArgs : FXArgs
{
    public FXEffecterData data;
}

public class FXLayerArgs : FXArgs
{
    public int renderPriority;
    public string categoryTag;
    public FXLayerData data;

    public static implicit operator int(FXLayerArgs args) => args.index;
    public static implicit operator string(FXLayerArgs args) => args.layerTag;
}