using System;
using UnityEngine;

/// <summary>
/// Saves a serializable state object as JSON under one PlayerPrefs key.
/// </summary>
public static class PlayerPrefsJson
{
    public static T Load<T>(string key) where T : class, new()
    {
        var json = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(json))
            return new T();

        try
        {
            return JsonUtility.FromJson<T>(json) ?? new T();
        }
        catch (ArgumentException error)
        {
            Debug.LogError("[Save] " + key + " is unreadable; starting fresh. " + error.Message);
            return new T();
        }
    }

    public static void Save<T>(string key, T value)
    {
        PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
        PlayerPrefs.Save();
    }
}
