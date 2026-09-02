using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Базовый класс для всех 3D-уровней тренажёра.
/// Реализует конечный автомат заданий, обработку ошибок, сбор метрик,
/// завершение уровня и показ итогового экрана.
/// </summary>
public abstract class BaseLevel3DManager : MonoBehaviour
{
    public enum TaskState
    {
        WaitingToStart,
        Task1, Task2, Task3, Task4, Task5, Task6,
        LevelComplete
    }

    [Header("Базовые ссылки")]
    [SerializeField] protected FirstPersonController playerController;
    [SerializeField] protected GazeDetector gazeDetector;
    [SerializeField] protected TrafficLightChangeOfColor trafficLight;
    [SerializeField] protected UIFeedbackController feedbackUI;
    [SerializeField] protected CameraHintController cameraHint;

    [SerializeField] protected bool logDebug = true;   // включать/выключать отладочные сообщения

    protected TaskState currentState = TaskState.WaitingToStart;
    protected int errorCountCurrentTask = 0;
    protected bool isTransitioning = false;

    // Количество попыток прохождения уровня (перезапусков из-за трёх ошибок)
    protected int levelAttempts = 0;

    // Коллектор метрик для текущего уровня
    protected LevelMetricsCollector metricsCollector = new LevelMetricsCollector();

    protected float levelStartTime;

    protected virtual void Start()
    {
        if (currentState == TaskState.WaitingToStart)
        {
            levelAttempts = 0;
            metricsCollector.Reset();
        }
        StartCoroutine(StartLevelSequence());
    }

    /// <summary>
    /// Начальная последовательность уровня: пауза перед стартом и запуск первого задания.
    /// Может быть переопределена наследниками.
    /// </summary>
    protected virtual IEnumerator StartLevelSequence()
    {
        yield return new WaitForSeconds(0.5f);

        levelStartTime = Time.time;
        NextTask();
    }

    /// <summary>
    /// Переключает конечный автомат на следующее задание.
    /// Если задания закончились – вызывает OnLevelComplete.
    /// </summary>
    protected void NextTask()
    {
        if (isTransitioning) return;

        CleanupCurrentTask();
        errorCountCurrentTask = 0;

        switch (currentState)
        {
            case TaskState.WaitingToStart: currentState = TaskState.Task1; break;
            case TaskState.Task1: currentState = TaskState.Task2; break;
            case TaskState.Task2: currentState = TaskState.Task3; break;
            case TaskState.Task3: currentState = TaskState.Task4; break;
            case TaskState.Task4: currentState = TaskState.Task5; break;
            case TaskState.Task5: currentState = TaskState.Task6; break;
            case TaskState.Task6: currentState = TaskState.LevelComplete; break;
        }

        if (currentState == TaskState.LevelComplete)
        {
            OnLevelComplete();
            return;
        }

        StartTask();
    }

    /// <summary>
    /// Запускает конкретное задание (реализуется наследником).
    /// </summary>
    protected abstract void StartTask();

    /// <summary>
    /// Очищает состояние текущего задания (реализуется наследником).
    /// Вызывается перед сменой задания или уровнем.
    /// </summary>
    protected abstract void CleanupCurrentTask();

    /// <summary>
    /// Обрабатывает ошибку в текущем задании.
    /// При трёх ошибках вызывает перезапуск уровня.
    /// </summary>
    protected void HandleTaskError()
    {
        errorCountCurrentTask++;
        if (errorCountCurrentTask >= 3)
        {
            RestartLevel();
        }
        else if (errorCountCurrentTask == 2)
        {
            OnSecondError();   // визуальная подсказка (по умолчанию ничего)
        }
    }

    /// <summary>
    /// Регистрирует ошибку в коллекторе метрик.
    /// </summary>
    protected void RegisterError(string errorType)
    {
        metricsCollector.RegisterError(errorType, Time.time - levelStartTime);
    }

    /// <summary>
    /// Перезапускает уровень (при трёх ошибках). Перезагружает текущую сцену.
    /// </summary>
    protected virtual void RestartLevel()
    {
        levelAttempts++;
        if (logDebug)
            Debug.Log("[BaseLevel3DManager] Уровень перезапускается из-за трёх ошибок.");

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Завершает уровень: собирает метрики, показывает финальный экран.
    /// </summary>
    protected virtual void OnLevelComplete()
    {
        float completionTime = Time.time - levelStartTime;

        // Формируем данные уровня через коллектор метрик
        SessionLevelData levelData = metricsCollector.CreateSessionLevelData(
            levelId: LevelManager.Instance.CurrentLevel,
            stage: "3d",
            success: true,
            levelStartTime: levelStartTime,
            levelEndTime: Time.time
        );
        SessionManager.Instance?.AddLevelResult(levelData);

        // Блокируем управление
        if (playerController != null)
            playerController.DisableMovement();

        // Останавливаем голос
        if (feedbackUI != null)
            feedbackUI.StopVoice();

        // Показываем финальный экран
        bool isLast = LevelManager.Instance.IsLastLevel;  
        feedbackUI?.ShowFinalScreen3D(levelData.stars, completionTime, isLast);
    }

    /// <summary>
    /// Метод, вызываемый при второй ошибке в одном задании.
    /// </summary>
    protected virtual void OnSecondError() { }

    //Виртуальные методы обработки зон (переопределяются в наследниках)
    public virtual void OnPlayerEnterWaitingZone() { }
    public virtual void OnPlayerExitWaitingZone() { }
    public virtual void OnPlayerEnterRoad() { }
    public virtual void OnPlayerEnterRoadTask6() { }
    public virtual void OnPlayerExitRoadTask6() { }
    public virtual void OnPlayerStepOffCrosswalk() { }
    public virtual void OnPlayerReachFinishZone() { }
    public virtual void OnPlayerEnterCrosswalk() { }
    public virtual void OnPlayerExitCrosswalk() { }
}