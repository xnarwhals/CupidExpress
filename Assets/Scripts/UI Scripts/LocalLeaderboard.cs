// ...existing code...
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalLeaderboard
{
    public const int MaxEntries = 8; // Maximum number of entries to keep

    [System.Serializable]
    class Entry { public float time; public string name; }

    [System.Serializable]
    class Wrapper { public Entry[] entries; }

    // In-memory cache keyed by scene name (lazy-loaded from PlayerPrefs)
    private static readonly Dictionary<string, List<Entry>> cache = new Dictionary<string, List<Entry>>();

    // Public API: add time for the current scene (or pass a sceneName explicitly)
    public static void AddTime(float time, string playerName, string sceneName = null)
    {
        if (string.IsNullOrEmpty(sceneName)) sceneName = SceneManager.GetActiveScene().name;
        var list = LoadForScene(sceneName);

        list.Add(new Entry { time = time, name = playerName ?? "Player" });
        list.Sort((a, b) => a.time.CompareTo(b.time));
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);

        SaveForScene(sceneName, list);
    }

    // Get leaderboard for current scene (or a specific scene)
    public static IReadOnlyList<(float time, string name)> GetLeaderboard(string sceneName = null)
    {
        if (string.IsNullOrEmpty(sceneName)) sceneName = SceneManager.GetActiveScene().name;
        var list = LoadForScene(sceneName);
        return list.Select(e => (e.time, e.name)).ToList().AsReadOnly();
    }

    // Clear leaderboard for a scene (or all)
    public static void Clear(string sceneName = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            // clear everything
            var keys = cache.Keys.ToList();
            foreach (var k in keys)
            {
                cache.Remove(k);
                PlayerPrefs.DeleteKey(GetPrefsKey(k));
            }
        }
        else
        {
            cache.Remove(sceneName);
            PlayerPrefs.DeleteKey(GetPrefsKey(sceneName));
        }
        PlayerPrefs.Save();
    }

    // Internal helpers
    private static List<Entry> LoadForScene(string sceneName)
    {
        if (cache.TryGetValue(sceneName, out var cached)) return cached;

        var key = GetPrefsKey(sceneName);
        var json = PlayerPrefs.GetString(key, null);
        List<Entry> list = new List<Entry>();
        if (!string.IsNullOrEmpty(json))
        {
            var wrapper = JsonUtility.FromJson<Wrapper>(json);
            if (wrapper?.entries != null) list = new List<Entry>(wrapper.entries);
        }

        // ensure sorted & trimmed
        list.Sort((a, b) => a.time.CompareTo(b.time));
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);

        cache[sceneName] = list;
        return list;
    }

    private static void SaveForScene(string sceneName, List<Entry> list)
    {
        var wrapper = new Wrapper { entries = list.ToArray() };
        var json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(GetPrefsKey(sceneName), json);
        PlayerPrefs.Save();
    }

    private static string GetPrefsKey(string sceneName) => $"LocalLeaderboard_{sceneName}";
}