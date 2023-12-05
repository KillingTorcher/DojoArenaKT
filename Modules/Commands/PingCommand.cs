using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DojoArenaKT;

public static class PingCommand
{
    [Command("p", "ping", "Shows you your ping.", AdminOnly = false, Aliases = new[] { "ping" })]
    public static void RunPingCommand(Player user)
    {
        var ping = user.GetData<Latency>().Value;
        user.Message($"Your latency is {Color.White}{ping * 1000}{Color.Clear} ms");
    }
}