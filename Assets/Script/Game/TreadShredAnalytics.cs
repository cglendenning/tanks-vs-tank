using System;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using TreadShred.Analytics;
using UnityEngine;

/// <summary>
/// D-021: sends game events to Firebase Analytics through AnalyticsDispatcher,
/// which queues until Firebase is ready and never lets a failure reach gameplay.
/// </summary>
public static class TreadShredAnalytics
{
    private static readonly AnalyticsDispatcher dispatcher = new AnalyticsDispatcher(Send, Debug.LogWarning);

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
            Debug.Log("[Analytics] Firebase ready; sending " + dispatcher.Queued + " queued events");
            dispatcher.MarkReady();
        });
    }

    public static void Log(AnalyticsEvent analyticsEvent)
    {
        dispatcher.Log(analyticsEvent);
    }

    public static void SetUserProperty(string name, string value)
    {
        if (!dispatcher.Ready)
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
        FirebaseAnalytics.LogEvent(analyticsEvent.Name, analyticsEvent.Parameters.Select(ToParameter).ToArray());
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
