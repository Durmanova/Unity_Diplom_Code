using System;
using UnityEngine;

/// <summary>
/// Описание одного UI-объекта (спрайта) для динамического создания на экране.
/// Используется в TaskData.objectsToShow.
/// Содержит имя, спрайт, позицию и размер.
/// </summary>
[Serializable]
public class TaskObjectInfo
{
    public string objectName;   // имя объекта (должно совпадать с correctTargetName или шагами sequentialTargets)
    public string spriteName;   // имя спрайта в Resources (без расширения)
    public Vector2 position;    // позиция на Canvas (в координатах anchoredPosition)
    public Vector2 size;        // размер объекта (ширина, высота)

    /// <summary>
    /// Конструктор для удобного создания объекта TaskObjectInfo.
    /// </summary>
    /// <param name="objectName">Имя объекта.</param>
    /// <param name="spriteName">Имя спрайта.</param>
    /// <param name="position">Позиция на Canvas.</param>
    /// <param name="size">Размер.</param>
    public TaskObjectInfo(string objectName, string spriteName, Vector2 position, Vector2 size)
    {
        this.objectName = objectName;
        this.spriteName = spriteName;
        this.position = position;
        this.size = size;
    }
}