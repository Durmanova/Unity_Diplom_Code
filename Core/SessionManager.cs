using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
/// <summary>
/// Отвечает за сбор и хранение метрик игровой сессии.
/// Сессия представляет собой набор пройденных уровней с информацией об ошибках,
/// времени выполнения и успешности. Данные могут сохраняться локально в JSON.
/// В последуещим планируется реализовать отправку на сервер.
/// Является синглтоном и живёт на протяжении всего приложения.
/// </summary>
public class SessionManager : MonoBehaviour
{
    //singltone
    public static SessionManager Instance { get; private set; }
    // Идентификатор ребёнка, используемый в демонстрационном режиме.
    // В полной версии должен подставляться реальный ID, полученный после авторизации.
    public const string DEMO_CHILD_ID = "demo_child_001";
    // Текущая игровая сессия (создаётся при старте сессии)
    private GameSessionData currentSession;
    // Возможность включения/отключения возможности сохранения игровых метрик локально 
    [Header("Local Save")]
    [SerializeField] private bool saveLevelsLocally = false; 

    private void Awake()
    {
        // Реализация паттерна Singleton: гарантируем единственный экземпляр
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        Application.quitting += OnApplicationQuitting;
    }

    /// <summary>
    /// Начинает новую игровую сессию для указанного ребёнка.
    /// Вызывается после успешной авторизации.
    /// </summary>
    /// <param name="childId">Уникальный идентификатор ребёнка.</param>
    public void StartSession(string childId)
    {
        currentSession = new GameSessionData
        {
            childId = childId,
            sessionStartTime = Time.time
        };
        Debug.Log($"[SessionManager] Сессия начата для ребёнка {childId}");
    }
    /// <summary>
    /// Запускает демонстрационную сессию с фиксированным ID ребёнка.
    /// Реализовано дл данной демонстрационной версии
    /// В полной версии этот метод будет заменён вызовом StartSession.
    /// </summary>
    public void StartDemoSession()
    {
        StartSession(DEMO_CHILD_ID);
    }

    /// <summary>
    /// Добавляет результат одного этапа в сессию.
    /// При saveLevelsLocally = true также сохраняет отдельный JSON-файл этапа. 
    /// </summary>
    /// <param name="levelData">Данные завершённого этапа (определенного уровня).</param>
    public void AddLevelResult(SessionLevelData levelData)
    {
        if (currentSession == null)
        {
            Debug.LogError("[SessionManager] Сессия не начата! Сначала вызовите StartSession.");
            return;
        }
        currentSession.AddLevel(levelData);
        Debug.Log($"[SessionManager] Добавлен результат этапа: {levelData.stage} уровня {levelData.levelId}, успех={levelData.success}");

    }

    /// <summary>
    /// Завершает сессию: финализирует время, сохраняет итоговый JSON локально и возвращает его для отправки на сервер.
    /// </summary>
    ///  <returns>JSON-представление всей сессии или null, если сессия не была начата.</returns>
    public async Task<string> FinishSessionAsync()
    {
        if (currentSession == null)
        {
            Debug.LogError("[SessionManager] Сессия не начата!");
            return null;
        }
        currentSession.Finish(); 
        string json = currentSession.ToJson();

        // Локальное сохранение полного отчёта
        if (saveLevelsLocally)
           await SaveSessionLocallyAsync(json);

        Debug.Log("[SessionManager] Сессия завершена. Итоговый JSON:\n" + json);
        return json;
    }

    /// <summary>
    /// Возвращает текущий объект сессии (может быть null, если сессия не начата).
    /// </summary>
    public GameSessionData GetCurrentSession() => currentSession;

    /// <summary>
    /// Сбрасывает сессию (например, при выходе в главное меню без завершения).
    /// </summary>
    public void ResetSession()
    {
        currentSession = null;
        Debug.Log("[SessionManager] Сессия сброшена.");
    }
    /// <summary>
    /// Асинхронно сохраняет полный отчёт о сессии локально.
    /// </summary>
    private async Task SaveSessionLocallyAsync(string fullJson)
    {
        string path = Path.Combine(Application.persistentDataPath, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        try
        {
            await File.WriteAllTextAsync(path, fullJson);
            Debug.Log($"[SessionManager] Итоговый JSON сессии сохранён: {path}");
        }
        catch (Exception ex) 
        {
            Debug.LogWarning($"[SessionManager] Не удалось сохранить сессию: {ex.Message}");
        }
     }
    /// <summary>
    /// Закрывает сессию при выходе из тренажера
    /// </summary>
    private void OnApplicationQuitting()
    {
        if (currentSession != null)
            FinishSessionAsync();
    }
}