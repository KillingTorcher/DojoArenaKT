using ProjectM;
using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using Unity.Mathematics;

namespace DojoArenaKT;
public static class G
{
    public static Dictionary<string, (string Name, PrefabGUID PrefabGUID)> PrefabNameLookup = new();
    public static Dictionary<Type, List<(MethodInfo Method, Attribute Attribute)>> MethodsWithAttributes = new();
    public static bool IsServer => Application.productName == "VRisingServer";

    private static World _serverWorld;
    private static EntityCommandBufferSystem _bufferSystem;
    private static ServerBootstrapSystem _bootstrapSystem;
    private static DebugEventsSystem _debugEvents;
    private static GameDataSystem _gameData;
    private static PrefabCollectionSystem _prefabSystem;
    private static AdminAuthSystem _adminSystem;
    public static World World
    {
        get
        {
            if (_serverWorld != null) return _serverWorld;

            _serverWorld = GetWorld("Server")
                ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
            return _serverWorld;
        }
    }
    public static EntityManager EntityManager
    {
        get
        {
            return World.EntityManager;
        }
    }

    public static EntityCommandBufferSystem BufferSystem
    {
        get
        {
            if (_bufferSystem == null) _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
            return _bufferSystem;
        }
    }
    
    public static ServerBootstrapSystem BootstrapSystem
    {
        get
        {
            if (_bootstrapSystem == null) _bootstrapSystem = World.GetOrCreateSystem<ServerBootstrapSystem>();
            return _bootstrapSystem;
        }
    }

    public static DebugEventsSystem DebugSystem
    {
        get
        {
            if (_debugEvents == null) _debugEvents = World.GetOrCreateSystem<DebugEventsSystem>();
            return _debugEvents;
        }
    }

    public static GameDataSystem GameData
    {
        get
        {
            if (_gameData == null) _gameData = World.GetOrCreateSystem<GameDataSystem>();
            return _gameData;
        }
    }
    public static PrefabCollectionSystem PrefabSystem
    {
        get
        {
            if (_prefabSystem == null) _prefabSystem = World.GetOrCreateSystem<PrefabCollectionSystem>();
            return _prefabSystem;
        }
    }
    public static AdminAuthSystem AdminSystem
    {
        get
        {
            if (_adminSystem == null) _adminSystem = World.GetOrCreateSystem<AdminAuthSystem>();
            return _adminSystem;
        }
    }

    private static World GetWorld(string name)
    {
        foreach (var world in World.s_AllWorlds)
        {
            if (world.Name == name)
            {
                return world;
            }
        }

        return null;

    }

    public static void SetupAttributedMethodList()
    {
        var methods = System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(false).Length > 0)
                .ToArray();

        foreach (var markedMethod in methods)
        {
            foreach (var attrib in markedMethod.CustomAttributes)
            {
                try
                {
                    if (!MethodsWithAttributes.ContainsKey(attrib.AttributeType)) MethodsWithAttributes.Add(attrib.AttributeType, new());
                    MethodsWithAttributes[attrib.AttributeType].Add((markedMethod, markedMethod.GetCustomAttribute(attrib.AttributeType, false)));
                }
                catch { }
            }
        }
    }

    public static float3 defaultSpawnLocation = new float3(-2002.5f, 5, -2797.5f); 
  
    public static float3 SpawnLocation
    {
        get
        {
            if (Database.globalWaypoint.TryGetValue("spawn", out var value)) return new float3(value.LocationX, value.LocationY, value.LocationZ);
            return defaultSpawnLocation;
        }
    }
    [EventSubscriber(Events.Load)]
    public static void PopulatePrefabLookup()
    {
        foreach (var kvp in PrefabSystem.SpawnableNameToPrefabGuidDictionary)
        {
            PrefabNameLookup[kvp.Key.ToLower()] = (kvp.Key, kvp.Value);
        }
    }
}