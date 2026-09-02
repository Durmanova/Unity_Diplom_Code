using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Обрабатывает последовательные (многошаговые) задания.
/// Подписывается на глобальные клики и проверяет, соответствует ли нажатый объект
/// текущему шагу. При правильном выборе переходит к следующему шагу, при неправильном —
/// сообщает об ошибке через Stage2DManager.
/// Когда все шаги выполнены, вызывает CompleteTask и удаляет себя.
/// </summary>
public class SequentialTaskHandler : MonoBehaviour
{
    // Список целевых объектов, которые нужно нажимать по порядку
    private List<string> targets;

    // Текущий шаг (индекс в targets)
    private int currentStep;

    // Идентификатор задания, для которого создан обработчик
    private int taskId;

    // Флаг активности: если false, обработчик больше не реагирует на клики
    private bool isActive = false;

    /// <summary>
    /// Инициализирует обработчик.
    /// </summary>
    /// <param name="taskId">ID задания (используется для обратной связи со Stage2DManager).</param>
    /// <param name="sequentialTargets">Список имён объектов, которые нужно нажимать по порядку.</param>
    public void Initialize(int taskId, List<string> sequentialTargets)
    {
        this.taskId = taskId;
        this.targets = new List<string>(sequentialTargets);
        this.currentStep = 0;
        this.isActive = true;

        // Подписываемся на глобальные клики
        ClickHandler.OnObjectClicked += OnObjectClicked;

        // Показываем подсказку с первым целевым объектом
        if (targets.Count > 0)
            UIFeedbackController.Instance?.ShowHint("Нажми на " + targets[0]);
    }

    /// <summary>
    /// Обрабатывает клик по объекту.
    /// Если имя нажатого объекта совпадает с ожидаемым для текущего шага,
    /// переходит к следующему шагу; иначе регистрирует ошибку.
    /// </summary>
    private void OnObjectClicked(string clickedObjectName)
    {
        if (!isActive) return;
        if (currentStep >= targets.Count) return; // все шаги уже выполнены

        if (clickedObjectName == targets[currentStep])
        {
            // Правильный выбор
            UIFeedbackController.Instance?.ShowSuccess();
            currentStep++;

            if (currentStep >= targets.Count)
            {
                // Все шаги выполнены – сообщаем Stage2DManager и удаляем себя
                Stage2DManager.Instance.CompleteTask(taskId);
                Cleanup();
            }
            else
            {
                // Переход к следующему шагу – выводим новую подсказку
                string nextHint = "Теперь нажми на " + targets[currentStep];
                UIFeedbackController.Instance?.ShowHint(nextHint);
            }
        }
        else
        {
            // Ошибка: сообщаем Stage2DManager
            Stage2DManager.Instance.ReportErrorForTask(taskId);

            // Визуально выделяем правильный объект (если доступен DynamicTaskBuilder)
            var builder = FindObjectOfType<DynamicTaskBuilder>();
            if (builder != null)
                builder.HighlightCorrectObject(targets[currentStep]);
        }
    }

    /// <summary>
    /// Отписывается от события кликов и уничтожает компонент.
    /// </summary>
    private void Cleanup()
    {
        isActive = false;
        ClickHandler.OnObjectClicked -= OnObjectClicked;
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (isActive) Cleanup(); // на случай, если объект уничтожается вне Cleanup
    }
}