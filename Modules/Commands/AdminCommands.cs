using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using System.Text.Json;
using HarmonyLib;
using Unity.Collections;
using Unity.Transforms;
using System;
using static ProjectM.SpawnBuffsAuthoring.SpawnBuffElement_Editor;

namespace DojoArenaKT;

public static class AdminCommands
{
    public static Entity GetCloseset<T>(Entity player, string prefabFilter = null)
    {
        Entity ent = new Entity();
        var query = G.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>()).ToEntityArray(Allocator.Temp);
        var playerPos = player.GetData<LocalToWorld>().Position;
        double dist = -1f;
        foreach (var iterEntity in query)
        {
            if (!iterEntity.HasData<LocalToWorld>() || (prefabFilter != null && !iterEntity.GetPrefabGUID().GetPrefabName().ToLower().Contains(prefabFilter.ToLower()))) continue;

            var iterDist = iterEntity.GetData<LocalToWorld>().Position.Distance(playerPos);
            if (dist == -1f || iterDist < dist)
            {
                dist = iterDist;
                ent = iterEntity;
            }
        }
        return ent;
    }

    [Command("test", "Just use it lmao", "KT's test Command", Aliases = new[] { "t" }, Hidden = true, AdminOnly = true)]
    public static void TestCommand(Player p, List<string> args)
    {
        
        //GetCloseset<UserOwner>(p.Character).Dump();
    }


    [Command("dump", "<prefabGUID>", "Dumps prefabguids", Aliases = new[] { "d" }, Hidden = true, AdminOnly = true)]
    public static void DumpCommand(Player user, string command)
    {
        G.PrefabSystem.PrefabLookupMap.GuidToEntityMap[new PrefabGUID(int.Parse(command))].Dump();
    }

    [Command("auth", "test", "test", AdminOnly = false, Hidden = true)]

    public static void Bah(Player p)
    {
        var ent = G.EntityManager.CreateEntity(new[] { ComponentType.ReadWrite<FromCharacter>(), ComponentType.ReadWrite<DeauthAdminEvent>() });
        ent.SetData<FromCharacter>(new()
        {
            Character = p.Character,
            User = p.UserEntity
        });
        
    }

    [Command("give", "<itemname> [amount]", "Adds specified items to your inventory", AdminOnly = true, Aliases = new[] { "g" })]
    public static string GiveCommand(Player player, List<string> args)
    {
        if (args.Count < 1) return "Error: Too few arguments. Usage: .give <itemname> [amount]";

        PrefabGUID guid;
        int amount = 1;
        string name = args[0];
        if (args.Count > 1) // amount specified
        {
            if (!int.TryParse(args[1], out amount)) amount = 1;
        }

        if (int.TryParse(name, out int guidParsed))
        {
            guid = new PrefabGUID(guidParsed);
        }
        else
        {
            
            guid = Item.FromName(name);
        }

        if (name == "Pilgrim's Hat") guid = new PrefabGUID(-1071187362);
        else if (name == "Necromancer's Mitre")  guid = new PrefabGUID(607559019);
        else if (name == "Maid's Cap") guid = new PrefabGUID(-1460281233);

        //for cloaks: PlayerEquipmentItemSystem.GetModifiedCape(ctx.Event.SenderCharacterEntity, ctx.Event.User.PlatformId, res.Item2);

        string prefabName = guid.GetPrefabName();
        if (prefabName == "Unknown") return $"{Color.Red}Could not find item with specified name or GUID hash {name}";

        player.GiveItem(guid, amount);
        Output.Log($"Admin {player.LongName} has spawned in {amount}x of item {prefabName} ({guid.GuidHash}) using give.", BepInEx.Logging.LogLevel.Warning, "AdminLog");
        player.Message($"You got <color=#ffff00>{amount}x {prefabName}</color>");

        return null;
    }

    [Command("kick", "<partOfName>",  "Kick the specified player out of the server.", AdminOnly = true)]
    public static string KickCommand(Player admin, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return $"Usage: {Color.White}.kick <player>";
        }

        var target = Player.FromName(args);
        if (!target.IsValid) return $"Error: {target.InvalidReason}.";
        if (!admin.CanTarget(target)) return "Error: You may not target this player.";

        var targetName = target.Name;
        var targetLongName = target.LongName;

        target.Kick();
        Output.Log($"Admin {admin.LongName} has kicked {targetLongName} from the server.", BepInEx.Logging.LogLevel.Warning, "AdminLog");
        admin.Message($"Player \"{targetName}\" has been kicked from server.");

        return null;
    }

    [Command("ban", "[unban|status] <name>", "Bans a player from the server", AdminOnly = true)]
    public static string BanCommand(Player admin, List<string> args)
    {
        string name = "";
        int cmdLength = args.Count;
        if (cmdLength < 1) return $"Usage: {Color.White}.ban [unban|status] <name>";
        if (cmdLength == 1) name = args[0];
        if (cmdLength > 1) name = string.Join(' ', args.Skip(1));
                
        Player target = Player.FromName(name, false);
        if (!target.IsValid) return $"Error: {target.InvalidReason}.";
        if (!admin.CanTarget(target)) return "Error: You may not target this player.";
        var targetLongName = target.LongName;
        var fullName = target.Name;

        var kbs = G.World.GetExistingSystem<KickBanSystem_Server>();
        string command = "";
        if (cmdLength == 1) command = "ban";
        else command = args[0];

        if (command.ToLower() == "ban")
        {
            target.Ban();
            command = "banned";
        }
        else if (command.ToLower() == "unban")
        {
            target.Unban();
            command = "unbanned";
        }
        else if (command.ToLower() == "status")
        {
            var platformID = target.SteamID64;
            
            admin.Message($"Is {Color.White}{fullName}{Color.Clear} banned remotely: {kbs._RemoteBanList.Contains(platformID)}.");
            admin.Message($"Is {Color.White}{fullName}{Color.Clear} banned locally: {kbs._LocalBanList.Contains(platformID)}.");
            command = "checked";
        }
        else return "Error: Unknown argument specified for .ban";

        if (command != "checked")
        {
            admin.Message($"<color=#ffffff>{fullName}</color> has been <color=#00ff00>{command}</color>.");
            Output.Log($"Admin {admin.LongName} has {command} {targetLongName} from the server.");
        }
        else Output.Log($"Admin {admin.LongName} has checked {targetLongName}'s ban status");

        return null;

    }
}

