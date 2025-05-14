using System;
using System.Globalization;
using System.Xml;
using RimWorld;
using Verse;

namespace TeleCore;

public struct TickTime
{
    public float Hours => TotalTicks / (float) GenDate.TicksPerHour;
    public float Days => TotalTicks / (float) GenDate.TicksPerDay;

    public int TotalTicks { get; private set; }

    public TickTime(int ticks)
    {
        TotalTicks = ticks;
    }

    private void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        string value = xmlRoot.InnerText?.Trim();

        if (string.IsNullOrEmpty(value))
        {
            Log.Error("TickTime could not load: value is null or empty.");
            TotalTicks = 0;
            return;
        }

        try
        {
            if (value.EndsWith("h"))
            {
                string numberOnly = value.Substring(0, value.Length - 1);
                TotalTicks = (int)(float.Parse(numberOnly, CultureInfo.InvariantCulture) * 2500f);
            }
            else if (value.EndsWith("d"))
            {
                string numberOnly = value.Substring(0, value.Length - 1);
                TotalTicks = (int)(float.Parse(numberOnly, CultureInfo.InvariantCulture) * 60000f);
            }
            else if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var ticksVal))
            {
                TotalTicks = ticksVal;
            }
            else
            {
                Log.Error($"TickTime could not parse value: '{value}'");
                TotalTicks = 0;
            }
        }
        catch (Exception e)
        {
            Log.Error($"TickTime parsing failed: '{value}' → {e}");
            TotalTicks = 0;
        }
    }



    public override string ToString()
    {
        return TotalTicks.ToStringTicksToPeriodVerbose(true, false);
    }
}