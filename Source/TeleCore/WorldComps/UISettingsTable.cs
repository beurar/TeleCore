using System.Collections.Generic;
using Verse;

namespace TeleCore;

public class UISettingsTable : IExposable
{
    private Dictionary<ThingDef, bool> favoritedOptions = new Dictionary<ThingDef, bool>();

    //
    public Dictionary<ThingDef, bool> FavoritedOptions => favoritedOptions;

    //
    public void ExposeData()
    {
        Scribe_Collections.Look(ref favoritedOptions, "favoritedOptions");
    }

    //
    public void ToggleMenuOptionFavorite(ThingDef def)
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
        
    public bool MenuOptionIsFavorited(ThingDef def)
    {
        return favoritedOptions.TryGetValue(def, out bool value) && value;;
    }
}