using System.Collections.Generic;

/// <summary>
/// Хранит метрики одного этапа уровня (подготовительного "preparation" или основного "3d").
/// Содержит временные отметки, количество ошибок, попыток и звёзды.
/// Используется в составе GameSessionData.
/// </summary>
[System.Serializable]
public class SessionLevelData
{
    public int levelId;                // номер уровня (1-4)
    public string stage;               // этап: "preparation" или "3d"
    public float levelStartTime;       // время начала этапа (Time.time)
    public float levelEndTime;         // время окончания этапа (Time.time)

    /// <summary>Длительность этапа в секундах.</summary>
    public float levelTotalTime => levelEndTime - levelStartTime;

    public int attempts;               // количество возвратов/попыток в рамках этапа
    public bool success;               // признак успешного завершения этапа
    public int stars;                  // количество звёзд (только для подготовительного этапа)
    public int totalErrors;            // общее количество ошибок на этапе
    public List<SessionErrorData> errors; // детализированные ошибки (тип и время)
}