using System.Text.RegularExpressions;
using System.Xml;
using Verse;

namespace TeleCore;

public class DefCountChance
{
    public Def def;
    public int count;
    public float chance;

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        var innerValue = xmlRoot.InnerText;
        string s = Regex.Replace(innerValue, @"\s+", "");
        string[] array = s.Split(',');
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(def)}", array[0]);
        if(array.Length > 1)
            count = ParseHelper.FromString<int>(array.Length > 1 ? array[1] : "1");
        if(array.Length > 2)
            chance = ParseHelper.FromString<float>(array.Length > 1 ? array[2] : "1");
    }
}