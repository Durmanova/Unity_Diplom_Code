using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Обрабатывает клики по пустому месту (мимо всех интерактивных объектов).
/// Если ребенок нажимает на панель, но не на кликабельный объект, это считается промахом.
/// Используется для регистрации ошибок типа "miss" в метриках.
/// Компонент должен быть размещён на родительской панели с коллайдером/raycast target.
/// </summary>
public class MissClickHandler : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// Вызывается при клике по панели (или её дочерним элементам).
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Проверяем, что клик был именно по панели, а не по дочернему объекту
        if (eventData.pointerPress != gameObject)
            return;

        // Блокировка на время перехода между заданиями
        if (Stage2DManager.Instance == null || Stage2DManager.Instance.IsTransitioning)
            return;

        // Блокировка на время анимации (например, светофора)
        DynamicTaskBuilder builder = FindObjectOfType<DynamicTaskBuilder>();
        if (builder != null && builder.IsAnimating)
            return;

        // Регистрируем промах: передаём тип ошибки "miss" в Stage2DManager
        Stage2DManager.Instance.RegisterMissClick();
    }
}