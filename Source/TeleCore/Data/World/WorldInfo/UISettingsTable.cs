using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class UISettingsTable : IExposable
{
    private Dictionary<BuildableDef, bool> favoritedOptions = new Dictionary<BuildableDef, bool>();

    //
    public Dictionary<BuildableDef, bool> FavoritedOptions => favoritedOptions;

    //
    public void ExposeData()
    {
        Scribe_Collections.Look(ref favoritedOptions, "favoritedOptions");
    }

    //
    public void ToggleMenuOptionFavorite(BuildableDef def)
    {
        if (favoritedOptions.TryGetValue(def, out var value))
        {
            favoritedOptions[def] = !value;
        }
        else
        {
            favoritedOptions.Add(def, true);
        }
    }
        
    public bool MenuOptionIsFavorited(BuildableDef def)
    {
        return favoritedOptions.TryGetValue(def, out bool value) && value;;
    }
}