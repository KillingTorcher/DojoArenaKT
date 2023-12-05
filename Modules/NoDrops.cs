using HarmonyLib;
using ProjectM.Network;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using ProjectM.Gameplay.Systems;

namespace DojoArenaKT;

[HarmonyPatch(typeof(DropItemSystem), nameof(DropItemSystem.OnUpdate))]
public static class DropItemSystemHook
{
    [HarmonyPrefix]
    private static void Prefix(DropItemSystem __instance)
    {
        var entityManager = __instance.EntityManager;
        var entities = __instance.__DropItemsJob_entityQuery.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in entities)
        {
            
            var dropEvent = entity.GetData<DropItemAtSlotEvent>();
            var fromCharacter = entity.GetData<FromCharacter>();
            InventoryUtilities.TryGetItemAtSlot(entityManager, fromCharacter.Character, dropEvent.SlotIndex, out var item);
            InventoryUtilitiesServer.TryRemoveItemAtIndex(entityManager, fromCharacter.Character, item.ItemType, item.Amount, dropEvent.SlotIndex, true);
            entity.Destroy();
            return;
        }

        entities = __instance.__DropEquippedItemJob_entityQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var dropEvent = entity.GetData<DropEquippedItemEvent>();

            var fromCharacter = entity.GetData<FromCharacter>();
            var equipment = fromCharacter.Character.GetData<Equipment>();
            InventoryUtilitiesServer.TryRemoveItem(G.EntityManager, fromCharacter.Character, equipment.GetEquipmentItemId(dropEvent.EquipmentType), 1);
            entity.Destroy();
            return;
        }
    }
}

[HarmonyPatch(typeof(DropItemThrowSystem), nameof(DropItemThrowSystem.OnUpdate))]
public static class DropItemThrowSystem_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(DropItemThrowSystem __instance)
    {
        var entities = __instance.__CreateDropItemThrowsJob_entityQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            try
            {
                entity.Destroy();
            }
            catch (Exception ex)
            {
                Output.LogError("DropItemThrowSystem", ex);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(OnDeathSystem), nameof(OnDeathSystem.DropInventoryOnDeath))]
public static class DropInventoryOnDeath
{
    [HarmonyPrefix]
    public static bool Prefix(OnDeathSystem __instance)
    {
        return false;
    }
}