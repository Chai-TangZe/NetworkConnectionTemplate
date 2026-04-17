using UnityEngine;

public static class RoomIdGenerator
{
    public static string GenerateSixDigitId()
    {
        return Random.Range(100000, 1000000).ToString();
    }
}
