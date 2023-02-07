namespace TeleCore;

public struct FXLayerArgs
{
    public int index;
    public string layerTag;
    public string categoryTag;

    public static implicit operator int(FXLayerArgs args) => args.index;
    public static implicit operator string(FXLayerArgs args) => args.layerTag;
}