using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Глобальный менеджер уровней и переходов между сценами.
/// Отвечает за:
/// - хранение текущего номера уровня;
/// - загрузку подготовительной (2D) сцены;
/// - загрузку основной (3D) сцены после завершения подготовки;
/// - переход к следующему уровню или возврат в меню.
/// Является синглтоном и создаётся автоматически при старте игры.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    // Время начала 3D-уровня (используется для подсчёта метрик)
    private float threeDLevelStartTime;

    /// <summary>Возвращает true, если текущий уровень последний из доступных.</summary>
    public bool IsLastLevel => CurrentLevel >= levelSceneNames.Length - 1;

    /// <summary>
    /// Создаёт LevelManager и SessionManager при старте игры (до загрузки первой сцены).
    /// Гарантирует, что менеджеры будут существовать на протяжении всей сессии.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("LevelManager");
            Instance = go.AddComponent<LevelManager>();
            DontDestroyOnLoad(go);
            Instance.SetDefaultLevel(1); // уровень по умолчанию для тестов
            Debug.Log("LevelManager создан автоматически через RuntimeInitializeOnLoadMethod.");
        }

        if (SessionManager.Instance == null)
        {
            GameObject go = new GameObject("SessionManager");
            go.AddComponent<SessionManager>();
            DontDestroyOnLoad(go);
        }
    }

    [Header("Scene Names")]
    [SerializeField] private string preparationSceneName = "Preparation2D";
    [SerializeField] private string mainMenuSceneName = "Main";
    [SerializeField]
    private string[] levelSceneNames = new string[]
    {
        "",
        "Level1_Simple"   // для демо-версии оставлен только один уровень
    };

    /// <summary>Текущий номер уровня (1-based).</summary>
    public int CurrentLevel { get; private set; } = 1;

    // Флаг, показывающий, что мы проходим уровни последовательно
    private bool isLevelSequenceActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Запускает подготовительный этап для указанного уровня.
    /// Загружает сцену с 2D-заданиями (PECS).
    /// </summary>
    /// <param name="levelNumber">Номер уровня (от 1 до количества доступных).</param>
    public void StartLevel(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > levelSceneNames.Length - 1)
        {
            Debug.LogError($"Некорректный номер уровня: {levelNumber}");
            return;
        }

        CurrentLevel = levelNumber;
        isLevelSequenceActive = true;
        Debug.Log($"=== Запуск уровня {CurrentLevel}. Загружаем подготовительный этап: {preparationSceneName} ===");
        SceneManager.LoadScene(preparationSceneName);
    }

    /// <summary>
    /// Вызывается из Stage2DManager после успешного завершения всех заданий подготовительного этапа.
    /// Загружает основную 3D-сцену текущего уровня.
    /// </summary>
    public void LoadMain3DLevel()
    {
        threeDLevelStartTime = Time.time; // запоминаем время старта 3D-уровня
        string sceneName = levelSceneNames[CurrentLevel];
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"Не задано имя сцены для уровня {CurrentLevel}");
            return;
        }

        Debug.Log($"Подготовительный этап завершён. Загружаем основную сцену: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Вызывается из 3D-сцены, когда уровень пройден.
    /// </summary>
    public void CompleteCurrent3DLevel()
    {
        if (!isLevelSequenceActive)
        {
            Debug.LogWarning("CompleteCurrent3DLevel вызван вне последовательности уровней.");
            return;
        }

        // Если есть следующий уровень – запускаем его подготовительный этап.
        if (!IsLastLevel)
        {
            GoToNextLevel();
        }
        else
        {
            Debug.Log("Все уровни пройдены!");
            isLevelSequenceActive = false;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    /// <summary>
    /// Возвращает имя JSON-файла с заданиями для подготовительного этапа текущего уровня.
    /// Например, "prep_level1".
    /// </summary>
    public string GetPreparationTasksFileName()
    {
        return $"prep_level{CurrentLevel}";
    }

    /// <summary>
    /// Возвращает путь к JSON-файлу внутри Resources.
    /// Например, "Data/prep_level1".
    /// </summary>
    public string GetPreparationTasksFilePath()
    {
        return $"Data/{GetPreparationTasksFileName()}";
    }

    /// <summary>
    /// Устанавливает уровень по умолчанию (используется при автоматическом создании менеджера).
    /// </summary>
    public void SetDefaultLevel(int level)
    {
        CurrentLevel = level;
        isLevelSequenceActive = true;
    }

    /// <summary>
    /// Переходит к следующему уровню: увеличивает номер и загружает подготовительную сцену.
    /// Вызывается по кнопке «Следующий уровень» на финальном экране.
    /// </summary>
    public void GoToNextLevel()
    {
        if (CurrentLevel < levelSceneNames.Length - 1)
        {
            CurrentLevel++;
            isLevelSequenceActive = true;
            SceneManager.LoadScene(preparationSceneName);
        }
    }

    /// <summary>
    /// Завершает текущую сессию: финализирует метрики, разблокирует курсор
    /// и загружает сцену результатов.
    /// </summary>
    public void EndSession()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ВАЖНО: замените FinishSessionAsync() на FinishSession() или добавьте асинхронный метод
        SessionManager.Instance?.FinishSessionAsync();

        SceneManager.LoadScene("SessionResult");
    }
}