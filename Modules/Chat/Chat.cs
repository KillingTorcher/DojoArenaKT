using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Bloodstone.Hooks;
using Bloodstone;
using System;
namespace DojoArenaKT;


[HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
public class ChatMessage
{
    [HarmonyPrefix]
    public static bool Prefix(ChatMessageSystem __instance)
    {
        try
        {
            NativeArray<Entity> entities = __instance.__ChatMessageJob_entityQuery.ToEntityArray(Allocator.Temp);
            foreach (var msgEntity in entities)
            {
                var fromComponent = msgEntity.GetData<FromCharacter>();
                var chattingPlayer = Player.FromCharacter(fromComponent.Character);
                var message = msgEntity.GetData<ChatMessageEvent>();
            
                if (!PlayerChatMessage(chattingPlayer, message))
                {
                    msgEntity.Destroy();
                    return false;
                }

            }
        }
        catch (Exception ex)
        {
            Output.LogError("ChatMessagePatch-Prefix", ex);
        }

        return true;
    }

    public static bool PlayerChatMessage(Player player, ChatMessageEvent message)
    {
        if (!player.IsValid) return false;
        var msgStr = message.MessageText.ToString();

        Output.Log($"[{player.SteamID64}] [{Enum.GetName(message.MessageType)}] {player.CharacterData.Name}: {msgStr}",
            BepInEx.Logging.LogLevel.Message, "ChatLog");

        if (msgStr.Length == 0) return false;

        try
        {
            if (Command.InterceptCommand(player, msgStr)) return false; // If a command was issued, hide it from other players and return.
        }
        catch (Exception ex)
        {
            Output.LogError("Command.InterceptCommand", ex, $"Message from {player.LongName} with text '{msgStr}'");
        }

        return true; // Whether to let the message be shown to all clients
    }
}