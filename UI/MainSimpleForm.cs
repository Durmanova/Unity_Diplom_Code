using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// Главное меню демо-версии.
/// При старте уровня запускает демонстрационную сессию.
/// </summary>
public class MainSimpleForm : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;   // кнопка запуска уровня
    [SerializeField] private Button exitButton;    // кнопка выхода из приложения

    private void Start()
    {
        startButton.onClick.AddListener(() => StartLevel(1));
        exitButton.onClick.AddListener(ExitApplication);
    }

    /// <summary>
    /// Запускает уровень и начинает демонстрационную сессию.
    /// </summary>
    /// <param name="levelNumber">Номер уровня для запуска.</param>
    private void StartLevel(int levelNumber)
    {
        Debug.Log($"Запуск уровня {levelNumber}");

        // Запускаем демо-сессию (в полной версии здесь была бы авторизация)
        if (SessionManager.Instance != null)
            SessionManager.Instance.StartDemoSession();
        else
            Debug.LogError("SessionManager не найден");

        // Запускаем сам уровень (сначала подготовительный этап)
        LevelManager.Instance.StartLevel(levelNumber);
    }

    /// <summary>
    /// Закрывает приложение.
    /// </summary>
    private void ExitApplication()
    {
        Application.Quit();
    }
}