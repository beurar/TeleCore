using System.Text.RegularExpressions;
using System.Xml;
using OneOf;
using Verse;

namespace TeleCore;

public class DefValueLoadable<TDef, TValue> : IExposable
where TDef : Def
{
    private TDef _def;
    private TValue _value;

    public DefValueLoadable(){}

    public DefValueLoadable(TDef def, TValue value)
    {
        _def = def;
        _value = value;
    }

    public TDef Def => _def;
    
    public TValue Value
    {
        get => _value;
        set => this._value = value;
    }

    public bool IsValid => _def != null && _value != null;
    
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        //Listing
        if (xmlRoot.Name == "li")
        {
            var innerValue = xmlRoot.InnerText;
            string s = Regex.Replace(innerValue, @"\s+", "");
            string[] array = s.Split(',');
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(_def)}", array[0]);
            _value = ParseHelper.FromString<TValue>(array.Length > 1 ? array[1] : "0");
        }

        //InLined
        else
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(_def)}", xmlRoot.Name);
            _value = ParseHelper.FromString<TValue>(xmlRoot.FirstChild.Value);
        }
    }
    
    public void ExposeData()
    {
    }

    private static void Look<T>(ref T value, string label) where T : Def
    {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            string text;
            text = value == null ? "null" : value.defName;
            Scribe_Values.Look<string>(ref text, label, "null", false);
            return;
        }
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            value = ScribeExtractor.DefFromNodeUnsafe<T>(Scribe.loader.curXmlParent[label]);
        }
    }
}