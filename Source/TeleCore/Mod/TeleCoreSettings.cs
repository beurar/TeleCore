using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class TeleCoreSettings : ModSettings
    {
        internal bool enableProjectileGraphicRandomFix = false;
        internal AnimationSettings animationSettings = new();

        //
        public bool ProjectileGraphicRandomFix => enableProjectileGraphicRandomFix;

        //
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableProjectileGraphicRandomFix, "enableProjectileGraphicRandomFix");
            Scribe_Deep.Look(ref animationSettings, "animationSettings");
        }
    }

    internal class AnimationSettings : IExposable
    {
        private string userDefinedAnimationDefLocation = null;
        private Dictionary<string, bool> textureBrowserSettings = new();

        public string SaveAnimationDefLocation => userDefinedAnimationDefLocation;

        internal bool AllowsModInBrowser(ModContentPack mod)
        {
            return !textureBrowserSettings.TryGetValue(mod.Name, out var value) || value;
        }

        internal void SetAllowedModInBrowser(ModContentPack mod, bool setting)
        {
            if (!textureBrowserSettings.ContainsKey(mod.Name))
            {
                textureBrowserSettings.Add(mod.Name, setting);
                return;
            }
            textureBrowserSettings[mod.Name] = setting;
        }

        internal void SetAnimationDefLocation(string newPath)
        {
            userDefinedAnimationDefLocation = newPath;
        }

        internal void ResetAnimationDefLocation()
        {
            SetAnimationDefLocation(StringCache.DefaultAnimationDefLocation);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref userDefinedAnimationDefLocation, "userDefinedAnimationDefLocation");
            Scribe_Collections.Look(ref textureBrowserSettings, "textureBrowserSettings");

            if (userDefinedAnimationDefLocation == null)
            {
                SetAnimationDefLocation(StringCache.DefaultAnimationDefLocation);
            }
        }
    }
}
