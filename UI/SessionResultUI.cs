using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Отображает итоговые результаты игровой сессии.
/// Показывает общее время, количество завершённых уровней, количество попыток,
/// а также среднее время выполнения и среднее количество ошибок по подготовительным этапам.
/// Кнопка "Выйти" закрывает приложение.
/// </summary>
public class SessionResultUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI totalTimeText;        // текст для общего времени сессии
    [SerializeField] private TextMeshProUGUI levelsCompletedText;  // текст для количества завершённых уровней
    [SerializeField] private TextMeshProUGUI levelsAttemptedText;  // текст для количества предпринятых уровней
    [SerializeField] private TextMeshProUGUI averageTimeText;      // текст для среднего времени прохождения
    [SerializeField] private TextMeshProUGUI averageErrorsText;    // текст для среднего количества ошибок

    [Header("Buttons")]
    [SerializeField] private Button exitButton;                    // кнопка выхода из приложения

    private void Start()
    {
        // Получаем текущую сессию из SessionManager
        GameSessionData session = SessionManager.Instance?.GetCurrentSession();

        if (session != null)
        {
            // Общее время сессии
            totalTimeText.text = FormatTime(session.totalTime);
            levelsCompletedText.text = session.levelsCompleted.ToString();
            levelsAttemptedText.text = session.levelsAttempted.ToString();

            // Средние показатели по подготовительным этапам
            CalculateAverages(session, out float avgTime, out float avgErrors);
            averageTimeText.text = FormatTime(avgTime);
            averageErrorsText.text = avgErrors.ToString("F1");
        }
        else
        {
            Debug.LogError("Нет данных сессии!");
        }

        // По нажатию кнопки выходим из приложения
        exitButton.onClick.AddListener(Application.Quit);
    }

    /// <summary>
    /// Форматирует время из секунд в строку "ММ:СС".
    /// </summary>
    private string FormatTime(float sec)
    {
        int m = Mathf.FloorToInt(sec / 60f);
        int s = Mathf.FloorToInt(sec % 60f);
        return $"{m:D2}:{s:D2}";
    }

    /// <summary>
    /// Вычисляет среднее время выполнения и среднее количество ошибок
    /// по всем успешно завершённым подготовительным этапам.
    /// </summary>
    /// <param name="session">Текущая игровая сессия.</param>
    /// <param name="avgTime">Среднее время (исходящий параметр).</param>
    /// <param name="avgErrors">Среднее количество ошибок (исходящий параметр).</param>
    private void CalculateAverages(GameSessionData session, out float avgTime, out float avgErrors)
    {
        float totalTime = 0f;
        float totalErrors = 0f;
        int count = 0;

        foreach (var level in session.levels)
        {
            // Учитываем только успешно завершённые подготовительные этапы
            if (level.stage == "preparation" && level.success)
            {
                totalTime += level.levelTotalTime;
                totalErrors += level.totalErrors;
                count++;
            }
        }

        if (count > 0)
        {
            avgTime = totalTime / count;
            avgErrors = totalErrors / count;
        }
        else
        {
            avgTime = 0f;
            avgErrors = 0f;
        }
    }
}