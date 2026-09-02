using UnityEngine;
using System;

/// <summary>
/// Управляет отображением сигналов пешеходного светофора.
/// Позволяет включать красный или зелёный свет, меняя материалы.
/// Генерирует событие OnLightChanged при переключении.
/// Используется и для визуального отображения, и для оповещения машин (будет реализовано в более поздних уровнях),
/// которые должны останавливаться/ехать в зависимости от сигнала.
/// </summary>
public class TrafficLightChangeOfColor : MonoBehaviour
{
    public GameObject obj;                 // объект светофора (у него меняются материалы)
    public Material mainMaterial;          // основной материал корпуса (не меняется)

    public Material greenLighActive;       // материал активного зелёного сигнала
    public Material greenLighNotActive;    // материал неактивного зелёного сигнала

    public Material redLighActive;         // материал активного красного сигнала
    public Material redLighNotActive;      // материал неактивного красного сигнала

    /// <summary>
    /// Текущее состояние светофора для пешехода.
    /// true — зелёный (можно идти), false — красный (стоять).
    /// </summary>
    public bool IsGreenForPedestrian { get; private set; }

    /// <summary>
    /// Событие, вызываемое при переключении сигнала.
    /// Параметр: true, если включился зелёный для пешехода, false — красный.
    /// </summary>
    public event Action<bool> OnLightChanged;

    private void Start()
    {
        RedLightOn(); // по умолчанию красный для пешехода, машины едут
    }

    /// <summary>
    /// Включает зелёный сигнал для пешехода (машины должны остановиться).
    /// Если светофор уже был зелёным, повторное событие не генерируется.
    /// </summary>
    public void GreenLightOn()
    {
        // Формируем массив материалов: корпус, красный неактивный, зелёный активный
        Material[] trafficLights = new Material[3];
        var trafficLightRenderer = obj.GetComponent<Renderer>();
        trafficLights[0] = mainMaterial;
        trafficLights[1] = redLighNotActive;
        trafficLights[2] = greenLighActive;
        trafficLightRenderer.materials = trafficLights;

        if (!IsGreenForPedestrian)
        {
            IsGreenForPedestrian = true;
            OnLightChanged?.Invoke(true);
        }
    }

    /// <summary>
    /// Включает красный сигнал для пешехода (машины могут ехать).
    /// Если светофор уже был красным, повторное событие не генерируется.
    /// </summary>
    public void RedLightOn()
    {
        // Формируем массив материалов: корпус, красный активный, зелёный неактивный
        Material[] trafficLights = new Material[3];
        var trafficLightRenderer = obj.GetComponent<Renderer>();
        trafficLights[0] = mainMaterial;
        trafficLights[1] = redLighActive;
        trafficLights[2] = greenLighNotActive;
        trafficLightRenderer.materials = trafficLights;

        if (IsGreenForPedestrian)
        {
            IsGreenForPedestrian = false;
            OnLightChanged?.Invoke(false);
        }
    }
}