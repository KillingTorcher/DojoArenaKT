using Unity.Entities;
using ProjectM;
using Unity.Transforms;
using Unity.Mathematics;
using System.Linq;
using ProjectM.Network;
using System.Collections.Generic;
using System;
using Bloodstone.API;
using Unity.Collections;

namespace DojoArenaKT;
public class Player
{
    public static Dictionary<ulong, Player> List = new();
    public static Player Invalid = new();

    public ulong SteamID64 { get; private set; }
    public bool IsValid = false;
    public string InvalidReason = "Player is invalid";
    public Entity Character;
    public Entity UserEntity;
    public User User
    {
        get
        {
            return UserEntity.GetData<User>();
        }
    }

    public bool IsAdmin
    {
        get
        {
            return User.IsAdmin;
        }
    }
    public string LongName
    {
        get
        {
            return $"[{SteamID64}] {CharacterData.Name}";
        }
    }
    public string Name
    {
        get
        {
            return CharacterData.Name.ToString();;
        }
    }

    public PlayerCharacter CharacterData
    {
        get
        {
            return Character.GetData<PlayerCharacter>();
        }
    }

    public bool CheckValidity() // Proper validity check
    {
        return IsValid && G.EntityManager.Exists(Character) && G.EntityManager.Exists(UserEntity) && User.PlatformId == SteamID64;
    }

    public static Player FromUserEntity(Entity userEntity)
    {
        return Player.FromUser(userEntity.GetData<User>());
    }

    public static Player FromUser(User user)
    {
        return Player.FromCharacter(user.LocalCharacter._Entity);
    }
    public static Player FromCharacter(Entity characterEntity)
    {
        if (!G.EntityManager.Exists(characterEntity)) return Player.Invalid;
        if (!characterEntity.HasData<PlayerCharacter>()) return Player.Invalid;
        var UserEntity = characterEntity.GetData<PlayerCharacter>().UserEntity;
        var SteamID = UserEntity.GetData<User>().PlatformId;

        if (List.ContainsKey(SteamID))
        {
            var cached = List[SteamID];
            if (!cached.CheckValidity()) cached.IsValid = false;
            else return cached;
        } 

        Player newPlayer = new Player();
        newPlayer.SteamID64 = SteamID;
        newPlayer.Character = characterEntity;
        newPlayer.UserEntity = UserEntity;
        newPlayer.IsValid = true;
        List[SteamID] = newPlayer;

        return newPlayer;
    }

    private static Il2CppSystem.Reflection.MethodInfo RespawnMethod
    {
        get
        {
            if (_respawnMethod == null) _respawnMethod = G.BootstrapSystem.GetIl2CppType().GetMethods().First<Il2CppSystem.Reflection.MethodInfo>(x => x.Name.Contains("RespawnCharacter") && x.GetParametersCount() == 6);
            return _respawnMethod;
        }
    }
    private static Il2CppSystem.Reflection.MethodInfo _respawnMethod = null;
    public void Respawn()
    {
        Respawn(Character.GetData<LocalToWorld>().Position);
    }

    public void Respawn(float3 spawnLocation)
    {
        EntityCommandBuffer commandBufferSafe = G.BufferSystem.CreateCommandBuffer();
        var userEntityBoxed = UserEntity.BoxIl2CppObject();
        
        RespawnMethod.Invoke(G.BootstrapSystem, new Il2CppSystem.Object[] {
            commandBufferSafe.BoxIl2CppObject(), userEntityBoxed, new Il2CppSystem.Nullable<float3>(spawnLocation), Character.BoxIl2CppObject(), userEntityBoxed, -1
        });

        FullyRestore();
    }

    public void Message(object message)
    {
        Output.SendMessage(this, message);
    }

    public T GetData<T>() where T: new()
    {
        return Character.GetData<T>();
    }

    public void SetData<T>(T component) where T: new()
    {
        Character.SetData(component);
    }

    public bool RemoveItem(PrefabGUID prefabGUID, int amount)
    {
        return InventoryUtilitiesServer.TryRemoveItem(G.EntityManager, Character, prefabGUID, amount);
    }

