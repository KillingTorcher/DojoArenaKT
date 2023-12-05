using HarmonyLib;
using ProjectM;
using System;
using Unity.Collections;
using Unity.Entities;

namespace DojoArenaKT;

[HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
public class DeathEventListener // Handles when players fully die, respawning them wherever they were.
{
    [HarmonyPostfix]
    public static void Postfix(DeathEventListenerSystem __instance)
    {
        NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);
        foreach (DeathEvent ev in deathEvents)
        {
            try
            {
                Player deadPlayer = Player.FromCharacter(ev.Died);
                if (!deadPlayer.IsValid) continue;

                deadPlayer.Respawn();
            }
            catch (Exception ex) { Output.LogError("DeathEventListenerPostfix", ex); }
        }
    }
}

[HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
public class VampireDownedServerEventSystem_Patch // Handles when vampires are downed, actually unsure if this is necessary!
{
    public static void Postfix(VampireDownedServerEventSystem __instance)
    {
        EntityManager em = __instance.EntityManager;
        var EventsQuery = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in EventsQuery)
        {
            try
            {
                if (!VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, em, out var downedEntity)) continue;
                Player downedPlayer = Player.FromCharacter(downedEntity);
                if (!downedPlayer.IsValid) continue;

                //downedPlayer.Respawn(); // May be redundant!
            }
            catch (Exception ex)
            {
                Output.LogError("VampireDownedServerEvent", ex, entity.ToString());
            }
        }
    }
}