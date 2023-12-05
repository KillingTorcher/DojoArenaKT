using HarmonyLib;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DojoArenaKT;

[HarmonyPatch(typeof(SunSystem), nameof(SunSystem.OnUpdate))]
public static class Time
{
    public static Dictionary<string, Timer> ActiveTimers = new();
    public static List<Timer> AnonymousTimers = new();
    static bool FirstTick = true;
    public static Stopwatch internalClock {get; private set; } = new();
    public static TimeSpan Elapsed
    {
        get
        {
            return internalClock.Elapsed;
        }
    }

    [HarmonyPrefix]
    private static void Prefix(SunSystem __instance)
    {
        if (FirstTick)
        {
            internalClock.Start();
            Event.Fire(Events.Load);
            FirstTick = false;
            return;
        }
        if (!internalClock.IsRunning) return;

        HandleTimers();
    }
    private static void HandleTimers()
    {
        List<string> toBeDestroyed = new();
        
        foreach ((string identifier, Timer runningTimer) in ActiveTimers)
        {
            try
            {
                runningTimer.Tick();
                if (runningTimer.RepetitionsLeft == 0) toBeDestroyed.Add(identifier);
            }
            catch (Exception ex)
            {
                if (runningTimer.DestroyOnError) toBeDestroyed.Add(identifier);
                Output.LogError("TimerTickError-" + runningTimer.Identifier, ex, $"DestroyOnError={runningTimer.DestroyOnError}; interval={runningTimer.Interval.TotalMilliseconds}ms; RepetitionsLeft={runningTimer.RepetitionsLeft}");
            }
        }

        foreach (string identifier in toBeDestroyed)
        {
            Timer.Destroy(identifier);
        }

        foreach (Timer anonTimer in AnonymousTimers)
        {
            try
            {
                anonTimer.Tick();
            }
            catch (Exception ex)
            {
                Output.LogError("TimerTickError-" + anonTimer.Identifier, ex, $"DestroyOnError={anonTimer.DestroyOnError}; interval={anonTimer.Interval.TotalMilliseconds}ms; RepetitionsLeft={anonTimer.RepetitionsLeft}");
                if (anonTimer.DestroyOnError) anonTimer.RepetitionsLeft = 0;
            }
        }

        AnonymousTimers.RemoveAll(x => x.RepetitionsLeft == 0);
    }
}

public class Timer
{
    public TimeSpan Interval;
    public Action OnTick {get; private set; }
    public string Identifier { get; private set; }
    public int RepetitionsLeft;
    public bool DestroyOnError = true;

    private static int AnonymousCounter = 1;
    private TimeSpan NextTick;

    public static Timer Create(string identifier, TimeSpan interval, Action onTick, int repetitions=-1)
    {
        Timer newTimer = new();
        newTimer.Identifier = identifier;
        newTimer.Interval = interval;
        newTimer.OnTick = onTick;
        newTimer.RepetitionsLeft = repetitions;
        newTimer.NextTick = Time.Elapsed + newTimer.Interval;

        Timer.Destroy(identifier);
        Time.ActiveTimers.Add(identifier, newTimer);

        return newTimer;
    }
    public static Timer Create(string identifier, double intervalInSeconds, Action onTick, int repetitions=-1)
    {
        return Create(identifier, TimeSpan.FromSeconds(intervalInSeconds), onTick, repetitions);
    }
    public static Timer CreateAnonymous(TimeSpan interval, Action onTick, int repetitions=-1)
    {
        Timer newTimer = new();
        newTimer.Identifier = "AnonymousTimer" + AnonymousCounter.ToString();
        newTimer.Interval = interval;
        newTimer.OnTick = onTick;
        newTimer.RepetitionsLeft = repetitions;
        newTimer.NextTick = Time.Elapsed + newTimer.Interval;

        Time.AnonymousTimers.Add(newTimer);
        AnonymousCounter++;

        return newTimer;
    }

    public void Tick()
    {
        if (RepetitionsLeft == 0) return;

        if (Time.Elapsed >= NextTick)
        {
            
            NextTick += Interval;
            if (RepetitionsLeft > 0) RepetitionsLeft--;
            OnTick();
        }
    }

    public static void Destroy(string identifier)
    {
        if (Time.ActiveTimers.ContainsKey(identifier)) Time.ActiveTimers.Remove(identifier);
    }
}

public static class Delay
{
    public static void Action(TimeSpan delay, Action action)
    {
        Timer.CreateAnonymous(delay, action, 1);
    }
    public static void Action(double delayInSeconds, Action action)
    {
        Action(TimeSpan.FromSeconds(delayInSeconds), action);
    }
}

