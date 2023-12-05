using Bloodstone.API;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace DojoArenaKT;

public static class Item
{
    public static PrefabGUID FromName(string name)
    {
        if (G.PrefabNameLookup.TryGetValue(name.ToLower(), out var kvp)) return kvp.PrefabGUID;
        return new PrefabGUID(0);
    }

    static ManagedItemData itemDataEmpty = default(ManagedItemData); // Just in case these /aren't/ properly managed I'll only make one empty instance

    public static ManagedItemData GetData(PrefabGUID prefabGUID)
    {
        try
        {
            ManagedDataRegistry managed = G.GameData.ManagedDataRegistry;
            ManagedItemData itemData = managed.GetOrDefault<ManagedItemData>(prefabGUID);
            return itemData;
        }
        catch { }
        return itemDataEmpty;
    }

    public static string GetName(PrefabGUID prefabGUID)
    {
        return GetData(prefabGUID).Name.ToString();
    }
}