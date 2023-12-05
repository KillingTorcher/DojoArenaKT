using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Network;
using Unity.Mathematics;

namespace DojoArenaKT;
[HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.Start))]
public static class GameBootstrapStart
{
    [HarmonyPrefix]
    public static void Postfix()
    {
        Event.Fire(Events.BootstrapStart);
    }
}


[HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.OnApplicationQuit))]
public static class GameBootstrapQuit
{
    public static void Prefix()
    {
        Event.Fire(Events.BootstrapQuit);
    }
}

