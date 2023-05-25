using System.Text.RegularExpressions;
using System.Xml;
using OneOf;
using Verse;

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

public class DefValueLoadable<TDef> : IExposable
where TDef : Def
{
    private TDef? def;
    private OneOf<int, float> value;
    
    public TDef Def => def;
    
    public OneOf<int, float> Value
    {
        get => value;
        set => this.value = value;
    }

    public bool IsValid => def != null && value.Value != null;
    
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        //Listing
        if (xmlRoot.Name == "li")
        {
            var innerValue = xmlRoot.InnerText;
            string s = Regex.Replace(innerValue, @"\s+", "");
            string[] array = s.Split(',');
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(def)}", array[0]);
            value = ParseHelper.FromString<OneOfLoadable>(array.Length > 1 ? array[1] : "0").Value;
        }

        //InLined
        else
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, $"{nameof(def)}", xmlRoot.Name);
            value = ParseHelper.FromString<OneOfLoadable>(xmlRoot.FirstChild.Value).Value;
        }
    }
    
    public void ExposeData()
    {
        int? t0 = value.IsT0 ? value.AsT0 : null;
        float? t1 = value.IsT1 ? value.AsT1 : null;
        
        Look<TDef>(ref def, "def");
        Scribe_Values.Look(ref t0, "intVal");
        Scribe_Values.Look(ref t1, "floatVal");

        if (t0.HasValue)
        {
            value = t0.Value;
        }
        
        if (t1.HasValue)
        {
            value = t1.Value;
        }
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