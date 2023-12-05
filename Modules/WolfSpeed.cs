using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using static ProjectM.SpawnBuffsAuthoring.SpawnBuffElement_Editor;
using Unity.Entities;

namespace DojoArenaKT;

[HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
public static class ModifyUnitStatBuffSystem_Spawn_Patch
{
    public static ModifyUnitStatBuff_DOTS SpeedBoost = new()
    {
        StatType = UnitStatType.MovementSpeed,
        Value = 10,
        ModificationType = ModificationType.Add,
        Id = ModificationId.NewId(0)
    };
    [HarmonyPrefix]
    public static void Prefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        #pragma warning disable CS8073
        if (__instance.__OnUpdate_LambdaJob0_entityQuery != null)
        {
            var entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var guidHash = entity.GetPrefabGUID().GuidHash;
                if (guidHash == -351718282 || guidHash == -1158884666)
                {
                    var Buffer = G.EntityManager.GetBuffer<ModifyUnitStatBuff_DOTS>(entity);
                    Buffer.Add(SpeedBoost);
                }
            }
        }
        #pragma warning restore CS8073
    }
}