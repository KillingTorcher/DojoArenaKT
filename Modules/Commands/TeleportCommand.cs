using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System.IO;
using System.Text.Json;

namespace DojoArenaKT;



public static class TeleportCommand
{
    public static int WaypointLimit = 3;
    private static EntityManager entityManager = G.EntityManager;

    [Command("tp", "<Name|Set|Remove|List> [Waypoint]", "Teleports you to previously created waypoints.", AdminOnly = false)]
    public static void Initialize(Player player, List<string> arguments)
    {
        if (arguments.Count < 1)
        {
            player.Message("Usage: tp <Name|Set|Remove|List> [Waypoint]");
            return;
        }

        Entity PlayerEntity = player.Character;
        ulong SteamID = player.SteamID64;
        string cmd = arguments[0];

        if (arguments.Count > 1)
        {
            string wp_name = arguments[1].ToLower();
            string wp_true_name = arguments[1].ToLower();

            if (cmd.ToLower().Equals("set"))
            {
                if (!player.IsAdmin)
                {
                    player.Message("You do not have permission to edit a global waypoint.");
                    return;
                }

                float3 location = G.EntityManager.GetComponentData<LocalToWorld>(player.Character).Position;
                float3 f2_location = new float3(location.x, location.y, location.z);
                SetWaypoint(SteamID, f2_location, wp_name, wp_true_name);
                SaveWaypoints();
                player.Message("Successfully added/modified Waypoint.");
                return;
            }
            else if (cmd.ToLower().Equals("remove"))
            {
                if (!player.IsAdmin)
                {
                    player.Message("You do not have permission to edit a global waypoint.");
                    return;
                }

                if (!Database.globalWaypoint.TryGetValue(wp_name, out _))
                {
                    player.Message($"Global \"{wp_name}\" waypoint not found.");
                    return;
                }

                player.Message("Successfully removed Waypoint.");
                RemoveWaypoint(SteamID, wp_name);
                SaveWaypoints();
                return;
            }
        }

        if (cmd.ToLower().Equals("list"))
        {
            int total_wp = 0;
            foreach (KeyValuePair<string, WaypointData> global_wp in Database.globalWaypoint)
            {
               player.Message($" - <color=#ffff00ff>{global_wp.Key}</color> [<color=#00dd00ff>Global</color>]");
                total_wp++;
            }
            return;
        }

        string waypoint = arguments[0].ToLower();

        if (Database.globalWaypoint.TryGetValue(waypoint, out WaypointData WPData))
        {
            player.Character.Teleport(new float3(WPData.LocationX, WPData.LocationY, WPData.LocationZ));
            return;
        }

        player.Message("Waypoint not found.");
    }

    public static void SetWaypoint(ulong owner, float3 location, string name, string true_name)
    {
        WaypointData WaypointData = new WaypointData(true_name, owner, location.x, location.y, location.z);
        Database.globalWaypoint[name] = WaypointData;
    }

    public static void RemoveWaypoint(ulong owner, string name)
    {
        Database.globalWaypoint.Remove(name);
    }

    static string WaypointFile = Main.Folder + "waypoints.json";

    [EventSubscriber(Events.Load)]
    public static void LoadWaypoints()
    {
        /*
         *  [Warning:   Dueller] we're loading our waypoints
            [Warning:   Dueller] Waypoints DB Populated
            [Warning:   Dueller] GlobalWaypoints DB Populated
            [Warning:   Dueller] GlobalWaypoints DB Created
         */


        if (!File.Exists(WaypointFile))
        {
            FileStream stream = File.Create(WaypointFile);
            stream.Dispose();
        }

        var json = File.ReadAllText(WaypointFile);
        try
        {
            Database.globalWaypoint = JsonSerializer.Deserialize<Dictionary<string, WaypointData>>(json);
            Output.Log("Loaded Global Waypoints successfully.");
        }
        catch (Exception ex)
        {
            Output.Log(ex.Message);
            Database.globalWaypoint = new Dictionary<string, WaypointData>();
            Output.Log("GlobalWaypoints DB Created");
        }
    }

    public static void SaveWaypoints()
    {
        File.WriteAllText(WaypointFile, JsonSerializer.Serialize(Database.globalWaypoint, Database.JsonOptions));
    }
}

public struct WaypointData
{
    public string Name { get; set; }
    public ulong Owner { get; set; }
    public float LocationX { get; set; }
    public float LocationY { get; set; }
    public float LocationZ { get; set; }

    public WaypointData(string name, ulong owner, float locationX, float locationY, float locationZ)
    {
        Name = name;
        Owner = owner;
        LocationX = locationX;
        LocationY = locationY;
        LocationZ = locationZ;
    }
}