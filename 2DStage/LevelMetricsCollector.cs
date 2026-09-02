using System.Collections.Generic;

/// <summary>
/// Собирает и обрабатывает метрики одного уровня (подготовительного или 3D).
/// Отвечает за регистрацию ошибок, подсчёт звёзд и формирование данных для сохранения.
/// Не зависит от Unity API, время передаётся явно.
/// </summary>
public class LevelMetricsCollector
{
    // Накопленные ошибки с типами и временем от начала уровня
    public List<SessionErrorData> Errors { get; private set; } = new List<SessionErrorData>();

    // Общее количество ошибок за уровень
    public int TotalErrors { get; private set; }

    // Количество возвратов к предыдущему заданию
    public int ReturnCount { get; private set; }

    /// <summary>
    /// Регистрирует ошибку. Увеличивает счётчики и добавляет запись в список.
    /// </summary>
    /// <param name="errorType">Тип ошибки (например, "wrong_object", "miss").</param>
    /// <param name="errorTime">Время с начала уровня, когда произошла ошибка.</param>
    public void RegisterError(string errorType, float errorTime)
    {
        Errors.Add(new SessionErrorData
        {
            errorType = errorType,
            errorTime = errorTime
        });
        TotalErrors++;
    }

    /// <summary>
    /// Увеличивает счётчик возвратов к предыдущему заданию.
    /// </summary>
    public void RegisterReturn()
    {
        ReturnCount++;
    }

    /// <summary>
    /// Рассчитывает количество звёзд по ТЗ:
    /// 0 ошибок – 5 звёзд, 1–2 – 4, 3–4 – 3, 5+ – 2.
    /// </summary>
    public int CalculateStars()
    {
        if (TotalErrors == 0) return 5;
        if (TotalErrors >= 1 && TotalErrors <= 2) return 4;
        if (TotalErrors >= 3 && TotalErrors <= 4) return 3;
        return 2;
    }

    /// <summary>
    /// Формирует объект SessionLevelData для текущего уровня.
    /// </summary>
    /// <param name="levelId">Номер уровня.</param>
    /// <param name="stage">Этап ("preparation" или "3d").</param>
    /// <param name="success">Успешно ли завершён этап.</param>
    /// <param name="levelStartTime">Время начала этапа.</param>
    /// <param name="levelEndTime">Время окончания этапа.</param>
    public SessionLevelData CreateSessionLevelData(int levelId, string stage, bool success, float levelStartTime, float levelEndTime)
    {
        return new SessionLevelData
        {
            levelId = levelId,
            stage = stage,
            levelStartTime = levelStartTime,
            levelEndTime = levelEndTime,
            attempts = ReturnCount,
            success = success,
            stars = CalculateStars(),
            totalErrors = TotalErrors,
            errors = new List<SessionErrorData>(Errors)
        };
    }
    /// <summary>
    /// Полностью сбрасывает накопленные метрики: ошибки, количество возвратов.
    /// Используется перед началом нового уровня или этапа.
    /// </summary>
    public void Reset()
    {
        Errors.Clear();
        TotalErrors = 0;
        ReturnCount = 0;
    }
}