using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Динамически строит UI для текущего задания подготовительного этапа.
/// Подписывается на события <see cref="Stage2DManager"/> и при смене задания
/// очищает панель, создаёт кликабельные объекты из данных TaskData,
/// а также управляет анимациями (например, сменой сигналов светофора).
/// </summary>
public class DynamicTaskBuilder : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform taskPanel;      // панель, куда добавляются объекты задания
    [SerializeField] private TextMeshProUGUI instructionText; // текстовое поле для инструкций

    [Header("Resources")]
    [SerializeField] private string spritesPath = "2D/";   // папка внутри Resources, откуда грузятся спрайты

    // Список созданных объектов для последующей очистки
    private List<GameObject> currentObjects = new List<GameObject>();

    /// <summary>Флаг, указывающий, что сейчас проигрывается анимация (для блокировки кликов).</summary>
    public bool IsAnimating { get; private set; }

    private void Start()
    {
        if (Stage2DManager.Instance == null)
        {
            Debug.LogError("Stage2DManager.Instance не найден!");
            return;
        }

        // Подписываемся на события смены задания и завершения всех заданий
        Stage2DManager.Instance.onTaskChanged.AddListener(BuildTask);
        Stage2DManager.Instance.onAllTasksCompleted.AddListener(OnAllTasksCompleted);

        // Если задание уже загружено (например, при первом запуске сцены), строим его сразу
        if (Stage2DManager.Instance.CurrentTask != null)
        {
            BuildTask(Stage2DManager.Instance.CurrentTaskIndex, Stage2DManager.Instance.CurrentTask);
        }
    }

    /// <summary>
    /// Очищает панель и строит новое задание на основе переданных данных.
    /// </summary>
    private void BuildTask(int taskIndex, TaskData task)
    {
        ClearTaskPanel();

        if (instructionText != null)
        {
            instructionText.text = task.instructionText;
        }

        // Озвучиваем инструкцию голосом (если аудиофайл задан)
        UIFeedbackController.Instance?.PlayInstructionAudio(task.audioInstruction);

        if (task.hasAnimation)
        {
            // Задание с анимацией – запускаем специальную корутину
            StartCoroutine(PlayTrafficLightAnimation(task));
        }
        else
        {
            // Обычное задание – просто создаём кликабельные объекты
            foreach (TaskObjectInfo objInfo in task.objectsToShow)
                CreateClickableObject(objInfo);

            Debug.Log($"[DynamicTaskBuilder] Задание {taskIndex + 1} построено. Объектов: {task.objectsToShow.Count}");
        }
    }

    /// <summary>
    /// Создаёт один кликабельный объект (с кнопкой).
    /// </summary>
    private void CreateClickableObject(TaskObjectInfo objInfo)
    {
        CreateVisualObject(objInfo, withButton: true);
    }

    /// <summary>
    /// Загружает спрайт из папки Resources по имени.
    /// </summary>
    private Sprite LoadSprite(string spriteName)
    {
        string fullPath = spritesPath + spriteName;
        return Resources.Load<Sprite>(fullPath);
    }

    /// <summary>
    /// Уничтожает все объекты, созданные для текущего задания.
    /// </summary>
    private void ClearTaskPanel()
    {
        foreach (GameObject obj in currentObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        currentObjects.Clear();
    }

    /// <summary>
    /// Вызывается, когда все задания подготовительного этапа завершены.
    /// Очищает панель и выводит финальное сообщение.
    /// </summary>
    private void OnAllTasksCompleted()
    {
        Debug.Log("Все задания пройдены! Можно показать панель с звёздами.");
        if (instructionText != null)
            instructionText.text = "Молодец! Уровень пройден!";
        ClearTaskPanel();
    }

    private void OnDestroy()
    {
        // Отписываемся от события, чтобы избежать утечек памяти
        if (Stage2DManager.Instance != null)
            Stage2DManager.Instance.onTaskChanged.RemoveListener(BuildTask);
    }

    /// <summary>
    /// Визуально выделяет правильный объект (например, при второй ошибке).
    /// Найденный объект временно окрашивается в жёлтый цвет и возвращается к исходному.
    /// </summary>
    public void HighlightCorrectObject(string targetName)
    {
        GameObject correctObj = currentObjects.Find(obj => obj.name == targetName);
        if (correctObj != null)
        {
            Image img = correctObj.GetComponent<Image>();
            if (img != null)
            {
                StartCoroutine(FlashImage(img));
            }
        }
    }

    /// <summary>
    /// Корутина мигания: на короткое время меняет цвет изображения на жёлтый,
    /// затем возвращает исходный. Используется для визуальной подсказки.
    /// </summary>
    private IEnumerator FlashImage(Image img)
    {
        Color originalColor = img.color;
        img.color = Color.yellow;
        yield return new WaitForSeconds(0.8f);
        img.color = originalColor;
    }

    /// <summary>
    /// Корутина анимации светофора для задания с hasAnimation.
    /// Создаёт некликабельные объекты, поочерёдно активирует красный и зелёный сигналы,
    /// затем заменяет их на кликабельные и возвращает управление.
    /// </summary>
    private IEnumerator PlayTrafficLightAnimation(TaskData task)
    {
        IsAnimating = true;

        // Создаём все объекты задания без кнопок (для анимации)
        List<GameObject> animatedObjects = new List<GameObject>();
        foreach (TaskObjectInfo objInfo in task.objectsToShow)
        {
            GameObject obj = CreateVisualObject(objInfo, withButton: false);
            animatedObjects.Add(obj);
        }

        // Находим объекты сигналов по имени
        GameObject redLight = animatedObjects.Find(obj => obj.name == "RedLight");
        GameObject greenLight = animatedObjects.Find(obj => obj.name == "GreenLight");

        // Фаза 1: горит красный, зелёный тусклый
        SetLightActive(redLight, true);
        SetLightActive(greenLight, false);
        if (instructionText != null) instructionText.text = "Красный – СТОЙ";
        yield return new WaitForSeconds(3f);

        // Фаза 2: горит зелёный, красный тусклый
        SetLightActive(redLight, false);
        SetLightActive(greenLight, true);
        if (instructionText != null) instructionText.text = "Зелёный – ИДИ";
        yield return new WaitForSeconds(3f);

        // Удаляем анимационные объекты
        foreach (GameObject obj in animatedObjects)
            Destroy(obj);

        // Создаём нормальные кликабельные объекты для выполнения задания
        foreach (TaskObjectInfo objInfo in task.objectsToShow)
            CreateClickableObject(objInfo);

        // Возвращаем исходную инструкцию
        if (instructionText != null) instructionText.text = task.instructionText;

        IsAnimating = false;
        Debug.Log("[DynamicTaskBuilder] Анимация завершена, объекты готовы к кликам.");
    }

    /// <summary>
    /// Создаёт визуальный объект (Image) на панели. Если withButton == true,
    /// добавляет компоненты Button и ClickHandler для интерактивности.
    /// </summary>
    private GameObject CreateVisualObject(TaskObjectInfo objInfo, bool withButton = true)
    {
        GameObject go = new GameObject(objInfo.objectName);
        go.transform.SetParent(taskPanel, false);

        // Настройка RectTransform
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = objInfo.position;
        rect.sizeDelta = objInfo.size;

        // Добавляем Image и загружаем спрайт
        Image image = go.AddComponent<Image>();
        Sprite sprite = LoadSprite(objInfo.spriteName);
        if (sprite != null)
            image.sprite = sprite;
        else
            Debug.LogWarning($"Спрайт '{objInfo.spriteName}' не найден! Создан пустой Image.");

        if (withButton)
        {
            Button button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ClickHandler handler = go.AddComponent<ClickHandler>();
            handler.SetObjectName(objInfo.objectName);
        }

        currentObjects.Add(go);
        return go;
    }

    /// <summary>
    /// Устанавливает визуальное состояние сигнала светофора.
    /// Активный – белый, неактивный – тёмно-серый (тусклый).
    /// </summary>
    private void SetLightActive(GameObject lightObj, bool isActive)
    {
        if (lightObj == null) return;
        Image img = lightObj.GetComponent<Image>();
        if (img != null)
        {
            img.color = isActive ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }
}