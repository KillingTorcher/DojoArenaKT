using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Bloodstone.API;
using System;

namespace DojoArenaKT;

public class BuildConfig
{
    public const string PackageID = "DojoArenaKT";
    public const string Name = "DojoArenaKT";
    public const string Version = "indev1.13";
}

[BepInPlugin(BuildConfig.PackageID, BuildConfig.Name, BuildConfig.Version)]
[Reloadable]
public class Main : BasePlugin
{
    public static Main Plugin;
    public static Harmony harmony;
    public const string Folder = "BepInEx/Arena/";

    [EventSubscriber(Events.Load, 9001)] // It's OVER 9000!!! and fires after everything else :)
    public static void WelcomeMessage()
    {
        ConsoleManager.SetConsoleTitle("Welcome to The Dojo: Arena - by KT & Paps");
        ConsoleManager.ConsoleStream?.Write(System.Environment.NewLine);
        int i=0;
        ConsoleColor[] rainbow = new[] { ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Blue, ConsoleColor.Blue, ConsoleColor.Magenta };
        foreach (char c in Resources.GetString("WelcomeMessage"))
        {
            ConsoleManager.SetConsoleColor(rainbow[i]);
            ConsoleManager.ConsoleStream?.Write(c);
            ConsoleManager.SetConsoleColor(ConsoleColor.Gray);
            if (c.ToString() == Environment.NewLine) i=0;
            i++;
            if (i==rainbow.Length) i=0;
        }
    }
    
    public override void Load()
    {
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US", false);
        System.Diagnostics.Stopwatch sw = new();
        sw.Start();

        Plugin = this;
        Output.Logger = Log;

        harmony = new Harmony(BuildConfig.PackageID);
        harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

        G.SetupAttributedMethodList();
        Event.RegisterEvents();
        Event.Fire(Events.Preload);

        Harmony.VersionInfo(out var info);
        Output.Log($"Arena-{BuildConfig.Version}/Harmony{info.ToString()} loaded in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds.");
        Output.Log("Developed by Killing Torcher & Paps");
        sw.Stop();
    }

    public override bool Unload()
    {
        Event.Fire(Events.Unload);
        Config.Clear();
        harmony.UnpatchSelf();
        return true;
    }

    
    public static bool FirstTimeLoaded = false; // Whether this is the first time the plugin is loaded since server starts (not a reload)
    [EventSubscriber(Events.BootstrapStart)]
    public static void ServerStarted()
    {
        FirstTimeLoaded = true;
        Output.Log("Server has started.");
    }

    [EventSubscriber(Events.BootstrapQuit)]
    public static void GoodbyeMessage()
    {
        Time.internalClock.Stop();
        Output.Log($"Server shutting down gracefully after {Math.Floor(Time.internalClock.Elapsed.TotalSeconds)} seconds of operation!");
    }
}

