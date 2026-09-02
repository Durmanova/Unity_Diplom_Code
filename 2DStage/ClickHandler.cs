using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Обрабатывает клики по кликабельным объектам заданий.
/// При нажатии генерирует статическое событие <see cref="OnObjectClicked"/>,
/// передавая имя нажатого объекта. 
/// </summary>
[RequireComponent(typeof(Button))]
public class ClickHandler : MonoBehaviour
{
    // Имя объекта, по которому системы определяют, что было нажато
    private string objectName;

    // Ссылка на компонент Button, к которому прикреплён обработчик
    private Button button;

    /// <summary>
    /// Статическое событие, вызываемое при клике на любой объект с ClickHandler.
    /// Параметр: имя нажатого объекта.
    /// </summary>
    public static event System.Action<string> OnObjectClicked;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    /// <summary>
    /// Вызывается при нажатии на кнопку. Проверяет, не заблокированы ли клики,
    /// и генерирует событие OnObjectClicked.
    /// </summary>
    private void OnClick()
    {
        // Блокировка на время перехода между заданиями (чтобы нельзя было кликать во время анимации смены)
        if (Stage2DManager.Instance != null && Stage2DManager.Instance.IsTransitioning)
            return;

        // Блокировка на время анимации светофора (задание с hasAnimation)
        DynamicTaskBuilder builder = FindObjectOfType<DynamicTaskBuilder>();
        if (builder != null && builder.IsAnimating)
            return;

        // Генерируем событие клика – оно обрабатывается подписчиками
        OnObjectClicked?.Invoke(objectName);
    }

    /// <summary>
    /// Устанавливает имя объекта (вызывается из DynamicTaskBuilder при создании).
    /// </summary>
    public void SetObjectName(string name)
    {
        objectName = name;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }
}