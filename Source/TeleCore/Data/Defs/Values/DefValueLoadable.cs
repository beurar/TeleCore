using System.Text.RegularExpressions;
using System.Xml;
using Verse;

namespace TeleCore;

public class DefValueLoadable<TDef, TValue> : IExposable
    where TDef : Def
{
    private TDef _def;

    public DefValueLoadable()
    {
    }

    public DefValueLoadable(TDef def, TValue value)
    {
        _def = def;
        Value = value;
    }

    public TDef Def => _def;

    public TValue Value { get; set; }

    public bool IsValid => _def != null && Value != null;

    public void ExposeData()
    {
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        //Listing
        if (xmlRoot.Name == "li")
        {
            var innerValue = xmlRoot.InnerText;
            var s = Regex.Replace(innerValue, @"\s+", "");
            var array = s.Split(',');
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(_def)}", array[0]);
            Value = ParseHelper.FromString<TValue>(array.Length > 1 ? array[1] : "0");
        }

        //InLined
        else
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(_def)}", xmlRoot.Name);
            Value = ParseHelper.FromString<TValue>(xmlRoot.FirstChild.Value);
        }
    }

    private static void Look<T>(ref T value, string label) where T : Def
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            string text;
            text = value == null ? "null" : value.defName;
            Scribe_Values.Look<string>(ref text, label, "null");
            return;
        }

        if (Scribe.mode == LoadSaveMode.LoadingVars)
            value = ScribeExtractor.DefFromNodeUnsafe<T>(Scribe.loader.curXmlParent[label]);
    }
}