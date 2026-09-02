using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Отслеживает взгляд игрока через центр камеры.
/// Позволяет определить, смотрит ли игрок на объект с заданным тегом.
/// При удержании взгляда на цели дольше requiredHoldTime вызывает OnGazeComplete.
/// При отсутствии взгляда на цели дольше offTargetThreshold вызывает OnGazeWrong.
/// Поддерживает игнорирование объектов с определёнными тегами (например, машины),
/// чтобы случайное попадание луча на них не сбрасывало таймеры.
/// </summary>
public class GazeDetector : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float requiredHoldTime = 3.0f;   // время удержания взгляда для успеха
    [SerializeField] private float offTargetThreshold = 3.0f; // время отсутствия взгляда для ошибки
    [SerializeField] private float maxRayDistance = 50f;      // дальность луча
    [SerializeField] private LayerMask raycastMask = ~0;      // слои, по которым луч может попадать

    [Header("События")]
    public UnityEvent OnGazeComplete;   // взгляд успешно удержан на цели
    public UnityEvent OnGazeWrong;      // взгляд слишком долго не на цели

    private Camera cam;                  // камера, из которой выпускается луч
    private string currentTargetTag = ""; // текущий целевой тег
    private float holdTimer = 0f;         // таймер удержания на цели
    private float offTargetTimer = 0f;    // таймер отсутствия на цели
    private bool isHolding = false;       // взгляд сейчас на цели
    private bool isCompleted = false;     // задание уже выполнено (детектор выключен логически)
    private List<string> ignoredTags = new List<string>(); // теги, которые игнорируются

    /// <summary>
    /// Устанавливает список тегов, которые детектор должен игнорировать.
    /// Если луч попадает в объект (или его родителя) с таким тегом,
    /// текущий кадр не изменяет таймеры (ни удержания, ни отсутствия).
    /// </summary>
    /// <param name="tags">Список тегов для игнорирования или null, чтобы очистить.</param>
    public void SetIgnoredTags(List<string> tags)
    {
        ignoredTags = tags ?? new List<string>();
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        SetActive(false); // изначально детектор выключен
    }

    /// <summary>
    /// Устанавливает целевой тег, на который нужно смотреть.
    /// Сбрасывает все таймеры и состояние удержания.
    /// </summary>
    /// <param name="tag">Тег целевого объекта.</param>
    public void SetTargetTag(string tag)
    {
        currentTargetTag = tag;
        ResetGaze();
    }

    /// <summary>
    /// Включает или выключает детектор.
    /// При выключении сбрасывает таймеры и состояние завершения.
    /// </summary>
    /// <param name="active">True, чтобы начать отслеживание; False — остановить.</param>
    public void SetActive(bool active)
    {
        enabled = active;
        if (!active) ResetGaze();
        isCompleted = false;
        offTargetTimer = 0f;
    }

    private void Update()
    {
        // Если задание уже завершено или цель не задана, ничего не делаем
        if (isCompleted || string.IsNullOrEmpty(currentTargetTag)) return;

        // Создаём луч из центра камеры вперёд
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, isHolding ? Color.yellow : Color.green);

        // Проверяем попадание луча в коллайдеры
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastMask))
        {
            // 1. Игнорируем объекты с тегами из ignoredTags (и их родителей) 
            if (ignoredTags.Count > 0)
            {
                Transform check = hit.collider.transform;
                bool ignored = false;
                while (check != null)
                {
                    if (ignoredTags.Contains(check.tag))
                    {
                        ignored = true;
                        break;
                    }
                    check = check.parent;
                }
                if (ignored)
                    return; // этот кадр игнорируем полностью, таймеры не трогаем
            }

            // Отладочный вывод (при необходимости можно убрать или обернуть в logDebug)
            Debug.Log($"Hit: {hit.collider.name}, tag: {hit.collider.tag}");

            // 2. Ищем нужный тег у самого объекта или его родителей
            bool targetHit = false;
            Transform current = hit.collider.transform;
            while (current != null)
            {
                if (current.CompareTag(currentTargetTag))
                {
                    targetHit = true;
                    break;
                }
                current = current.parent;
            }

            if (targetHit)
            {
                // Взгляд на цели
                if (!isHolding) { isHolding = true; holdTimer = 0f; }
                holdTimer += Time.deltaTime;
                offTargetTimer = 0f;   // сбрасываем таймер отсутствия

                if (holdTimer >= requiredHoldTime)
                {
                    isCompleted = true;
                    OnGazeComplete?.Invoke();   // задание выполнено
                }
            }
            else
            {
                // Взгляд не на цели
                if (isHolding)
                {
                    isHolding = false;
                    holdTimer = 0f;
                }
                offTargetTimer += Time.deltaTime;
                if (offTargetTimer >= offTargetThreshold)
                {
                    OnGazeWrong?.Invoke();   // ошибка: слишком долго не смотрим на цель
                    offTargetTimer = 0f;     // сброс, чтобы не спамить
                }
            }
        }
        else
        {
            // Луч ни во что не попал (небо, пустота) — тоже считается отсутствием взгляда
            if (isHolding) { isHolding = false; holdTimer = 0f; }
            offTargetTimer += Time.deltaTime;
            if (offTargetTimer >= offTargetThreshold)
            {
                OnGazeWrong?.Invoke();
                offTargetTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Полностью сбрасывает состояние детектора: удержание, таймеры.
    /// </summary>
    public void ResetGaze()
    {
        isHolding = false;
        holdTimer = 0f;
        offTargetTimer = 0f;
    }
}