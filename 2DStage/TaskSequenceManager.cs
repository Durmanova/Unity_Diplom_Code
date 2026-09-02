using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет последовательностью заданий в рамках одного уровня.
/// Не зависит от UI и метрик, только переключает задания и уведомляет о смене.
/// </summary>
public class TaskSequenceManager
{
    // Список всех заданий уровня
    private List<TaskData> tasks;

    // Текущий индекс задания
    public int CurrentTaskIndex { get; private set; }

    // Текущее задание 
    public TaskData CurrentTask => tasks[CurrentTaskIndex];

    // События
    public event System.Action<int, TaskData> OnTaskChanged;  // (индекс, задание)
    public event System.Action OnAllTasksCompleted;

    // Активный обработчик многошагового задания
    private SequentialTaskHandler currentSequentialHandler;
    private GameObject hostGameObject; // объект, на котором создаём SequentialTaskHandler

    /// <summary>
    /// Инициализирует менеджер списком заданий.
    /// </summary>
    public void Initialize(List<TaskData> taskList, GameObject host)
    {
        tasks = taskList ?? new List<TaskData>();
        hostGameObject = host;
        CurrentTaskIndex = 0;
        currentSequentialHandler = null;
    }

    /// <summary>
    /// Загружает задание по индексу. Сбрасывает счётчик ошибок, создаёт
    /// обработчик многошагового задания при необходимости.
    /// </summary>
    public void LoadTask(int index)
    {
        // Удаляем старый обработчик
        if (currentSequentialHandler != null)
        {
            Object.Destroy(currentSequentialHandler);
            currentSequentialHandler = null;
        }

        // Если индекс вне диапазона – все задания пройдены
        if (index < 0 || index >= tasks.Count)
        {
            OnAllTasksCompleted?.Invoke();
            return;
        }

        CurrentTaskIndex = index;
        var task = tasks[index];

        // Если задание многошаговое – создаём обработчик
        if (task.sequentialTargets != null && task.sequentialTargets.Count > 0)
        {
            if (hostGameObject != null)
            {
                currentSequentialHandler = hostGameObject.AddComponent<SequentialTaskHandler>();
                currentSequentialHandler.Initialize(CurrentTaskIndex, task.sequentialTargets);
            }
        }

        // Уведомляем об изменении задания
        OnTaskChanged?.Invoke(CurrentTaskIndex, task);
    }

    /// <summary>
    /// Переходит к следующему заданию.
    /// </summary>
    public void MoveToNextTask()
    {
        LoadTask(CurrentTaskIndex + 1);
    }

    /// <summary>
    /// Возвращает true, если задание многошаговое.
    /// </summary>
    public bool IsCurrentTaskSequential()
    {
        return currentSequentialHandler != null && currentSequentialHandler.enabled;
    }

    /// <summary>
    /// Очищает состояние (вызывается при уничтожении менеджера или смене уровня).
    /// </summary>
    public void Cleanup()
    {
        if (currentSequentialHandler != null)
        {
            Object.Destroy(currentSequentialHandler);
            currentSequentialHandler = null;
        }
    }
}