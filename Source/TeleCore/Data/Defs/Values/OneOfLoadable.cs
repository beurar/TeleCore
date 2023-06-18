using System.Xml;
using Verse;
using OneOf;

namespace TeleCore;

public struct OneOfLoadable
{
    public OneOf<int, float> Value { get; set; }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        var val = xmlRoot.InnerText;
        var isF = xmlRoot.InnerText.EndsWith("f");
        var valTxt = isF ? val.Substring(0, val.Length - 1) : val;

        if (xmlRoot.InnerText.EndsWith("f"))
        {
            Value = ParseHelper.FromString<float>(valTxt);
        }
        else
        {
            Value = ParseHelper.FromString<int>(valTxt);
        }
    }
}