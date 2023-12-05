using ProjectM;
using Unity.Collections;
using Unity.Entities;
using Bloodstone.API;
using System;
using Newtonsoft.Json;
using Unity.Mathematics;
using ProjectM.Network;
using System.Linq;

using Il2CppSystem.Reflection;

namespace DojoArenaKT;
internal static class EntityExtensions
{
    public static bool HasData<T>(this Entity entity) where T: new()
    {
        return G.EntityManager.HasComponent<T>(entity);
    }
    public static T GetData<T>(this Entity entity) where T: new()
    {
        return G.EntityManager.GetComponentData<T>(entity);
    }

    public static void SetData<T>(this Entity entity, T component) where T: new()
    {
        VWorld.Server.EntityManager.SetComponentData(entity, component);
    }

    public static NativeArray<ComponentType> GetComponentTypes(this Entity entity)
    {
        return G.EntityManager.GetComponentTypes(entity);
    }
    public static string GetPrefabName(this PrefabGUID prefabGuid)
    {
        try
        {
            return VWorld.Server.GetExistingSystem<PrefabCollectionSystem>().PrefabGuidToNameDictionary[prefabGuid];
        }
        catch
        {
            return "Unknown";
        }
    }
    public static string GetPrefabName(this Entity entity)
    {
        try
        {
            return entity.GetData<PrefabGUID>().GetPrefabName();
        }
        catch
        {
            return "Unknown";
        }
    }

    public static PrefabGUID GetPrefabGUID(this Entity entity)
    {
        try
        {
            return entity.GetData<PrefabGUID>();
        }
        catch
        {
            return new PrefabGUID(0);
        }
    }
    public static Guid ToSystemGuid(this Il2CppSystem.Guid il2CppGuid)
    {
        return Guid.Parse(il2CppGuid.ToString());
    }


    public static void Dump(this Entity target)
    {
        try
        {
            Output.Log(target.GetPrefabName() + " [" + target.GetPrefabGUID().GuidHash + "]" + Environment.NewLine);
        } catch { }
        var componentTypes = target.GetComponentTypes();
        foreach (var compType in componentTypes)
        {
            try
            {
                Il2CppSystem.Reflection.MethodInfo methodInfo = G.EntityManager.BoxIl2CppObject().GetIl2CppType().GetMethod("GetComponentData");
                Il2CppSystem.Reflection.MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(compType.GetManagedType());
                var parameters = new Il2CppSystem.Object[]{target.BoxIl2CppObject()};
                object componentData = genericMethodInfo.Invoke( G.EntityManager.BoxIl2CppObject(), parameters );
                Output.Log(compType.GetManagedType().ToString() + ": " + JsonConvert.SerializeObject((Il2CppSystem.Object)componentData));
            }
            catch { }
        }
    }

    public static void Destroy(this Entity target)
    {
        ProjectM.Shared.DestroyUtility.CreateDestroyEvent(G.EntityManager, target, DestroyReason.Default, DestroyDebugReason.ByScript);
    }
    public static void Teleport(this Entity target, float3 position) // To be REVISED
    {
        if (!G.EntityManager.HasComponent<PlayerCharacter>(target)) return;
        var userEntity = G.EntityManager.GetComponentData<PlayerCharacter>(target).UserEntity;

        var fromCharacter = new FromCharacter { User = userEntity, Character = target };

        var entity = G.EntityManager.CreateEntity(
            ComponentType.ReadWrite<FromCharacter>(),
            ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
        );

        G.EntityManager.SetComponentData(entity, fromCharacter);

        G.EntityManager.SetComponentData<PlayerTeleportDebugEvent>(entity, new()
        {
            Position = position,
            Target = PlayerTeleportDebugEvent.TeleportTarget.Self
        });
    }
}

public static class Entities
{

}