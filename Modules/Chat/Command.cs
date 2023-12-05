using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace DojoArenaKT;

public static class Command
{
    public static Dictionary<string, CommandAttribute> Lookup = new();
    public static List<CommandAttribute> SortedList = new();
    public const string UnknownCommand = $"Error: Unknown command. Use <color=#ffffffff>.help</color> for a list of commands.";
    public const string Error = "Fatal Error: There was a fatal exception during your use of that command. Please notify a developer.";
    public const string PermissionCheckFailed = "Error: You do not have permission to run this command.";

    public static bool ValidateCommandMethod(CommandAttribute attribute) // Validate that the command method is defined correctly
    {
        var paramList = attribute.Method.GetParameters();
        
        switch (paramList.Length)
        {
            case 0:
                attribute.Parameters = CommandParameters.None;
                return true;

            case 1:
                if (paramList[0].ParameterType == typeof(Player))
                {
                    attribute.Parameters = CommandParameters.Player;
                    return true;
                }
                return false;

            case 2:
                if (paramList[0].ParameterType != typeof(Player)) return false;
                if (paramList[1].ParameterType == typeof(List<string>))
                {
                    attribute.Parameters = CommandParameters.PlayerAndArgs;
                    return true;
                }
                else if (paramList[1].ParameterType == typeof(string))
                {
                    attribute.Parameters = CommandParameters.PlayerAndFullArgText;
                    return true;
                }
                return false;
            default:
                return false;
        }
    }
    [EventSubscriber(Events.Load)]
    public static void RegisterCommands()
    {
        if (!G.MethodsWithAttributes.ContainsKey(typeof(CommandAttribute))) return;
        foreach ((var commandMethod, var attribute) in G.MethodsWithAttributes[typeof(CommandAttribute)])
        {

            var commandAttribute = (CommandAttribute)attribute;
            commandAttribute.Method = commandMethod;
            commandAttribute.Name = commandAttribute.Name.ToLower().Replace(" ", "");
            var name = commandAttribute.Name;

            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!ValidateCommandMethod(commandAttribute))
            {
                Output.LogError("Command-ValidationFailed", $"Command {name} has failed method validation and may be inappropriately defined.", $"MethodName = {commandMethod.Name}");
                continue;
            }

            if (Lookup.ContainsKey(name))
            {
                Output.LogError("Command-NameTaken", $"The command name '{name}' was already taken in lookup by a pre-existing command!",
                    $"Method={commandAttribute.Method.Name}, Usage={commandAttribute.Usage}, Desc={commandAttribute.Description}");
                continue;
            }

            Lookup.Add(name, commandAttribute);
            SortedList.Add(commandAttribute);

            if (commandAttribute.Aliases != null)
            {
                foreach (var alias in commandAttribute.Aliases)
                {
                    if (Lookup.ContainsKey(alias))
                    {
                        Output.LogError("Command-AliasTaken", $"Command '{name}'s alternative alias '{alias}' in lookup by a pre-existing command!",
                            $"Method={commandAttribute.Method.Name}, Usage={commandAttribute.Usage}, Desc={commandAttribute.Description}");
                        continue;
                    }

                    Lookup.Add(alias, commandAttribute);
                }
            }
        }

