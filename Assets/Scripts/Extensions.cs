using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extensions
{
    public static Vector3 ForceNormalizeVector(this Vector3 vec)
    {
        if (vec.magnitude > 1) return vec.normalized;
        return vec;
    }

    public static T GetLatestMessage<T>(this SortedDictionary<long, T> dict, double timestamp)
    {
        var stamp = dict.Keys.FirstOrDefault(k => k > timestamp);

        if (stamp == 0) return dict[dict.Keys.Max()];

        return dict[stamp];
    }
}