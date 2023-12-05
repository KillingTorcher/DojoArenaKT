using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using ProjectM.Shared;
using ProjectM.Network;

namespace DojoArenaKT;

public static class DayNightCycle
{
    [EventSubscriber(Events.Load)]
    public static void ResetTime()
    {
		SetDebugSettingEvent setDebugSettingEvent = new()
		{
			SettingType = DebugSettingType.DayNightCycleDisabled,
			Value = true,
		};
		G.DebugSystem.SetDebugSetting(0, ref setDebugSettingEvent);
        SetTime(0);
    }
    public static void SetTime(int hour)
    {
        G.World.GetExistingSystem<VariousMigratedDebugEventsSystem>().HandleSetTimeOfDayEvent(new ProjectM.Network.SetTimeOfDayEvent()
        {
            Hour = hour,
            Day = 0,
            Type = ProjectM.Network.SetTimeOfDayEvent.SetTimeType.Set
        });
    }
}
