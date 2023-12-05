using HarmonyLib;
using ProjectM.Network;
using ProjectM;
using System;
using Unity.Collections;

namespace DojoArenaKT;

[HarmonyPatch(typeof(PlaceTileModelSystem), nameof(PlaceTileModelSystem.OnUpdate))]
public static class BuildingPermissions
{
    [HarmonyPrefix]
    public static bool Prefix(PlaceTileModelSystem __instance)
    {
        try
        {
            var dismantleEntityQuery = __instance._DismantleTileQuery.ToEntityArray(Allocator.Temp);
            if (dismantleEntityQuery.Length > 0)
            {
                foreach (var eventEntity in dismantleEntityQuery)
                {
                    Player user = Player.FromCharacter(eventEntity.GetData<FromCharacter>().Character);
                    if (!user.IsAdmin)
                    {
                        user.Message($"{Color.Red}Non-admins are not allowed to dismantle buildings.");
                        eventEntity.Destroy();
                        return false;
                    }
                }
            }

            var buildEntityQuery = __instance._BuildTileQuery.ToEntityArray(Allocator.Temp);
            if (buildEntityQuery.Length > 0)
            {
                foreach (var eventEntity in buildEntityQuery)
                {
                    Player user = Player.FromCharacter(eventEntity.GetData<FromCharacter>().Character);
                    if (!user.IsAdmin)
                    {
                        user.Message($"{Color.Red}Non-admins are not allowed to build.");
                        eventEntity.Destroy();
                        return false;
                    }
                }
            }

            var moveEntitiesQuery = __instance._MoveTileQuery.ToEntityArray(Allocator.Temp);
            if (moveEntitiesQuery.Length > 0)
            {
                foreach (var eventEntity in moveEntitiesQuery)
                {
                    Player user = Player.FromCharacter(eventEntity.GetData<FromCharacter>().Character);
                    if (!user.IsAdmin)
                    {
                        user.Message($"{Color.Red}Non-admins are not allowed to move buildings.");
                        eventEntity.Destroy();
                        return false;
                    }
                }
            }

            var wallpaperQuery = __instance._BuildWallpaperQuery.ToEntityArray(Allocator.Temp);
            if (wallpaperQuery.Length > 0)
            {
                foreach (var eventEntity in wallpaperQuery)
                {
                    Player user = Player.FromCharacter(eventEntity.GetData<FromCharacter>().Character);
                    if (!user.IsAdmin)
                    {
                        user.Message($"{Color.Red}Non-admins are not allowed to change wallpapers.");
                        eventEntity.Destroy();
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Output.LogError("DismantleTileOverrides", ex);
        }
        return true;

    }
}