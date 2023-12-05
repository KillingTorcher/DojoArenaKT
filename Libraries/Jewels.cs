using ProjectM.Shared;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

namespace DojoArenaKT;
public class SpellModData
{
    public string PrefabName { get; set; }
    public int PrefabGUID  { get; set; }
    public string Group { get; set; }
    public string AbilityName { get; set; }
}

public struct SpellModConfig
{
    public int GUID { get; set; }
    public string Name { get; set; }
}
public class Jewel
{
    public string PrefabName { get; set; }
    public int PrefabGUID  { get; set; }
    public int AbilityPrefabGUID  { get; set; }
    public string AbilityPrefabName { get; set; }
    public string AbilityGroup { get; set; }
    public string AbilityName { get; set; }
    public Dictionary<int, SpellModConfig> SpellMods { get; set; }
    internal Dictionary<int, string> spellModsInternal;
}
public static class Jewels
{
    public const string ConfigFile = Main.Folder + "jewels.json";
    public static Dictionary<int, Jewel> List = new();
    public static Dictionary<string, int> SpellKeywords = new();
    public static string SpellKeywordsJoined;

    [EventSubscriber(Events.Load)]
    public static void LoadConfig()
    {
        if (!File.Exists(ConfigFile))
        {
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(List, new JsonSerializerOptions() { WriteIndented = true }));
        }

        List = JsonSerializer.Deserialize<Dictionary<int, Jewel>>(File.ReadAllText(ConfigFile));
        List<string> joinKeywords = new();

        foreach ((int guid, Jewel jwl) in List)
        {
            var splitName = jwl.PrefabName.Split('_');
            var keyword = splitName.Last();
            keyword = Regex.Replace(keyword, "(?<=[a-zA-Z])(?=[A-Z])", " ");
            SpellKeywords[keyword] = guid;
            joinKeywords.Add(keyword);
        }
        SpellKeywordsJoined = string.Join(", ", joinKeywords);
        Output.Log("Loaded Jewel Configuration successfully.");
    }
    
    public static void GenerateDebugConfig()
    {
        Dictionary<int, Jewel> legalJewels = new();
        List<SpellModData> legalSpellMods = new();
        string allSpellMods = "";
        Dictionary<string, HashSet<int>> associativeDict = new();
        List<SpellModData> oddOnesOut = new();

        foreach (var kvp in G.PrefabSystem.PrefabGuidToNameDictionary)
        {
            var guid = kvp.Key;
            var prefabName = kvp.Value;
            if (prefabName.StartsWith("SpellMod_"))
            {
                allSpellMods += string.Format("{0,-15} {1}", guid.GuidHash, prefabName) + System.Environment.NewLine; 
            }

            if (prefabName.StartsWith("SpellMod_") && !prefabName.StartsWith("SpellMod_Weapon_") && !prefabName.Contains("DefaultCurve") && !prefabName.Contains("_Shared_Weapon_"))
            {
                var simpleName = prefabName.Replace("SpellMod_Shared_", "").Replace("SpellMod_", "");
                var simpleNameBroken = simpleName.Split('_');
                var newSpellMod = new SpellModData()
                {
                    PrefabName = prefabName,
                    PrefabGUID = guid.GuidHash,
                    Group = simpleNameBroken[0],
                    AbilityName = simpleNameBroken.Length >= 3 ? simpleNameBroken[1] : simpleNameBroken[0]
                };

                legalSpellMods.Add(newSpellMod);
                continue;
            }
            if (!prefabName.StartsWith("Item_Jewel") || !prefabName.Contains("_T03_")) continue;

            var comp = G.PrefabSystem.PrefabLookupMap.GuidToEntityMap[guid].GetData<JewelInstance>();
            var abilityGUID = comp.OverrideAbilityType.GuidHash;

            var abilityPrefabName = comp.OverrideAbilityType.GetPrefabName().Replace("AB_", "").Replace("_AbilityGroup", "");
            var abilityPrefabSplit = abilityPrefabName.Split('_');
            Debug.Assert(abilityPrefabSplit.Length > 0 && abilityPrefabSplit.Length < 3);
            var abGroup = abilityPrefabSplit[0];
            var abName = abilityPrefabSplit.Length >= 2 ? abilityPrefabSplit[1] : abilityPrefabSplit[0];

            var newJewel = new Jewel() {
                PrefabGUID = guid.GuidHash,
                PrefabName = prefabName,
                AbilityPrefabGUID = abilityGUID,
                AbilityPrefabName = abilityPrefabName,
                AbilityGroup = abGroup,
                AbilityName = abName,
                spellModsInternal = new()
            };

            legalJewels[guid.GuidHash] = newJewel;

            // Building an associative dictionary
            var ident1Lower = abGroup.ToLower();
            var ident2Lower = abName.ToLower();
            if (!associativeDict.ContainsKey(ident1Lower)) associativeDict[ident1Lower] = new();
            if (!associativeDict.ContainsKey(ident2Lower)) associativeDict[ident2Lower] = new();
            associativeDict[ident1Lower].Add(newJewel.PrefabGUID);
            associativeDict[ident2Lower].Add(newJewel.PrefabGUID);
        }

        foreach (var spellMod in legalSpellMods)
        {
            HashSet<Jewel> jewelList = new();
            bool assigned = false;
            if (associativeDict.ContainsKey(spellMod.Group.ToLower()) && spellMod.PrefabName.Contains("_Shared_"))
            {
                foreach (var jewel in associativeDict[spellMod.Group.ToLower()])
                    legalJewels[jewel].spellModsInternal[spellMod.PrefabGUID] = spellMod.PrefabName;

                assigned = true;
            }

            if (associativeDict.ContainsKey(spellMod.AbilityName.ToLower()))
            {
                foreach (var jewel in associativeDict[spellMod.AbilityName.ToLower()])
                    legalJewels[jewel].spellModsInternal[spellMod.PrefabGUID] = spellMod.PrefabName;

                assigned = true;
            }

            if (!assigned) oddOnesOut.Add(spellMod);

        }

        foreach ((_, var jewel) in legalJewels)
        {
            jewel.SpellMods = new();
            int i=1;
            foreach ((var modGUID, var modName) in jewel.spellModsInternal)
            {
                jewel.SpellMods[i] = new SpellModConfig { GUID = modGUID, Name = modName };
                i++;
            }
        }
        
        var json = JsonSerializer.Serialize(legalJewels, new JsonSerializerOptions() { WriteIndented = true });
        File.WriteAllText(Main.Folder + "generated_jewel_data.txt", json);
        File.WriteAllText(Main.Folder + "generated_oddonesout_jewel_data.txt", JsonSerializer.Serialize(oddOnesOut, new JsonSerializerOptions() { WriteIndented = true }));
        File.WriteAllText(Main.Folder + "generated_all_spellmods.txt", allSpellMods);

        Output.Log("Debug config generated for Jewels.");
    }
}
