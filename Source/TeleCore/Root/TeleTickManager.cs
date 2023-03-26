using System;
using System.Diagnostics;
using UnityEngine;
using Verse;

namespace TeleCore;

public class TeleTickManager
{
    private Stopwatch clock = new Stopwatch();

    private float realTimeToTickThrough;
    private bool isPaused = false;

    private const float _FPSLimiter = 45.4545441f;
        
    private Action UITickers;
    private Action GameUITickers;
    private Action GameTickers;

    private int internalTicks;
    private int internalMapTicks;

    public bool Paused => isPaused;

    public static bool GameActive => Current.Game != null && Current.ProgramState == ProgramState.Playing;
    public bool GamePaused => !GameActive || Find.TickManager.Paused;

    public int CurrentTick => internalTicks;
    public int CurrentMapTick => internalMapTicks;
        
    private float ReusedTickRateMultiplier
    {
        get
        {
            if (!GameActive) return 0;
            return Find.TickManager?.TickRateMultiplier ?? 0;
        }
    }

    private float CurTimePerTick => 0.016666668F;

    public TeleTickManager()
    {
        TLog.Message("Starting TeleTickManager!");
    }
        
    public void Update()
    {
        UpdateTick();
            
        //
        if (!GameActive) return;
        for (var i = 0; i < Current.Game.maps.Count; i++)
        {
            var map = Current.Game.maps[i].TeleCore();
            UpdateMapTick(map);
            UpdateDrawMap(map);
        }
    }

    private int ticksThisFrame = 0;
        
    //TODO: Check why it ticks 4 times
    private void UpdateMapTick(MapComponent_TeleCore map)
    {
        ticksThisFrame = 0;
        if (!Paused)
        {
            var curTimePerTick = Find.TickManager.CurTimePerTick;
            if (Mathf.Abs(Time.deltaTime - curTimePerTick) < curTimePerTick * 0.1f)
                realTimeToTickThrough += curTimePerTick;
            else
                realTimeToTickThrough += Time.deltaTime;
            var tickRateMultiplier = Find.TickManager.TickRateMultiplier;
            clock.Reset();
            clock.Start();
            while (realTimeToTickThrough > 0f && ticksThisFrame < tickRateMultiplier * 2f)
            {
                map.TeleMapSingleTick();
                realTimeToTickThrough -= curTimePerTick;
                ticksThisFrame++;
                internalMapTicks++;
                if (Paused || clock.ElapsedMilliseconds > 45.454544f) break;
            }

            if (realTimeToTickThrough > 0f) realTimeToTickThrough = 0f;
        }
        /*
        var timePerMapTick = Find.TickManager.CurTimePerTick;
        //
        if (Mathf.Abs(Time.deltaTime - timePerMapTick) < timePerMapTick * 0.1f)
            realTimeToTickThrough += timePerMapTick;
        else
            realTimeToTickThrough += Time.deltaTime;

        var tickRate = Find.TickManager.TickRateMultiplier;
        ticksThisFrame = 0;
        clock.Reset();
        clock.Start();
        while (realTimeToTickThrough > 0f && ticksThisFrame < tickRate * 2f)
        {
            map.TeleMapSingleTick();
            
            //
            realTimeToTickThrough -= timePerMapTick;
            ticksThisFrame++;
            internalMapTicks++;
            if (Find.TickManager.Paused || clock.ElapsedMilliseconds > 45.4545441f)
            {
                break;
            }
        }
        
        //
        realTimeToTickThrough = 0;
        */
    }
        
    private int ticksThisFrameNormal = 0;
    private void UpdateTick()
    {
        if (Paused) return;
            
        //
        float curTimePerTick = CurTimePerTick;
        if (Mathf.Abs(Time.deltaTime - curTimePerTick) < curTimePerTick * 0.1f)
        {
            realTimeToTickThrough += curTimePerTick;
        }
        else
        {
            realTimeToTickThrough += Time.deltaTime;
        }

        ticksThisFrameNormal = 0;
        clock.Reset();
        clock.Start();
        while (realTimeToTickThrough > 0f && ticksThisFrameNormal < 2)
        {
            if (!GamePaused)
            {
                GameTickers?.Invoke();
            }
                
            UITickers?.Invoke();
            GameUITickers?.Invoke();

            //
            realTimeToTickThrough -= curTimePerTick;
            ticksThisFrameNormal++;
            internalTicks++;
                
            if (Paused || clock.ElapsedMilliseconds > _FPSLimiter)
            {
                break;
            }
        }

        //
        realTimeToTickThrough = 0;
    }

    private void UpdateDrawMap(MapComponent_TeleCore map)
    {
        map?.TeleMapUpdate();
    }

    public void ClearGameTickers()
    {
        GameTickers = null;
        GameUITickers = null;
    }

    public void TogglePlay()
    {
        isPaused = !isPaused;
    }

    public void RegisterUITickAction(Action action)
    {
        UITickers += action;
    }

    public void RegisterMapUITickAction(Action action)
    {
        GameUITickers += action;
    }

    public void RegisterMapTickAction(Action action)
    {
        GameTickers += action;
    }
}