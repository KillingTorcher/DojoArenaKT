using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace DojoArenaKT;
    
public static class BloodPotionCommand
{
    static Dictionary<ulong, (BloodType, float)> lastPotionCache = new();

    [Command("bp", "<Type> [<Quality>]", "Creates a Potion with specified Blood Type, Quality and Value.",  Aliases = new[] { "bloodpotion" }, AdminOnly = false)]
    public static string BPCommand(Player player, List<string> args)
    {
        bool updatedPreferredType = false;
        float bloodQuality = 100f;
        BloodType bloodType = BloodType.Frailed;
        if (lastPotionCache.ContainsKey(player.SteamID64))
        {
            (bloodType, bloodQuality) = lastPotionCache[player.SteamID64];
            bloodQuality = Math.Clamp(bloodQuality, 0f, 100f);
        }
        

        if (args.Count >= 2 && float.TryParse(args[1], out bloodQuality))
            bloodQuality = Math.Clamp(bloodQuality, 0f, 100f);

        if (args.Count >= 1)
        {
            var name = args[0];
            if (!Enum.TryParse(name, true, out bloodType) || !Enum.IsDefined(typeof(BloodType), bloodType))
            {
                player.Message("Error: Unknown blood type. Dispensing frailed blood instead!");
                bloodType = BloodType.Frailed;
            }
            else
            {
                lastPotionCache[player.SteamID64] = (bloodType, bloodQuality);
                updatedPreferredType = true;
            }
        }

        var addItemResponse = player.GiveItem(new PrefabGUID(828432508), 1);
        if (addItemResponse.Result != AddItemResult.Success_Complete) return "Something went wrong setting up your blood potion!";

        var blood = new StoredBlood()
        {
            BloodQuality = bloodQuality,
            BloodType = new PrefabGUID((int)bloodType)
        };
        addItemResponse.NewEntity.SetData<StoredBlood>(blood);

        player.Message($"Spawned {Color.Yellow}{bloodType.ToString()} Potion{Color.Clear} with{Color.Yellow} {bloodQuality.ToString()}%{Color.Clear} quality");
        if (updatedPreferredType) player.Message($"{Color.White}Your preference for blood potions has been saved. {Environment.NewLine}To spawn another {bloodQuality:F0}% {bloodType.ToString()} Potion, you can just type {Color.Green}.bp");
        return null;
    }
}

public enum BloodType
{
	Frailed = -899826404,
	Creature = -77658840,
	Warrior = -1094467405,
	Rogue = 793735874,
	Brute = 581377887,
	Scholar = -586506765,
	Worker = -540707191,
	Mutant = -2017994753,
}
