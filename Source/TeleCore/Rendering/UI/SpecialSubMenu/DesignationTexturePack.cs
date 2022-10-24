using UnityEngine;
using Verse;

namespace TeleCore;

public class DesignationTexturePack
{
    private static string DefaultPackPath = "Menu/SubBuildMenu";
        
    //
    public Texture2D backGround;
    public Texture2D tab;
    public Texture2D tabSelected;
    public Texture2D designator;
    public Texture2D designatorSelected;

    //TODO:Document
    public DesignationTexturePack(string packPath, Def fromDef)
    {
        packPath ??= DefaultPackPath;
        backGround = ContentFinder<Texture2D>.Get(packPath + "/BuildMenu");
        tab = ContentFinder<Texture2D>.Get(packPath + "/Tab");
        tabSelected = ContentFinder<Texture2D>.Get(packPath + "/Tab_Sel");
        designator = ContentFinder<Texture2D>.Get(packPath + "/Des");
        designatorSelected = ContentFinder<Texture2D>.Get(packPath + "/Des_Sel");
    }
}