using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DojoArenaKT;
public static class HelpCommand
{
    [Command("help", "[command]", "Lists available commands.", AdminOnly = false, Aliases = new[] { "h", "commands", "list", "?"})]
    public static string HeNeedsSomeMilk(Player player, string detailedCommandInfo)
    {
        if (string.IsNullOrEmpty(detailedCommandInfo))
        {
            ListAllCommands(player);
            return null;
        }
        
        var key = detailedCommandInfo.Replace(" ", "").ToLower();
        if (!Command.Lookup.ContainsKey(key))
        {
            return $"Specified command not found. Try using {Color.White}.help{Color.Clear} without any arguments for a list of commands.";
        }

        var command = Command.Lookup[key];
        if (command.AdminOnly && !player.IsAdmin)
        {
            return $"Error: You do not have access to this command. Try using {Color.White}.help{Color.Clear} without any arguments for a list of commands.";
        }
        player.Message($"{Color.Green}.{command.Name.ToLower()} {Color.White}{command.Usage}{Environment.NewLine}Description: {command.Description}");

        return null;
    }

    public static void ListAllCommands(Player player)
    {
        List<CommandAttribute> adminCommands = new();

        player.Message($"{Color.Orange} === Command List ===");
        foreach (var command in Command.SortedList)
        {
            if (command.Hidden) continue;
            if (command.AdminOnly)
            {
                adminCommands.Add(command);
                continue;
            }

            string usageString = $"{Color.White}{command.Description}".Trim();
            player.Message($"{Color.Green}.{command.Name.ToLower()} {usageString}");
        }

        if (!player.IsAdmin) return;
        player.Message($"{Color.Orange} === Admin Commands ===");
        foreach (var command in adminCommands)
        {
            if (command.Hidden) continue;
            string usageString = $"{Color.White}{command.Description}".Trim();
            player.Message($"{Color.Red}.{command.Name.ToLower()} {usageString}");
        }
    }
}