    public List<(PrefabGUID, NetworkedEntity)> GetEquipment()
    {
        List<(PrefabGUID, NetworkedEntity)> output = new();

        try
        {
            Equipment eq = Character.GetData<Equipment>();
  
            if (eq.ArmorChestSlotId.GuidHash != 0) output.Add((eq.ArmorChestSlotId, eq.ArmorChestSlotEntity));
            if (eq.ArmorHeadgearSlotId.GuidHash != 0) output.Add((eq.ArmorHeadgearSlotId, eq.ArmorHeadgearSlotEntity));
            if (eq.ArmorLegsSlotId.GuidHash != 0) output.Add((eq.ArmorLegsSlotId, eq.ArmorLegsSlotEntity));
            if (eq.ArmorFootgearSlotId.GuidHash != 0) output.Add((eq.ArmorFootgearSlotId, eq.ArmorFootgearSlotEntity));
            if (eq.ArmorGlovesSlotId.GuidHash != 0) output.Add((eq.ArmorGlovesSlotId, eq.ArmorGlovesSlotEntity));
            if (eq.CloakSlotId.GuidHash != 0) output.Add((eq.CloakSlotId, eq.CloakSlotEntity));
            if (eq.GrimoireSlotId.GuidHash != 0) output.Add((eq.GrimoireSlotId, eq.GrimoireSlotEntity));
            if (eq.WeaponSlotId.GuidHash != 0) output.Add((eq.WeaponSlotId, eq.WeaponSlotEntity));
        }
        catch (Exception ex)
        {
            Output.LogError("GetPlayerEquipment", ex, $"SteamID={SteamID64}, Name={Name}");
        }

        return output;
    }

    public int CountItems(PrefabGUID item)
    {
        int count = 0;

        foreach (InventoryBuffer itemBuffer in GetInventory())
        {
            if (itemBuffer.ItemType == item) count += itemBuffer.Amount;
        }

        return count;
    }

    public DynamicBuffer<InventoryBuffer> GetInventory()
    {
        try
        {
            /// I'm guessing the players inventory is always located at index 0
            var inventoryInstanceElement = G.EntityManager.GetBuffer<InventoryInstanceElement>(Character);
            var inventoryBuffer = G.EntityManager.GetBuffer<InventoryBuffer>(inventoryInstanceElement[0].ExternalInventoryEntity._Entity);
            return inventoryBuffer;
        }
        catch { }

        return new DynamicBuffer<InventoryBuffer>();
    }

    public void GiveEquipItem(PrefabGUID prefabGUID)
    {
        GiveDebugEvent giveDebugEvent = new GiveDebugEvent()
        {
            Amount = 1,
            PrefabGuid = prefabGUID
        };

        G.DebugSystem.GiveEvent(User.Index, ref giveDebugEvent);
    }

    public AddItemResponse GiveItem(PrefabGUID itemGUID, int amount)
    {
        AddItemSettings addItemSettings = new AddItemSettings()
        {
            ItemDataMap = G.GameData.ItemHashLookupMap,
            EntityManager = G.EntityManager,
            DropRemainder = false,
            EquipIfPossible = false,
            OnlyCheckOneSlot = false,
            OnlyFillEmptySlots = false,
        };

        return InventoryUtilitiesServer.TryAddItem(addItemSettings, Character, itemGUID, amount);
    }

    public AddItemResponse GiveItemAtSlot(PrefabGUID itemGUID, int amount, int slotID)
    {
        AddItemSettings addItemSettings = new AddItemSettings()
        {
            ItemDataMap = G.GameData.ItemHashLookupMap,
            EntityManager = G.EntityManager,
            DropRemainder = false,
            EquipIfPossible = false,
            OnlyCheckOneSlot = false,
            OnlyFillEmptySlots = false,
            StartIndex = new() { has_value = true, value = slotID}
        };

        return InventoryUtilitiesServer.TryAddItem(addItemSettings, Character, itemGUID, amount);
    }

    public void FullyRestore()
    {
        SetHealth(100f);
        ResetCooldowns();
        RemoveBuff(Database.BuffGUID.InCombat_PvP);
    }

    public void SetHealth(float healthPercentage, float maxRecoveryHealth = 100f)
    {
        try {
            healthPercentage = Math.Clamp(healthPercentage, 0f, 100f);
            maxRecoveryHealth = Math.Clamp(maxRecoveryHealth, 0f, 100f);
            var healthComp = Character.GetData<Health>();
            healthComp.Value = healthPercentage / 100f * healthComp.MaxHealth;
            healthComp.MaxRecoveryHealth = maxRecoveryHealth / 100f * healthComp.MaxHealth;
            Character.SetData<Health>(healthComp);
        } catch { }
    }

    public void ResetCooldowns()
    {
        var AbilityBuffer = G.EntityManager.GetBuffer<AbilityGroupSlotBuffer>(Character);
        for (int i = 0; i < AbilityBuffer.Length; i++)
        {
            var AbilitySlot = AbilityBuffer[i].GroupSlotEntity._Entity;
            var ActiveAbility = G.EntityManager.GetComponentData<AbilityGroupSlot>(AbilitySlot);
            var ActiveAbility_Entity = ActiveAbility.StateEntity._Entity;

            var b = ActiveAbility_Entity.GetPrefabGUID();
            if (b.GuidHash == 0) continue;

            var AbilityStateBuffer = G.EntityManager.GetBuffer<AbilityStateBuffer>(ActiveAbility_Entity);
            for (int c_i = 0; c_i < AbilityStateBuffer.Length; c_i++)
            {
                var abilityState = AbilityStateBuffer[c_i].StateEntity._Entity;
                var abilityCooldownState = G.EntityManager.GetComponentData<AbilityCooldownState>(abilityState);
                abilityCooldownState.CooldownEndTime = 0;
                G.EntityManager.SetComponentData(abilityState, abilityCooldownState);
            }
        }

    }

