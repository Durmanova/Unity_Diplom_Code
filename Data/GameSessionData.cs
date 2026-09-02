using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Хранит все данные одной игровой сессии: идентификатор ребёнка,
/// время начала/окончания, список пройденных уровней и агрегированные метрики.
/// Используется SessionManager для сбора и сохранения результатов.
/// Сериализуется в JSON для отправки на сервер.
/// </summary>
[System.Serializable]
public class GameSessionData
{
    public string childId;              // идентификатор ребёнка (в демо – константа DEMO_CHILD_ID)
    public float sessionStartTime;      // время начала сессии (Time.time)
    public float sessionEndTime;        // время окончания сессии (заполняется в Finish())

    /// <summary>Общая длительность сессии в секундах.</summary>
    public float totalTime => sessionEndTime - sessionStartTime;

    /// <summary>Количество уровней, которые были хотя бы начаты (уникальные levelId).</summary>
    public int levelsAttempted => levels.Select(l => l.levelId).Distinct().Count();

    /// <summary>Количество полностью завершённых 3D-уровней (уникальные levelId).</summary>
    public int levelsCompleted => levels
        .Where(l => l.stage == "3d" && l.success)
        .Select(l => l.levelId)
        .Distinct()
        .Count();

    /// <summary>Список всех пройденных этапов (подготовительных и 3D).</summary>
    public List<SessionLevelData> levels = new List<SessionLevelData>();

    /// <summary>
    /// Добавляет результат одного этапа в общий список.
    /// </summary>
    /// <param name="level">Данные этапа (подготовительного или 3D).</param>
    public void AddLevel(SessionLevelData level)
    {
        levels.Add(level);
    }

    /// <summary>
    /// Фиксирует время окончания сессии.
    /// Вызывается при завершении всей сессии.
    /// </summary>
    public void Finish()
    {
        sessionEndTime = Time.time;
    }

    /// <summary>
    /// Преобразует объект сессии в JSON-строку (для сохранения или отправки на сервер).
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }
}