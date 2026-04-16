using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MapDataRepository
{
    static readonly List<MapDataDefinition> loadedMaps = new List<MapDataDefinition>();
    static bool loaded;

    public static IReadOnlyList<MapDataDefinition> GetAll()
    {
        EnsureLoaded();
        return loadedMaps;
    }

    public static MapDataDefinition GetByType(GameMapType mapType)
    {
        EnsureLoaded();
        return loadedMaps.FirstOrDefault(map => map != null && map.mapType == mapType);
    }

    public static MapDataDefinition GetByMapName(string mapName)
    {
        string normalized = GameMapCatalog.Normalize(mapName);
        if (!System.Enum.TryParse(normalized, out GameMapType mapType))
        {
            return null;
        }

        return GetByType(mapType);
    }

    static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        loadedMaps.Clear();
        loadedMaps.AddRange(Resources.LoadAll<MapDataDefinition>("MapDefinitions"));
        loadedMaps.RemoveAll(item => item == null);
    }
}
