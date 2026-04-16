using System;
using System.Collections.Generic;
using System.Linq;

public enum GameMapType
{
    Map01 = 0,
    Map02 = 1,
    Map03 = 2
}

public static class GameMapCatalog
{
    static readonly List<string> mapNames = Enum.GetNames(typeof(GameMapType)).ToList();

    public static IReadOnlyList<string> GetMapNames()
    {
        return mapNames;
    }

    public static string Normalize(string mapName)
    {
        if (string.IsNullOrWhiteSpace(mapName))
        {
            return GameMapType.Map01.ToString();
        }

        string trimmed = mapName.Trim();
        return mapNames.Contains(trimmed) ? trimmed : GameMapType.Map01.ToString();
    }
}
