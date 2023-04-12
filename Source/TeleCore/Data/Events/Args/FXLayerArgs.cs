using System;

namespace TeleCore.Data.Events;

public class FXArgs : EventArgs
{
    public int index;
    public string layerTag;
    public string categoryTag;
    public bool needsPower;
}

public class FXEffecterArgs : FXArgs
{
    public FXEffecterData data;
}

public class FXLayerArgs : FXArgs
{
    public int renderPriority;
    public FXLayerData data;

    public static implicit operator int(FXLayerArgs args) => args.index;
    public static implicit operator string(FXLayerArgs args) => args.layerTag;
}