    public void RemoveBuff(PrefabGUID buffGUID)
    {
        if (BuffUtility.TryGetBuff(G.EntityManager, Character, buffGUID, out Entity ent))
        {
            ProjectM.Shared.DestroyUtility.CreateDestroyEvent(G.EntityManager, ent, DestroyReason.Default, DestroyDebugReason.TryRemoveBuff);
        }
    }

    public static Player FromName(string searchName, bool requiresOnline = true)
    {
        string searchNameLower = searchName.ToLower();
        int matches = 0;

        Player notFound = new();
        Player found = notFound;

        foreach (var iterUserEntity in G.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp))
        {
            var iterUser = iterUserEntity.GetData<User>();
            if (requiresOnline && !iterUser.IsConnected) continue;
            string charName = iterUser.CharacterName.ToString().ToLower();
            if (charName.Contains(searchNameLower))
            {
                matches++;
                found = Player.FromCharacter(iterUser.LocalCharacter._Entity);
                if (charName == searchNameLower) break;
            }
        }

        if (matches == 0)
        {
            notFound.InvalidReason = "Specified player not found";
            return notFound;
        }
        if (matches > 1)
        {
            notFound.InvalidReason = "Multiple players matching criteria found";
            return notFound;
        }
        return found;
    }

    public void Kick()
    {
        var userData = User;
        int index = userData.Index;
        NetworkId id = UserEntity.GetData<NetworkId>();

        var eventEntity = G.EntityManager.CreateEntity(
            ComponentType.ReadOnly<NetworkEventType>(),
            ComponentType.ReadOnly<SendEventToUser>(),
            ComponentType.ReadOnly<KickEvent>()
        );

        var KickEvent = new KickEvent()
        {
            PlatformId = userData.PlatformId
        };

        eventEntity.SetData<SendEventToUser>(new()
        {
            UserIndex = index
        });

        eventEntity.SetData<NetworkEventType>(new()
        {
            EventId = NetworkEvents.EventId_KickEvent,
            IsAdminEvent = false,
            IsDebugEvent = false
        });

        eventEntity.SetData<KickEvent>(KickEvent);
    }

    public bool CanTarget(Player target)
    {
        return true;
    }

    public void Ban()
    {
        var kbs = G.World.GetExistingSystem<KickBanSystem_Server>();
        kbs._RemoteBanList.Add(SteamID64);
        kbs._LocalBanList.Add(SteamID64);

        var userData = User;
        int index = userData.Index;

        var banEventEntity = G.EntityManager.CreateEntity(
            ComponentType.ReadOnly<NetworkEventType>(),
            ComponentType.ReadOnly<SendEventToUser>(),
            ComponentType.ReadOnly<BanEvent>()
        );

        var banEvent = new BanEvent()
        {
            PlatformId = userData.PlatformId,
            Unban = false,
        };

        banEventEntity.SetData<SendEventToUser>(new()
        {
            UserIndex = index
        });

        banEventEntity.SetData<NetworkEventType>(new()
        {
            EventId = NetworkEvents.EventId_KickEvent,
            IsAdminEvent = false,
            IsDebugEvent = false
        });

        banEventEntity.SetData(banEvent);
        kbs._RemoteBanList.Save();
        kbs._LocalBanList.Save();
    }

    public void Unban()
    {
        var userData = User;
        int index = userData.Index;

        var kbs = G.World.GetExistingSystem<KickBanSystem_Server>();
        kbs._RemoteBanList.Remove(SteamID64);
        kbs._LocalBanList.Remove(SteamID64);

        var entity = G.EntityManager.CreateEntity(
            ComponentType.ReadOnly<NetworkEventType>(),
            ComponentType.ReadOnly<SendEventToUser>(),
            ComponentType.ReadOnly<BanEvent>()
        );

        var banEvent = new BanEvent()
        {
            PlatformId = userData.PlatformId,
            Unban = true,
        };

        entity.SetData<SendEventToUser>(new()
        {
            UserIndex = index
        });

        entity.SetData<NetworkEventType>(new()
        {
            EventId = NetworkEvents.EventId_KickEvent,
            IsAdminEvent = false,
            IsDebugEvent = false
        });

        entity.SetData(banEvent);
        kbs._RemoteBanList.Save();
        kbs._LocalBanList.Save();
    }
}