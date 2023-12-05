using ProjectM;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System;
using DojoArenaKT;

namespace DojoArenaKT;


public class ReplacingItemKit
{
    public List<ReplacingItem> Items { get; set; }

    public ReplacingItemKit()
    {
        Items = new List<ReplacingItem>();
    }
}

public class ReplacingItem
{
    public bool AutoEquip { get; set; }
    public int PrefabGUID { get; set; }

    public ReplacingItem()
    {
        AutoEquip = false;
        PrefabGUID = 0;
    }
}


public static class KitCommand
{
    public static Dictionary<string, ReplacingItemKit> Kits = new();
    public const string SaveFile = Main.Folder + "kits.json";

    [Command("kit", "[Name]", "Gives you a previously specified set of items.", AdminOnly = false)]
    public static void Initialize(Player player, string kitName)
    {
        string suppliedArgument = "sanguine";
        if (kitName.Length > 0) suppliedArgument = kitName;

        if (!Kits.ContainsKey(suppliedArgument))
        {
            player.Message($"Kit by the name of {Color.White}{suppliedArgument}{Color.Clear} does not exist. Available kits: {string.Join(""",""", Kits.Keys)}");
            return;
        }

        foreach (var equipData in player.GetEquipment())
        {
            var prefabGUID = equipData.Item1;
            player.RemoveItem(prefabGUID, 1);
        }

        string[] savePreferencesFor = {
            "Slasher",
            "Axe",
            "Spear",
            "_Sword_",
            "Crossbow",
            "Mace",
            "_GreatSword_",
            "Pistols"
        };
        Dictionary<string, int> savedSlots = new();

        foreach (var kitKvp in Kits)
        {
            foreach (ReplacingItem replItem in kitKvp.Value.Items)
            {
                var prefabGUID = new PrefabGUID(replItem.PrefabGUID);
                var itemCount = player.CountItems(prefabGUID);
                if (itemCount == 0) continue;       
                
                var prefabName = prefabGUID.GetPrefabName();

                foreach (var filter in savePreferencesFor)
                {
                    if (prefabName.Contains("Weapon_Reaper"))
                    {
                        var saveSlot = "Weapon_Reaper";
                        if (prefabName.Contains("UndeadGeneral")) saveSlot = "Weapon_Reaper_UndeadGeneral";

                        if (InventoryUtilities.TryGetItemSlot(G.EntityManager, player.Character, prefabGUID, out int slotID))
                        {
                            savedSlots[saveSlot] = slotID;
                            break;
                        }
                    }
                    else if (prefabName.Contains(filter))
                    {
                        if (InventoryUtilities.TryGetItemSlot(G.EntityManager, player.Character, prefabGUID, out int slotID))
                        {
                            savedSlots[filter] = slotID;
                            break;
                        }
                    }
                }

                player.RemoveItem(prefabGUID, itemCount);
            }
        }

        List<PrefabGUID> defferedGive = new();

        foreach (ReplacingItem giveItem in Kits[suppliedArgument].Items)
        {
            var prefabGUID = new PrefabGUID(giveItem.PrefabGUID);

            if (giveItem.AutoEquip) player.GiveEquipItem(prefabGUID);
            else
            {
                var prefabName = prefabGUID.GetPrefabName();
                int slotID = -1;
                if (prefabGUID.GuidHash == 1887724512 && savedSlots.ContainsKey("Weapon_Reaper_UndeadGeneral"))
                {
                    slotID = savedSlots["Weapon_Reaper_UndeadGeneral"];
                    savedSlots.Remove("Weapon_Reaper_UndeadGeneral");
                }

                foreach ((var filter, int iteratedSlotID) in savedSlots)
                {
                    if (prefabGUID.GuidHash == 1887724512) continue;

                    if (prefabName.Contains(filter))
                    {
                        slotID = iteratedSlotID;
                        break;
                    }
                }

                if (slotID == -1)
                {
                    defferedGive.Add(prefabGUID);
                }
                else
                {
                    player.GiveItemAtSlot(prefabGUID, 1, slotID);
                }
            }
        }

        foreach (var defferedItem in defferedGive)
        {
            player.GiveItem(defferedItem, 1);
        }
        player.Message($"You got the kit: <color=#ffff00ff>{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(suppliedArgument)}</color>");

        return;
    }
       

    [EventSubscriber(Events.Load)]
    public static void LoadKits()
    {
        try
        {
            if (!File.Exists(SaveFile))
            {
                SaveKits();
            }

            var json = File.ReadAllText(SaveFile);
            Kits = JsonSerializer.Deserialize<Dictionary<string, ReplacingItemKit>>(json);
            
            Output.Log("Loaded Kits Config successfully.");
        }
        catch (Exception ex)
        {
            Output.LogError("LoadKits-kits.json", ex);
        }
    }

    public static void SaveKits()
    {
        try {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true,
                IncludeFields = true
            };
            File.WriteAllText(SaveFile, JsonSerializer.Serialize(Kits, options));
            Output.Log("Saved kits.json");
        }
        catch (System.Exception ex)
        {
            Output.Log(ex);
        }
    }
}