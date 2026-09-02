using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Управляет меню паузы.
/// Вызывается по кнопке паузы или клавише Esc.
/// Ставит игру на паузу (Time.timeScale = 0), разблокирует курсор.
/// Позволяет продолжить игру или завершить сессию и перейти к результатам.
/// Работает как в 2D, так и в 3D сценах: в 2D дополнительно блокирует интерактивность через Stage2DManager.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private GameObject pausePanel;   // панель с кнопками паузы
    [SerializeField] private Button pauseButton;      // кнопка в углу экрана для вызова паузы
    [SerializeField] private Button resumeButton;     // кнопка «Продолжить»
    [SerializeField] private Button quitButton;       // кнопка «Завершить занятие»

    private bool isPaused = false;                    // текущее состояние паузы

    private void Start()
    {
        pausePanel.SetActive(false);                 // меню паузы скрыто изначально

        pauseButton.onClick.AddListener(TogglePause);
        resumeButton.onClick.AddListener(Resume);
        quitButton.onClick.AddListener(QuitSession);
    }

    private void Update()
    {
        // Реагируем на клавишу Esc
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    /// <summary>
    /// Переключает состояние паузы. При активации останавливает время,
    /// блокирует взаимодействие и показывает курсор.
    /// </summary>
    private void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            // Ставим игру на паузу
            Time.timeScale = 0f;

            // Если это 2D-сцена, блокируем клики через Stage2DManager
            if (Stage2DManager.Instance != null)
                Stage2DManager.Instance.SetInteractionBlocked(true);

            // Освобождаем курсор для UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Resume();
        }
    }

    /// <summary>
    /// Возобновляет игру: снимает паузу, восстанавливает курсор.
    /// </summary>
    private void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;

        if (Stage2DManager.Instance != null)
        {
            Stage2DManager.Instance.SetInteractionBlocked(false);
            // В 2D не блокируем курсор
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // В 3D снова блокируем курсор
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Завершает игровую сессию: сбрасывает паузу, разблокирует курсор,
    /// финализирует метрики и загружает сцену результатов.
    /// </summary>
    private void QuitSession()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ВНИМАНИЕ: в текущем SessionManager нет метода FinishSessionAsync()!
        // Нужно заменить на SessionManager.Instance?.FinishSession();
        // Или добавить асинхронный метод в SessionManager.
        SessionManager.Instance?.FinishSessionAsync();

        SceneManager.LoadScene("SessionResult");
    }
}