        SortedList.Sort((x, y) => (x.Name.CompareTo(y.Name)));
    }

    public static void RunCommand(Player player, CommandAttribute command, List<string> parsedArgs, string chatMessage)
    {
        try
        {
            object ret = null;
            object[] invokeArgs = null;
            switch (command.Parameters)
            {
                case CommandParameters.None:
                    invokeArgs = null;
                    break;
                case CommandParameters.Player:
                    invokeArgs = new[] { player };
                    break;
                case CommandParameters.PlayerAndArgs:
                    invokeArgs = new object[] { player, parsedArgs };
                    break;
                case CommandParameters.PlayerAndFullArgText:
                    var splitArgs = chatMessage.Trim().Split(' ');
                    string argText = "";
                    if (splitArgs.Length >= 2) argText = string.Join(' ', splitArgs.Skip(1));

                    invokeArgs = new object[] { player, argText.Trim() }; 
                    break;
                default:
                    throw new Exception("CommandParameters are invalid. Command may not be properly defined or something may have gone wrong.");
            }

            ret = command.Method.Invoke(null, invokeArgs);

            string log = "ChatLog";
            if (command.AdminOnly) log = "AdminLog";
            Output.Log($"{player.LongName} executed command: {chatMessage}", BepInEx.Logging.LogLevel.Warning, log);

            if (ret != null && ret is string && !string.IsNullOrWhiteSpace((string)ret))
            {
                player.Message(ret);
            }
        }
        catch (Exception ex)
        {
            //Output.Log($"{player.LongIdentifier} encountered an error trying to run: {chatMessage}", BepInEx.Logging.LogLevel.Error, "AdminLog");
            Output.LogError("Command-ExecutionError", ex, $"Player {player.CharacterData.Name} tried to run '{command.Name}' unsucessfully.");
            player.Message(Error);
        }
    }
    public static bool InterceptCommand(Player player, string chatMessage)
    {
        char firstChar = chatMessage[0];
        if (chatMessage.Length < 2) return false;
        if (!player.CheckValidity()) return false;
        if (firstChar != '.' && firstChar != '!') return false;

        List<string> parsedCommand = Parse(chatMessage.Remove(0, 1));
        string commandIdentifier = parsedCommand[0].ToLower();
        parsedCommand.RemoveAt(0);

        if (!Lookup.ContainsKey(commandIdentifier))
        {
            player.Message(UnknownCommand);
            Output.Log($"{player.LongName} tried to run non-existent command: {chatMessage}", BepInEx.Logging.LogLevel.Warning, "AdminLog");
            return true;
        }

        var command = Lookup[commandIdentifier];
        if (command.AdminOnly && !player.User.IsAdmin)
        {
            player.Message(PermissionCheckFailed);
            Output.Log($"{player.LongName} failed a permission check trying to run: {chatMessage}", BepInEx.Logging.LogLevel.Error, "AdminLog");
            return true;
        }
        
        RunCommand(player, command, parsedCommand, chatMessage);
       
        return true;
    }

    public static List<string> Parse(string commandText) // Simple flat command parser. It took me ages, leave me alone.
    {
        List<string> choppedUp = new();

        char lastChar = ' ';
        int i = 0;
        int openQuoteLoc = -1;
        bool quoteMode = false;

        StringBuilder buffer = new();

        foreach (char c in commandText)
        {
            if (quoteMode)
            {
                if (c == ' ' && lastChar == '"' && openQuoteLoc != i-1)
                {
                    string newToken = stripQuotes(buffer.ToString());

                    choppedUp.Add(newToken);
                    buffer.Clear();
                    quoteMode = false;
                }
                else buffer.Append(c);
            }
            else
            {
                if (c == ' ' && buffer.Length > 0)
                {
                    choppedUp.Add(buffer.ToString());
                    buffer.Clear();
                }
                else if (c != ' ')
                {
                    if (c == '"')
                    {
                        quoteMode = true;
                        openQuoteLoc = i;
                    }
                    buffer.Append(c);
                }
            }
            i++;
            lastChar = c;
        }

        if (buffer.Length > 0)
        {
            string lastToken = buffer.ToString();
            if (lastChar == '"' && quoteMode && openQuoteLoc != i-1) lastToken = stripQuotes(lastToken);

            choppedUp.Add(lastToken);
        }

        return choppedUp;
    }

    private static string stripQuotes(string quotedString) // Helper function for parsing
    {
        int len = quotedString.Length;
        if (len > 2 && quotedString[0] == '"' && quotedString[len - 1] == '"')
        {
            return quotedString.Remove(len-1, 1).Remove(0, 1);
        }

        return quotedString;
    }
}

public enum CommandParameters
{
    Invalid,
    None,
    Player,
    PlayerAndArgs,
    PlayerAndFullArgText
}
public class CommandAttribute : Attribute
{
    public string Name = "";
    public string Usage = "";
    public string Description = "";
    public string[] Aliases = null;
    public bool AdminOnly = true;
    public bool Hidden = false;
    public CommandParameters Parameters = CommandParameters.Invalid;
    public MethodInfo Method;

    public CommandAttribute(string name, string usage, string description)
    {
        Name = name;
        Usage = usage;
        Description = description;
        AdminOnly = true;
    }
}