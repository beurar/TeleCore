using System.Xml;
using RimWorld;
using Verse;

namespace TeleCore;

public struct TickTime
{
    private int _ticks;

    public float Hours => _ticks / (float)GenDate.TicksPerHour;
    public float Days => _ticks / (float)GenDate.TicksPerDay;
    
    public int TotalTicks => _ticks;

    public TickTime(int ticks)
    {
        _ticks = ticks;
    }

    private void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        var value = xmlRoot.Value;
        
        if (value.EndsWith("h"))
        {
            _ticks = (int)float.Parse(value.Substring(0, value.Length - 1)) * GenDate.TicksPerHour;
        }
        else if (value.EndsWith("d"))
        {
            _ticks = (int)float.Parse(value.Substring(0, value.Length - 1)) * GenDate.TicksPerDay;
        }

        if (int.TryParse(value, out var ticksVal))
        {
            _ticks = ticksVal;
        }
    }

    public override string ToString()
    {
        return _ticks.ToStringTicksToPeriodVerbose(true, false);
    }
}