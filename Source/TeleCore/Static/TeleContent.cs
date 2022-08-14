using UnityEngine;
using Verse;

namespace TeleCore
{
    [StaticConstructorOnStartup]
    public static class TeleContent
    {
        //UI
        public static readonly Texture2D InfoButton = ContentFinder<Texture2D>.Get("UI/Buttons/InfoButton", true);
        public static readonly Texture2D SideBarArrow = ContentFinder<Texture2D>.Get("UI/Icons/Arrow", true);

        //
        public static readonly Texture2D ButtonBGAtlas = ContentFinder<Texture2D>.Get("UI/Buttons/ButtonBG");
        public static readonly Texture2D ButtonBGAtlasMouseover = ContentFinder<Texture2D>.Get("UI/Buttons/ButtonBGMouseover");
        public static readonly Texture2D ButtonBGAtlasClick = ContentFinder<Texture2D>.Get("UI/Buttons/ButtonBGClick");

        //General
        public static readonly Texture2D HightlightInMenu = ContentFinder<Texture2D>.Get("UI/Icons/HighLight", true);
        public static readonly Texture2D OpenMenu = ContentFinder<Texture2D>.Get("UI/Icons/OpenMenu", true);

        //UIElement
        public static readonly Texture2D LockOpen = ContentFinder<Texture2D>.Get("UI/Icons/Animator/LockOpen", true);
        public static readonly Texture2D LockClosed = ContentFinder<Texture2D>.Get("UI/Icons/Animator/LockClosed", true);

        public static readonly Texture2D UIDataNode = ContentFinder<Texture2D>.Get("UI/Icons/Tools/Node", true);

        //TextureElement
        public static readonly Texture2D PivotPoint = ContentFinder<Texture2D>.Get("UI/Icons/Animator/PivotPoint", true);

        //Animation Tool
        //LayerView
        public static readonly Texture2D LinkIcon = ContentFinder<Texture2D>.Get("UI/Icons/Link", false);
        public static readonly Texture2D VisibilityOff = ContentFinder<Texture2D>.Get("UI/Icons/VisibilityOff", false);
        public static readonly Texture2D VisibilityOn = ContentFinder<Texture2D>.Get("UI/Icons/VisibilityOn", false);

        public static readonly Texture2D BurgerMenu = ContentFinder<Texture2D>.Get("UI/Icons/BurgerMenu", false);
        public static readonly Texture2D SettingsWheel = ContentFinder<Texture2D>.Get("UI/Icons/SettingsWheel", false);
        public static readonly Texture2D HelpIcon = ContentFinder<Texture2D>.Get("UI/Icons/Help", false);

        //TimeLine
        public static readonly Texture2D KeyFrame = ContentFinder<Texture2D>.Get("UI/Icons/Animator/KeyFrame", true);
        public static readonly Texture2D KeyFrameSelection = ContentFinder<Texture2D>.Get("UI/Icons/Animator/KeyFrameSelection", true);
        public static readonly Texture2D AddKeyFrame = ContentFinder<Texture2D>.Get("UI/Icons/Animator/AddKeyFrame", true);
        public static readonly Texture2D TimeSelMarker = ContentFinder<Texture2D>.Get("UI/Icons/Animator/TimeSelector", true);
        public static readonly Texture2D PlayPause = ContentFinder<Texture2D>.Get("UI/Icons/Animator/PlayPause", true);
        public static readonly Texture2D TimeSelRangeL = ContentFinder<Texture2D>.Get("UI/Icons/Animator/RangeL", true);
        public static readonly Texture2D TimeSelRangeR = ContentFinder<Texture2D>.Get("UI/Icons/Animator/RangeR", true);

        internal static readonly Texture2D TimeFlag = ContentFinder<Texture2D>.Get("UI/Icons/Animator/FlagAtlas");

        //ModuleVis
        public static readonly Texture Node_Open = ContentFinder<Texture2D>.Get("UI/Icons/Node_Open");
        public static readonly Texture NodeOut_Closed = ContentFinder<Texture2D>.Get("UI/Icons/NodeOut_Closed");
        public static readonly Texture NodeIn_Closed = ContentFinder<Texture2D>.Get("UI/Icons/NodeIn_Closed");

        //Internal RW Crap /FROM TexButton
        public static readonly Texture2D DeleteX = TexButton.DeleteX;
        public static readonly Texture2D Plus = TexButton.Plus;
        public static readonly Texture2D Minus = TexButton.Minus;
        public static readonly Texture2D Infinity = TexButton.Infinity;
        public static readonly Texture2D Suspend = TexButton.Suspend;
        public static readonly Texture2D Copy = TexButton.Copy;
        public static readonly Texture2D Paste = TexButton.Paste;

        //Materials
        public static readonly Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));
        public static readonly Material IOArrow = MaterialPool.MatFrom("Buildings/IOArrow", ShaderDatabase.Transparent);

        public static readonly Material WorldTerrain = MaterialPool.MatFrom("World/Tile/Terrain", ShaderDatabase.WorldOverlayCutout, 3500);
    }
}
