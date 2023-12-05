using BepInEx.Logging;
using System;
using Unity.Entities;
using ProjectM.Network;
using ProjectM;
namespace DojoArenaKT;

public static class Output
{
    public static ManualLogSource Logger;
    public static void Log(object messageObject, LogLevel logLevel = LogLevel.Warning, string LogType = Logs.CompleteLogFileName)
    {
        try
        {
            string logText = messageObject.ToString();
            Logger.Log(logLevel, logText);
            Logs.Write(logText, LogType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to execute Output.Log '{messageObject.ToString()}' with Error: {ex.ToString()}");
        }
    }

    public static void LogError(string identifier, object exception, string additionalInfo = null)
    {
        try
        {
            string logText = $"Runtime Exception '{identifier}': " + Environment.NewLine
                + exception.ToString() + Environment.NewLine
                + (additionalInfo == null ? "No additional information." : $"Additional Information: {additionalInfo}");

            Logger.LogError(logText);
            Logs.Write(logText, "ErrorLog");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to execute Output.LogError '{identifier.ToString()}' with Error: {ex.ToString()}");
        }
    }

    public static void Dump(Entity ent)
    {
        ent.Dump();
    }

    public static void SendMessage(User user, object message)
    {
        ServerChatUtils.SendSystemMessageToClient(G.BufferSystem.CreateCommandBuffer(), user, Color.ProcessMessage(message.ToString()));
    }
    public static void SendMessage(Player player, object message)
    {
        SendMessage(player.User, message);
    }

    public static void SendMessage(Entity playerCharacterEntity, object message)
    {
        Player target = Player.FromCharacter(playerCharacterEntity);
        if (!target.IsValid) return;

        SendMessage(target, message);
    }
}