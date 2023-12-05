using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectM;
using System.Text.RegularExpressions;
using Unity.Entities.UniversalDelegates;
using ProjectM.Shared;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using ProjectM.Gameplay;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using static ProjectM.Scripting.Game;

namespace DojoArenaKT;
public static class JewelCommand
{
    public const float DefaultPower = 1f;

    [Command("j", "<spell name> [mods: 132|1,3,2|13,2|?]", "Spawns a spell-enhancing jewel with the specified mods.", AdminOnly = false, Aliases = new[] { "jewel" })]
    public static string RunJewelCommand(Player user, string args)
    {
        if (args.Length == 0) return $"Usage: {Color.White}.jewel <spell name> [mods: 132|1,3,2|13,2|?]";
        var splitArgs = args.Split(' ');
        string jewelString = "";
        string modString = "";
        if (splitArgs.Length == 1)
        {
            if (args.Contains("?")) return $"Usage: {Color.White}.jewel <spell name> [mods: 132|1,3,2|13,2|?]";
            jewelString = splitArgs[0];
        }
        else
        {
            string lastArg = splitArgs[splitArgs.Length - 1];
            if (lastArg == "?" || Regex.IsMatch(lastArg, "(,?(\\d+),?)+"))
            {
                modString = splitArgs[splitArgs.Length - 1];
                jewelString = string.Join("", splitArgs.SkipLast(1)).ToLower();
            }
            else
            {
                jewelString = string.Join("", splitArgs).ToLower();
            }
        }

        if (jewelString == "") return $"Usage: {Color.White}.jewel <spell name> [mods: 132|1,3,2|13,2|?]";
        List<(string keyword, int guid)> matches = new();
        foreach ((string keyToCheckRaw, int keyGUID) in Jewels.SpellKeywords)
        {
            string keyToCheck = keyToCheckRaw.ToLower().Replace(" ", "");
            if (keyToCheck == jewelString) // Exact match
            {
                matches.Clear();
                matches.Add((keyToCheckRaw, keyGUID));
                break;
            }
            if (keyToCheck.Contains(jewelString))
            {
                matches.Add((keyToCheckRaw, keyGUID));
            }
        }

        if (matches.Count == 0) return $"The specified spell could not be found.\r\nUsage: {Color.White}.jewel <spell name> [mods: 132|1,3,2|13,2|?]";
        if (matches.Count > 1) return $"Your spell name has returned multiple matches. Please specify more of the spell name.";

        var jewelName = matches[0].keyword;
        var jewelGUID = matches[0].guid;
        var jewelData = Jewels.List[jewelGUID];

        if (modString == "?")
        {
            user.Message($"{Color.Yellow}{jewelName} {Color.White}Mods: ");
            foreach (var kvp in jewelData.SpellMods)
            {
                var niceName = kvp.Value.Name.Replace("SpellMod_Shared_", "").Replace("SpellMod_", "");
                user.Message($"{Color.White}{kvp.Key}. {niceName}");
            }
            return null;
        }

        HashSet<int> modIDs = new();
        List<string> modNames = new();
        var modStringSplit = modString.Split(',');

        if (modStringSplit.Length == 1)
        {
            foreach (char c in modStringSplit[0])
            {
                if (!char.IsDigit(c))
                {
                    return $"Error: A specified mod contained a non-digit character.\r\nUsage: {Color.White}.jewel <spell name> [mods: 132|1,3,2|13,2|?]";
                }
                if (int.TryParse(c.ToString(), out int parsed)) modIDs.Add(parsed);
                else return $"Error: Could not parse a mod digit as a number.\r\nUsage: {Color.White}.jewel <spell name> [mods: 132|1,3,2|13,2|?]";
            }
        }
        else
        {
            foreach (var split in modStringSplit)
            {
                if (int.TryParse(split, out int modID)) modIDs.Add(modID);
                else return $"Error: Could not parse a mod ID as a number.\r\nUsage: {Color.White}.jewel <spell name> [mods: 132|1,3,2|13,2|?]";
            }
        }

        modIDs.RemoveWhere(x => x <= 0);

        string modsMessage = "";
        byte modCount = 0;

        SpellModSet newSpellModSet = new();

        
        if (modIDs.Count > 3)
        {
            return $"Error: You are limited to 3 mods per jewel.\r\nUsage: {Color.White}.jewel <spell name> [mods: 132|1,3,2|13,2|?]";
        }

        if (modIDs.Count > 0)
        {
            foreach (int modID in modIDs)
            {
                if (!jewelData.SpellMods.ContainsKey(modID)) return $"The mod ID {Color.White}{modID}{Color.Clear} is not valid for that jewel.";
                modNames.Add(Color.Teal + jewelData.SpellMods[modID].Name.Replace("SpellMod_Shared_", "").Replace("SpellMod_", "") + Color.Clear);

                var modGUID = jewelData.SpellMods[modID].GUID;
                var spellMod = new SpellMod() {Power = DefaultPower, Id = new PrefabGUID(modGUID)};

                switch(modCount)
                {
                    case 0: newSpellModSet.Mod0 = spellMod; break;
                    case 1: newSpellModSet.Mod1 = spellMod; break;
                    case 2: newSpellModSet.Mod2 = spellMod; break;
                    case 3: newSpellModSet.Mod3 = spellMod; break;
                    case 4: newSpellModSet.Mod4 = spellMod; break;
                    case 5: newSpellModSet.Mod5 = spellMod; break;
                    case 6: newSpellModSet.Mod6 = spellMod; break;
                    case 7: newSpellModSet.Mod7 = spellMod; break;
                    default: return "Something went wrong. Contact a dev!";
                }
                modCount++;
            }

            newSpellModSet.Count = modCount;
            modsMessage = $"\r\n{Color.White}With {string.Join($"{Color.White}, ", modNames)}";
        }

        var itemResponse = user.GiveItem(new PrefabGUID(jewelGUID), 1);
        if (!itemResponse.Success) return "There was an error in trying to give you the jewel. Maybe your inventory is full?";
        if (modCount > 0)
        {
            var ent = itemResponse.NewEntity;

            Delay.Action(0.1, () => {
                var comp = ent.GetData<SpellModSetComponent>();
                G.World.GetExistingSystem<SpellModSyncSystem_Server>().AddSpellMod(ref newSpellModSet);
                comp.SpellMods = newSpellModSet;
                
                itemResponse.NewEntity.SetData(comp);

                var comp2 = ent.GetData<JewelInstance>();
                comp2.Ability = new PrefabGUID(jewelData.AbilityPrefabGUID);
                comp2.OverrideAbilityType = new PrefabGUID(0);
                ent.SetData(comp2);
            });

        }
        user.Message($"{Color.White}You've been given a {Color.Yellow}Tier 3 {jewelName} Jewel{Color.Clear}{modsMessage}");
        
        return null;
    }
}