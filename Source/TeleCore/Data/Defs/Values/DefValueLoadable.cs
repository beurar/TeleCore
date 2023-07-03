using System.Text.RegularExpressions;
using System.Xml;
using TeleCore.Primitive;
using Verse;

namespace TeleCore;

public class DefValueLoadable<TDef, TValue> : IExposable
    where TDef : Def
    where TValue : struct
{
    private TDef _def;
    private Numeric<TValue> _value;
    
    public TDef Def => _def;
    public Numeric<TValue> Value => _value;
    public bool IsValid => _def != null!;
    
    public static implicit operator DefValue<TDef, TValue>(DefValueLoadable<TDef, TValue> refValue) => new(refValue.Def, refValue.Value);
    
    public void ExposeData()
    {
        //Scribe_Defs.Look(ref _def, "def");
        Look(ref _def, "def");
        Scribe_Values.Look(ref _value, "value");
        
        return;

        static void Look<T>(ref T value, string label) where T : Def
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
    
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        //Listing
        if (xmlRoot.Name == "li")
        {
            var innerValue = xmlRoot.InnerText;
            var s = Regex.Replace(innerValue, @"\s+", "");
            var array = s.Split(',');
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
}