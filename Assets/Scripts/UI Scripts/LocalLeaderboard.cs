using System.Collections.Generic;
using UnityEngine;

public class LocalLeaderboard : MonoBehaviour
{
    public static List<float> BestTimes = new List<float>();
    public static List<string> BestNames = new List<string>();

    public static void AddTime(float time, string playerName)
    {
        BestTimes.Add(time);
        BestNames.Add(playerName);
        SortLeaderboard();
    }

    private static void SortLeaderboard()
    {
        for (int i = 0; i < BestTimes.Count - 1; i++)
        {
            for (int j = i + 1; j < BestTimes.Count; j++)
            {
                if (BestTimes[i] > BestTimes[j])
                {
                    // Swap times
                    float tempTime = BestTimes[i];
                    BestTimes[i] = BestTimes[j];
                    BestTimes[j] = tempTime;

                    // Swap names
                    string tempName = BestNames[i];
                    BestNames[i] = BestNames[j];
                    BestNames[j] = tempName;
                }
            }
        }
    }
}
