using System.Reflection;
using Verse;

namespace TeleCore;

public static class TeleCoreDebugViewSettings
{
    public static bool DrawNetwork = true;
    public static bool ShowNetworks = false;
    public static bool ShowNetworkParts = false;
    public static bool ShowNetworkPartLinks = false;
    public static bool ShowRoomtrackers = false;
    public static bool DrawRoomLabels { get; set; }
    public static bool DrawGraphOnGUI { get; set; } = true;
}

public class DebugSettings_Telecore : DebugTabMenu
{
    public DebugSettings_Telecore(DebugTabMenuDef def, Dialog_Debug dialog, DebugActionNode rootNode) : base(def, dialog, rootNode)
    {
    }

    public override DebugActionNode InitActions(DebugActionNode absRoot)
    {
        myRoot = new DebugActionNode("TeleCore", DebugActionType.Action, null, null);
        absRoot.AddChild(myRoot);
        foreach (FieldInfo fi in typeof(TeleCoreDebugViewSettings).GetFields())
        {
            this.AddNode(fi, "IM DEBUGGGIINNNGGGGG");
        }
        return this.myRoot;
    }
    
    private void AddNode(FieldInfo fi, string categoryLabel)
    {
        if (fi.IsLiteral)
        {
            return;
        }
        DebugActionNode debugActionNode = new DebugActionNode(this.LegibleFieldName(fi), DebugActionType.Action, delegate()
        {
            bool flag = (bool)fi.GetValue(null);
            fi.SetValue(null, !flag);
            MethodInfo method = fi.DeclaringType.GetMethod(fi.Name + "Toggled", BindingFlags.Static | BindingFlags.Public);
            if (method != null)
            {
                method.Invoke(null, null);
            }
        }, null);
        debugActionNode.category = categoryLabel;
        debugActionNode.settingsField = fi;
        this.myRoot.AddChild(debugActionNode);
    }
    
    private string LegibleFieldName(FieldInfo fi)
    {
        return GenText.SplitCamelCase(fi.Name).CapitalizeFirst();
    }
}