using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Управляет подготовительным (2D) этапом уровня.
/// Загружает задания из JSON, обрабатывает ответы пользователя, считает ошибки,
/// управляет последовательностью заданий и собирает метрики.
/// Является синглтоном и существует только в сцене подготовки.
/// После завершения всех заданий передаёт результат в SessionManager и запускает 3D-уровень.
/// </summary>
public class Stage2DManager : MonoBehaviour
{
    //Singleton
    /// <summary>Глобальный доступ к менеджеру этапа.</summary>
    public static Stage2DManager Instance { get; private set; }

    //События
    /// <summary>Вызывается при откате к предыдущему заданию после трёх ошибок. Передаёт индекс задания.</summary>
    public UnityEvent<int> onTaskFailedAndReset = new UnityEvent<int>();

    /// <summary>Вызывается при смене текущего задания. Передаёт индекс и данные задания.</summary>
    public UnityEvent<int, TaskData> onTaskChanged = new UnityEvent<int, TaskData>();

    /// <summary>Вызывается, когда все задания подготовительного этапа завершены.</summary>
    public UnityEvent onAllTasksCompleted = new UnityEvent();

    //Метрики
    private LevelMetricsCollector metricsCollector = new LevelMetricsCollector();

    [SerializeField] private bool logDebug = true; // включать/выключать отладочные сообщения

    private int errorCountCurrentTask = 0; // количество ошибок в текущем задании

    /// <summary>Флаг, блокирующий обработку кликов во время смены задания или паузы.</summary>
    public bool IsTransitioning { get; private set; } = false;

    private float levelStartTime; // время начала выполнения уровня

    //Последовательность заданий
    private TaskSequenceManager sequenceManager = new TaskSequenceManager();

    /// <summary>Индекс текущего задания.</summary>
    public int CurrentTaskIndex => sequenceManager.CurrentTaskIndex;

    /// <summary>Текущее задание.</summary>
    public TaskData CurrentTask => sequenceManager.CurrentTask;

    // Инициализация
    private void Awake()
    {
        //  только один экземпляр в сцене
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator Start()
    {
        // Ждём инициализации LevelManager 
        float timer = 0f;
        while (LevelManager.Instance == null && timer < 2f)
        {
            yield return null;
            timer += Time.deltaTime;
        }

        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelManager.Instance всё ещё null после ожидания!");
            yield break;
        }

        // Загружаем задания из JSON, путь определяется уровнем
        string filePath = LevelManager.Instance.GetPreparationTasksFilePath();
        List<TaskData> loadedTasks = TaskLoader.LoadFromJSON(filePath);

        // Инициализируем менеджер последовательности заданий
        sequenceManager.Initialize(loadedTasks, gameObject);
        sequenceManager.OnTaskChanged += (idx, task) => onTaskChanged?.Invoke(idx, task);
        sequenceManager.OnAllTasksCompleted += OnAllTasksCompletedHandler;
        sequenceManager.LoadTask(0);

        // Сбрасываем подписки на случай повторного использования объекта
        onTaskFailedAndReset = new UnityEvent<int>();

        // Подписываемся на глобальные клики
        ClickHandler.OnObjectClicked += OnAnyObjectClicked;

        levelStartTime = Time.time;
    }

    //Обработка ответов

    /// <summary>
    /// Вызывается при правильном выборе объекта.
    /// Показывает поощрение и запускает отложенный переход к следующему заданию.
    /// </summary>
    public void OnCorrect()
    {
        if (IsTransitioning) return;

        if (logDebug) Debug.Log($"[Stage2DManager] Правильно! Задание {CurrentTaskIndex + 1} выполнено.");

        // Поощрение (текст + звук)
        UIFeedbackController.Instance?.ShowSuccess();

        IsTransitioning = true;
        StartCoroutine(NextTaskAfterDelay(2f)); // даём ребёнку увидеть сообщение
    }

