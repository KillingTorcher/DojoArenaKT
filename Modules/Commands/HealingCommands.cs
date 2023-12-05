using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace DojoArenaKT;
public static class HealingCommands
{
    [Command("r", "", "Heals you to full and restores your cooldowns.", AdminOnly = false, Aliases = new[] { "cd", "hp" } )]
    public static void RCommand(Player player)
    {
        player.FullyRestore();
        player.Message($"{Color.White}Your health and cooldowns have been restored.");
    }
}
