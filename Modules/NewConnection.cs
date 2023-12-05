using HarmonyLib;
using ProjectM.Network;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ProjectM.Tiles.TileMapCollisionMath;
using Unity.Entities;
using Unity.Collections;
using Stunlock.Network;
using static ProjectM.ServerBootstrapSystem;
using Unity.Mathematics;
using Bloodstone.API;
using ProjectM.Gameplay.Systems;
using static ProjectM.ServantCoffinstationUpdateSystem;

namespace DojoArenaKT;

[HarmonyPatch(typeof(SpawnCharacterSystem), nameof(SpawnCharacterSystem.OnUpdate))]
public static class SpawnCharacterPatch
{
    public static HashSet<ulong> FirstTimeTeleport = new();
    static bool errorOccured = false;

    [HarmonyPrefix]
    public static void Postfix(SpawnCharacterSystem __instance)
    {
        var query = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
        foreach (var ent in query)
        {
            try
            {
                var spawnData = ent.GetData<SpawnCharacter>();
                if (!spawnData.HasSpawned) continue;
                
                var steamID = spawnData.User.GetData<User>().PlatformId;
                if (!FirstTimeTeleport.Contains(steamID)) continue;

                FirstTimeTeleport.Remove(steamID);
                spawnData.PostSpawn_Character.Teleport(G.SpawnLocation);

                Player player = Player.FromCharacter(spawnData.PostSpawn_Character);
                KitCommand.Initialize(player, "");
            }
            catch (Exception ex)
            {
                if (!errorOccured)
                {
                    errorOccured = true;
                    Output.LogError("NewCharacterSpawnHack", ex, "Suppressed further errors from this source, as they could be spammy if not.");
                }
            }

        }
    }
}

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
public static class OnUserConnected_Patch
{
    public static void Postfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        User TmpUser = new();
        bool IsNewVampire = false;

        try
        {
            var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
            var serverClient = __instance._ApprovedUsersLookup[userIndex];
            var userEntity = serverClient.UserEntity;
            var userData = __instance.EntityManager.GetComponentData<User>(userEntity);
            var playerEntity = userData.LocalCharacter.GetEntityOnServer();
            ulong playerPlatformID = userData.PlatformId;


            bool isReturningPlayer = userData.CharacterName.IsEmpty;
            IsNewVampire = isReturningPlayer;
            TmpUser = userData;

            if (playerEntity.HasData<UnitStats>())
            {
                var comp = playerEntity.GetData<UnitStats>();
                comp.PvPProtected.SetBaseValue(false, G.EntityManager.GetBuffer<BoolModificationBuffer>(playerEntity));
                playerEntity.SetData<UnitStats>(comp);
            }
            else
            {
                SpawnCharacterPatch.FirstTimeTeleport.Add(playerPlatformID);
            }

            var adminSystem = G.AdminSystem;
            if (adminSystem._LocalAdminList.Contains(playerPlatformID))
            {
                Output.Log($"Authenticated {playerPlatformID} as admin.", BepInEx.Logging.LogLevel.Warning, "AdminLog");
                var ent = G.EntityManager.CreateEntity(new[] { ComponentType.ReadWrite<FromCharacter>(), ComponentType.ReadWrite<AdminAuthEvent>() });
                ent.SetData<FromCharacter>(new()
                {
                    Character = playerEntity,
                    User = userEntity
                });
            }
            else
            {
                userData.IsAdmin = false;
                userEntity.SetData(userData);
            }
        }
        catch (Exception ex) { Output.Log(ex.Message); };
        try
        {
            if (!IsNewVampire)
            {
                var playerEntity = TmpUser.LocalCharacter._Entity;
                playerEntity.Teleport(G.SpawnLocation);
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
public static class OnUserDisconnected_Patch
{
    private static void Prefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId, ConnectionStatusChangeReason connectionStatusReason, string extraData)
    {
        try
        {
             if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out int userIndex)) return;
            var serverClient = __instance._ApprovedUsersLookup[userIndex];
            var userData = __instance.EntityManager.GetComponentData<User>(serverClient.UserEntity);
            if(userData.CharacterName.IsEmpty) return; // New Vampire
            if ((new ConnectionStatusChangeReason[] { ConnectionStatusChangeReason.IncorrectPassword, ConnectionStatusChangeReason.Unknown, ConnectionStatusChangeReason.NoFreeSlots, ConnectionStatusChangeReason.Banned }).Contains(connectionStatusReason)) return;

            // -- new void (hot & cold storage) x = -2375, y = -1900
            userData.LocalCharacter._Entity.Teleport(new Unity.Mathematics.float3(-2375, 0, -1900));
        }
        catch (Exception ex)
        {
            string steamID = "Unknown";

            try
            {
                var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
                var serverClient = __instance._ApprovedUsersLookup[userIndex];
                var userData = __instance.EntityManager.GetComponentData<User>(serverClient.UserEntity);
                steamID = userData.PlatformId.ToString();
            } catch{ }

            Output.LogError("UserDisconnectException", ex, $"SteamID={steamID}");
        }
    }
}