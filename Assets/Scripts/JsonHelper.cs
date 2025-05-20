using UnityEngine;
using System;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string wrappedJson = "{\"array\":" + json + "}";
        return JsonUtility.FromJson<Wrapper<T>>(wrappedJson).array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}