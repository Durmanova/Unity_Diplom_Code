using UnityEngine;

/// <summary>
/// Обрабатывает вход/выход игрока в различные зоны сцены.
/// Определяет, какой менеджер уровня сейчас активен (передаётся через инспектор),
/// и вызывает соответствующие методы при срабатывании триггеров.
/// </summary>
public class PlayerZoneHandler : MonoBehaviour
{
    [SerializeField] private BaseLevel3DManager levelManager; // назначить в инспекторе

    /// <summary>
    /// Вызывается при входе коллайдера игрока в триггер.
    /// В зависимости от тега вызывает нужный метод менеджера уровня.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (levelManager == null) return;

        if (other.CompareTag("WaitingZone"))
            levelManager.OnPlayerEnterWaitingZone();
        else if (other.CompareTag("Road"))
        {
            levelManager.OnPlayerEnterRoad();
            levelManager.OnPlayerEnterRoadTask6();
        }
        else if (other.CompareTag("Crosswalk"))
            levelManager.OnPlayerEnterCrosswalk();
        else if (other.CompareTag("FinishZone"))
            levelManager.OnPlayerReachFinishZone();
    }

    /// <summary>
    /// Вызывается при выходе коллайдера игрока из триггера.
    /// Используется для сброса флагов и фиксации схода с перехода.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (levelManager == null) return;

        if (other.CompareTag("WaitingZone"))
            levelManager.OnPlayerExitWaitingZone();
        else if (other.CompareTag("Road"))
            levelManager.OnPlayerExitRoadTask6();
        else if (other.CompareTag("Crosswalk"))
            levelManager.OnPlayerExitCrosswalk(); // здесь проверим сход
    }
}