    /// <summary>
    /// Корутина задержки перед переходом к следующему заданию.
    /// </summary>
    private IEnumerator NextTaskAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        IsTransitioning = false;
        sequenceManager.MoveToNextTask();
    }

    /// <summary>
    /// Обрабатывает неправильный выбор.
    /// Увеличивает счётчик ошибок, регистрирует ошибку в метриках, выводит подсказку.
    /// При трёх ошибках возвращает к предыдущему заданию.
    /// </summary>
    /// <param name="errorType">Тип ошибки (по умолчанию "wrong_object").</param>
    public void OnWrong(string errorType = "wrong_object")
    {
        errorCountCurrentTask++;
        metricsCollector.RegisterError(errorType, Time.time - levelStartTime);

        if (logDebug) Debug.Log($"[Stage2DManager] Ошибка! Ошибок в текущем задании: {errorCountCurrentTask}");

        // Повторяем инструкцию текущего задания
        TaskData currentTask = sequenceManager.CurrentTask;
        UIFeedbackController.Instance?.ShowRepeatInstruction(currentTask.instructionText, currentTask.audioInstruction);

        // Если набрано 3 ошибки – возврат к предыдущему заданию
        if (errorCountCurrentTask >= 3)
        {
            if (logDebug) Debug.Log($"[Stage2DManager] Три ошибки. Возврат к предыдущему заданию.");

            if (CurrentTaskIndex > 0)
            {
                errorCountCurrentTask = 0;
                sequenceManager.LoadTask(sequenceManager.CurrentTaskIndex - 1);
                metricsCollector.RegisterReturn();
                onTaskFailedAndReset?.Invoke(CurrentTaskIndex);
            }
            else
            {
                // Если это первое задание, перезагружаем его же
                errorCountCurrentTask = 0;
                sequenceManager.LoadTask(sequenceManager.CurrentTaskIndex);
                metricsCollector.RegisterReturn();
                if (logDebug) Debug.Log("[Stage2DManager] Первое задание: перезагрузка того же задания.");
            }
        }
        else if (errorCountCurrentTask == 2)
        {
            // На второй ошибке визуально выделяем правильный объект
            FindObjectOfType<DynamicTaskBuilder>()?.HighlightCorrectObject(CurrentTask.correctTargetName);
        }
    }

    /// <summary>
    /// Вызывается из SequentialTaskHandler, когда все шаги многошагового задания выполнены.
    /// Переходит к следующему заданию.
    /// </summary>
    public void CompleteTask(int taskId)
    {
        sequenceManager.MoveToNextTask();
    }

    /// <summary>
    /// Регистрирует ошибку в последовательном задании.
    /// Аналогичен OnWrong, но не создаёт дополнительной логики для многошаговых состояний.
    /// </summary>
    public void ReportErrorForTask(int taskId)
    {
        errorCountCurrentTask++;
        TaskData currentTask = sequenceManager.CurrentTask;
        UIFeedbackController.Instance?.ShowRepeatInstruction(currentTask.instructionText, currentTask.audioInstruction);

        metricsCollector.RegisterError("wrong_sequence", Time.time - levelStartTime);

        if (logDebug) Debug.Log($"[Stage2DManager] Ошибка в задании {taskId + 1}. Всего ошибок: {errorCountCurrentTask}");

        // При трёх ошибках – возврат к предыдущему заданию
        if (errorCountCurrentTask >= 3)
        {
            if (CurrentTaskIndex > 0)
            {
                errorCountCurrentTask = 0;
                sequenceManager.LoadTask(sequenceManager.CurrentTaskIndex - 1);
                onTaskFailedAndReset?.Invoke(CurrentTaskIndex);
            }
            else
            {
                errorCountCurrentTask = 0;
                sequenceManager.LoadTask(sequenceManager.CurrentTaskIndex);
                metricsCollector.RegisterReturn();
            }
        }
    }

    //Завершение этапа

    /// <summary>
    /// Обработчик завершения всех заданий.
    /// Считает звёзды, сохраняет результат в сессию и запускает переход к 3D-уровню.
    /// </summary>
    private void OnAllTasksCompletedHandler()
    {
        float completionTime = Time.time - levelStartTime;
        int stars = metricsCollector.CalculateStars();

        // Формируем данные этапа
        SessionLevelData levelData = metricsCollector.CreateSessionLevelData(
            levelId: LevelManager.Instance.CurrentLevel,
            stage: "preparation",
            success: true,
            levelStartTime: levelStartTime,
            levelEndTime: Time.time
        );
        SessionManager.Instance?.AddLevelResult(levelData);

        // Показываем финальный экран со звёздами
        UIFeedbackController.Instance?.ShowFinalScreen(stars, completionTime);

        // Отложенный переход к 3D-уровню
        StartCoroutine(ProceedToMainLevelAfterDelay(2.5f));
    }

    /// <summary>
    /// Корутина отложенного перехода к основной 3D-сцене.
    /// </summary>
    private IEnumerator ProceedToMainLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        LevelManager.Instance.LoadMain3DLevel();
    }

    // ---------- Обработка кликов ----------

    /// <summary>
    /// Обработчик глобального клика. Если задание многошаговое, делегирует обработку
    /// его обработчику. Для обычных заданий проверяет правильность ответа.
    /// </summary>
    private void OnAnyObjectClicked(string clickedName)
    {
        // Если есть активный обработчик последовательности, он сам обработает клик
        if (sequenceManager.IsCurrentTaskSequential())
            return;

        var task = sequenceManager.CurrentTask;
        if (clickedName == task.correctTargetName)
        {
            OnCorrect();
        }
        else
        {
            OnWrong();
        }
    }

    private void OnDestroy()
    {
        ClickHandler.OnObjectClicked -= OnAnyObjectClicked;
    }

    /// <summary>
    /// Блокирует/разблокирует интерактивность (например, из меню паузы).
    /// </summary>
    public void SetInteractionBlocked(bool blocked)
    {
        IsTransitioning = blocked;
    }

    /// <summary>
    /// Регистрирует промах – клик мимо всех объектов (для учёта ошибок в метриках).
    /// </summary>
    public void RegisterMissClick()
    {
        OnWrong("miss");
    }
}