using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    const int MaxEntries = 3;

    [SerializeField]
    private List<LeaderboardSection> LeaderboardSection; // Assign 3 LeaderSection objects in the inspector

    public void AddScore(string playerName, int score)
    {
        List<(string, int)> entries = GetScores();
        entries.Add((playerName, score));
        entries = entries.OrderByDescending(e => e.Item2).Take(MaxEntries).ToList();

        for (int i = 0; i < entries.Count; i++)
        {
            PlayerPrefs.SetString($"Leaderboard_Name_{i}", entries[i].Item1);
            PlayerPrefs.SetInt($"Leaderboard_Score_{i}", entries[i].Item2);
        }
        PlayerPrefs.Save();
    }

    public List<(string, int)> GetScores()
    {
        var scores = new List<(string, int)>();
        for (int i = 0; i < MaxEntries; i++)
        {
            string name = PlayerPrefs.GetString($"Leaderboard_Name_{i}", "");
            int score = PlayerPrefs.GetInt($"Leaderboard_Score_{i}", 0);
            if (!string.IsNullOrEmpty(name))
                scores.Add((name, score));
        }
        return scores;
    }   

    // Call this after user presses confirm
    public void UpdateLeaderboard()
    {
        var scores = GetScores();
        for (int i = 0; i < LeaderboardSection.Count; i++)
        {
            if (i < scores.Count)
            {
                LeaderboardSection[i].Setup(scores[i].Item1, scores[i].Item2);
            }
            else
            {
                LeaderboardSection[i].Setup("-", 0);
            }
        }
    }
}