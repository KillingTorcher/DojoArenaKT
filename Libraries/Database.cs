using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DojoArenaKT;
public static class Database
{
    public static Dictionary<string, WaypointData> globalWaypoint = new();
    public static JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        WriteIndented = false,
        IncludeFields = false
    };

    public static class BuffGUID
    {
        public static PrefabGUID AB_Interact_GetInside_Owner_Buff_Stone = new PrefabGUID(569692162); //-- Inside Stone Coffin
        public static PrefabGUID AB_Interact_GetInside_Owner_Buff_Base = new PrefabGUID(381160212); //-- Inside Base/Wooden Coffin

        public static PrefabGUID AB_ExitCoffin_Travel_Phase_Stone = new PrefabGUID(-162820429);
        public static PrefabGUID AB_ExitCoffin_Travel_Phase_Base = new PrefabGUID(-997204628);
        public static PrefabGUID AB_Interact_TombCoffinSpawn_Travel = new PrefabGUID(722466953);

        public static PrefabGUID AB_Interact_WaypointSpawn_Travel = new PrefabGUID(-66432447);
        public static PrefabGUID AB_Interact_WoodenCoffinSpawn_Travel = new PrefabGUID(-1705977973);
        public static PrefabGUID AB_Interact_StoneCoffinSpawn_Travel = new PrefabGUID(-1276482574);

        public static PrefabGUID WolfStygian = new PrefabGUID(-1158884666);
        public static PrefabGUID WolfNormal = new PrefabGUID(-351718282);
        public static PrefabGUID BatForm = new PrefabGUID(1205505492);
        public static PrefabGUID NormalForm = new PrefabGUID(1352541204);
        public static PrefabGUID RatForm = new PrefabGUID(902394170);

        public static PrefabGUID DownedBuff = new PrefabGUID(-1992158531);
        public static PrefabGUID BloodSight = new PrefabGUID(1199823151);

        public static PrefabGUID InCombat = new PrefabGUID(581443919);
        public static PrefabGUID InCombat_PvP = new PrefabGUID(697095869);
        public static PrefabGUID OutofCombat = new PrefabGUID(897325455);
        public static PrefabGUID BloodMoon = new PrefabGUID(-560523291);

        public static PrefabGUID Severe_GarlicDebuff = new PrefabGUID(1582196539);          //-- Using this for PvP Punishment debuff
        public static PrefabGUID General_GarlicDebuff = new PrefabGUID(-1701323826);

        public static PrefabGUID Buff_VBlood_Perk_Moose = new PrefabGUID(-1464851863);      //-- Using this for commands & mastery buff
        public static PrefabGUID PerkMoose = new PrefabGUID(-1464851863);

        public static PrefabGUID SiegeGolem_T01 = new PrefabGUID(-148535031);
        public static PrefabGUID SiegeGolem_T02 = new PrefabGUID(914043867);

        //-- LevelUp Buff
        public static PrefabGUID LevelUp_Buff = new PrefabGUID(-1133938228);

        //-- Nice Effect...
        public static PrefabGUID AB_Undead_BishopOfShadows_ShadowSoldier_Minion_Buff = new PrefabGUID(450215391);   //-- Impair cast & movement

        //-- Relic Buff
        //[-238197495]          AB_Interact_UseRelic_Manticore_Buff
        //[-1161197991]		    AB_Interact_UseRelic_Paladin_Buff
        //[-1703886455]		    AB_Interact_UseRelic_Behemoth_Buff

        //-- Fun
        public static PrefabGUID Pig_Transform_Debuff = new PrefabGUID(1356064917);
    }
}