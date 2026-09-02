using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class TaskLoader
{
    public static List<TaskData> LoadFromJSON(string filePath)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(filePath);
        if (jsonFile == null) return null;
        var wrapper = JsonUtility.FromJson<TaskWrapper>(jsonFile.text);
        return wrapper.tasks;
    }

    [System.Serializable]
    private class TaskWrapper { public List<TaskData> tasks; }
}
