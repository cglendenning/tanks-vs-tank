using System;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using TreadShred.Analytics;
using UnityEngine;

/// <summary>
/// D-021: sends game events to Firebase Analytics. Events wait in a short
/// queue until Firebase is ready; analytics never blocks or breaks gameplay.
/// </summary>
public static class TreadShredAnalytics
{
    private const int MaxQueued = 100;
    private static readonly Queue<AnalyticsEvent> queued = new Queue<AnalyticsEvent>();
    private static bool ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.Result != DependencyStatus.Available)
            {
                Debug.LogWarning("[Analytics] Firebase unavailable: " + (task.Exception?.Message ?? task.Result.ToString()));
                return;
            }
            ready = true;
            while (queued.Count > 0)
                Send(queued.Dequeue());
        });
    }

    public static void Log(AnalyticsEvent analyticsEvent)
    {
        if (!ready)
        {
            if (queued.Count < MaxQueued)
                queued.Enqueue(analyticsEvent);
            return;
        }
        Send(analyticsEvent);
    }

    public static void SetUserProperty(string name, string value)
    {
        if (!ready)
            return;
        try
        {
            FirebaseAnalytics.SetUserProperty(name, value);
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Analytics] user property " + name + " failed: " + error.Message);
        }
    }

    private static void Send(AnalyticsEvent analyticsEvent)
    {
        try
        {
            var parameters = analyticsEvent.Parameters.Select(ToParameter).ToArray();
            FirebaseAnalytics.LogEvent(analyticsEvent.Name, parameters);
        }
        catch (Exception error)
        {
            Debug.LogWarning("[Analytics] " + analyticsEvent.Name + " failed: " + error.Message);
        }
    }

    private static Parameter ToParameter(KeyValuePair<string, object> entry)
    {
        switch (entry.Value)
        {
            case long number:
                return new Parameter(entry.Key, number);
            case double fraction:
                return new Parameter(entry.Key, fraction);
            default:
                return new Parameter(entry.Key, Convert.ToString(entry.Value));
        }
    }
}
