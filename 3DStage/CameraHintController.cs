using System.Collections;
using UnityEngine;

/// <summary>
/// Управляет визуальными подсказками с использованием камеры.
/// Позволяет плавно перемещать камеру к целевой точке, удерживать её там,
/// возвращать обратно и управлять зумом.
/// Используется в 3D-уровнях для демонстрации правильных действий ребёнку.
/// </summary>
public class CameraHintController : MonoBehaviour
{
    [SerializeField] private float moveDuration = 1.5f;   // длительность перемещения к цели
    [SerializeField] private float holdDuration = 1.5f;   // пауза на цели
    [SerializeField] private float returnDuration = 1.5f; // длительность возврата

    // Сохранённые параметры для возврата камеры после режима "оставаться"
    private Transform originalParent;          // исходный родитель камеры
    private Vector3 originalLocalPos;          // исходная локальная позиция
    private Quaternion originalLocalRot;       // исходный локальный поворот
    private FirstPersonController savedController; // ссылка на контроллер, чтобы вернуть управление
    private bool isActive = false;              // флаг, что камера находится в режиме "оставаться"

    /// <summary>
    /// Перемещает камеру в заданную мировую точку и фиксирует взгляд на цели.
    /// Камера остаётся в этой точке до вызова ReturnCamera().
    /// Управление персонажем отключается.
    /// </summary>
    /// <param name="exactWorldPosition">Точка, куда переместить камеру.</param>
    /// <param name="lookTarget">Объект, на который должна смотреть камера.</param>
    /// <param name="controller">Контроллер игрока.</param>
    public IEnumerator PlayHintAndStayExact(Vector3 exactWorldPosition, Transform lookTarget, FirstPersonController controller)
    {
        Camera cam = controller.playerCamera;
        if (cam == null) yield break;

        Transform camTransform = cam.transform;

        // Сохраняем исходное состояние камеры
        originalParent = camTransform.parent;
        originalLocalPos = camTransform.localPosition;
        originalLocalRot = camTransform.localRotation;
        savedController = controller;

        // Открепляем камеру от персонажа, чтобы она двигалась независимо
        camTransform.SetParent(null);

        // Камера летит в заданную точку
        Vector3 targetPos = exactWorldPosition;

        // И смотрит на переданный объект
        Quaternion targetRot = Quaternion.LookRotation(lookTarget.position - targetPos);

        yield return StartCoroutine(MoveCamera(camTransform, camTransform.position, camTransform.rotation, targetPos, targetRot, moveDuration));

        // Держим камеру на цели, чтобы ребёнок успел увидеть
        yield return new WaitForSeconds(holdDuration);

        isActive = true;
    }

    /// <summary>
    /// Возвращает камеру в исходное положение и включает управление.
    /// Вызывается после завершения задания, где использовался PlayHintAndStayExact.
    /// </summary>
    public IEnumerator ReturnCamera()
    {
        if (!isActive || savedController == null) yield break;

        Camera cam = savedController.playerCamera;
        Transform camTransform = cam.transform;

        // Вычисляем исходную мировую позицию и поворот
        Vector3 returnPos = originalParent.TransformPoint(originalLocalPos);
        Quaternion returnRot = originalParent.rotation * originalLocalRot;

        yield return StartCoroutine(MoveCamera(camTransform, camTransform.position, camTransform.rotation, returnPos, returnRot, returnDuration));

        // Восстанавливаем родителя и локальные координаты
        camTransform.SetParent(originalParent);
        camTransform.localPosition = originalLocalPos;
        camTransform.localRotation = originalLocalRot;

        // Включаем управление
        savedController.EnableControl();
        isActive = false;
    }

    /// <summary>
    /// Вспомогательный метод плавного перемещения и поворота камеры.
    /// Использует SmoothStep для более естественного движения.
    /// </summary>
    private IEnumerator MoveCamera(Transform cam, Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            cam.position = Vector3.Lerp(fromPos, toPos, t);
            cam.rotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }
        // Точно фиксируем конечные значения
        cam.position = toPos;
        cam.rotation = toRot;
    }

    /// <summary>
    /// Перемещает камеру к цели, показывает её несколько секунд и возвращает обратно.
    /// Полный цикл подсказки без "оставания".
    /// </summary>
    /// <param name="target">Целевой объект, к которому нужно подвести камеру.</param>
    /// <param name="controller">Контроллер игрока.</param>
    public IEnumerator PlayHint(Transform target, FirstPersonController controller)
    {
        if (target == null)
        {
            Debug.LogWarning("[CameraHint] Target is null, skipping hint.");
            yield break;
        }

        Camera cam = controller.playerCamera;
        if (cam == null) cam = Camera.main;
        Transform camTransform = cam.transform;

        // 1. Отключаем управление
        controller.SetInteractionBlocked(true);
        controller.DisableControl();

        // 2. Сохраняем исходную позицию и поворот камеры
        Vector3 originalPos = camTransform.position;
        Quaternion originalRot = camTransform.rotation;

        // 3. Открепляем камеру, чтобы она не двигалась с персонажем
        Transform originalParent = camTransform.parent;
        camTransform.SetParent(null);

        // 4. Перемещаем камеру к цели и фокусируемся на ней
        Vector3 targetPos = target.position - camTransform.forward * 2f;
        Quaternion targetRot = Quaternion.LookRotation(target.position - camTransform.position);

        yield return StartCoroutine(MoveCamera(camTransform, originalPos, originalRot, targetPos, targetRot, moveDuration));

        // 5. Держим камеру на цели
        yield return new WaitForSeconds(holdDuration);

        // 6. Возвращаем камеру обратно
        yield return StartCoroutine(MoveCamera(camTransform, targetPos, targetRot, originalPos, originalRot, returnDuration));

        // 7. Восстанавливаем родителя
        camTransform.SetParent(originalParent);

        // 8. Включаем управление
        controller.EnableControl();
    }

    /// <summary>
    /// Плавно изменяет поле зрения камеры (зум) до указанного значения.
    /// </summary>
    /// <param name="targetFov">Конечное значение FOV.</param>
    /// <param name="duration">Длительность анимации.</param>
    public IEnumerator ZoomTo(float targetFov, float duration)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        float startFov = cam.fieldOfView;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            cam.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
            yield return null;
        }
        cam.fieldOfView = targetFov;
    }

    /// <summary>
    /// Сбрасывает зум камеры к исходному значению из FirstPersonController.
    /// </summary>
    /// <param name="duration">Длительность анимации.</param>
    public IEnumerator ResetZoom(float duration)
    {
        Camera cam = Camera.main;
        FirstPersonController controller = FindObjectOfType<FirstPersonController>();
        float originalFov = controller != null ? controller.fov : 60f;

        float startFov = cam.fieldOfView;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            cam.fieldOfView = Mathf.Lerp(startFov, originalFov, t);
            yield return null;
        }
        cam.fieldOfView = originalFov;
    }
}