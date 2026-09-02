using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Отвечает за отображение текстовых сообщений, озвучивание инструкций,
/// показ итогового экрана со звёздами и управление кнопками на нём.
/// Является синглтоном и существует в каждой игровой сцене.
/// </summary>
public class UIFeedbackController : MonoBehaviour
{
    // ---------- Singleton ----------
    public static UIFeedbackController Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI feedbackText;      // основное текстовое поле для сообщений
    [SerializeField] private float messageDuration = 3f;        // сколько секунд сообщение остаётся на экране

    [Header("UI Panel")]
    [SerializeField] private GameObject feedbackPanel;          // панель с текстом (может скрываться)

    [Header("Audio - Effects")]
    [SerializeField] private AudioSource audioSource;           // источник для коротких звуковых эффектов
    [SerializeField] private AudioClip successClip;             // звук успеха
    [SerializeField] private AudioClip errorClip;               // звук ошибки
    [SerializeField] private AudioClip hintClip;                // звук подсказки

    [Header("Audio - Voice")]
    [SerializeField] private AudioSource voiceAudioSource;      // отдельный источник для озвучки инструкций

    [Header("Final Screen")]
    [SerializeField] private GameObject finalScreenPanel;       // панель итогового экрана
    [SerializeField] private Image[] starImages;                // массив из пяти звёзд
    [SerializeField] private Sprite filledStarSprite;           // спрайт закрашенной звезды
    [SerializeField] private Sprite emptyStarSprite;            // спрайт пустой звезды
    [SerializeField] private Image backgroundImage;             // фон финального экрана (затемнение)

    [Header("Final Screen Buttons")]
    [SerializeField] private Button nextLevelButton;            // кнопка «Следующий уровень»
    [SerializeField] private Button endSessionButton;           // кнопка «Закончить сессию»
    [SerializeField] private float buttonsDelay = 2.5f;         // задержка перед показом кнопок

    // Корутина для отложенного показа кнопок
    private Coroutine delayedButtonsCoroutine;

    private void Awake()
    {
        // Singleton: оставляем только один экземпляр
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Кнопки финального экрана изначально скрыты
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (endSessionButton != null) endSessionButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Показывает сообщение об успехе: текст «Молодец!» + звуковой эффект и голосовое поощрение.
    /// </summary>
    public void ShowSuccess()
    {
        ShowMessage("Молодец!", successClip);
        PlayInstructionAudio("success_voice");
    }

    /// <summary>
    /// Повторяет инструкцию (текст и/или озвучку). Используется при ошибке,
    /// чтобы напомнить ребёнку, что нужно делать.
    /// </summary>
    /// <param name="text">Текст инструкции.</param>
    /// <param name="audioFileName">Имя аудиофайла озвучки (без расширения).</param>
    public void ShowRepeatInstruction(string text, string audioFileName)
    {
        // Показываем панель и текст инструкции
        if (feedbackPanel != null) feedbackPanel.SetActive(true);
        if (!string.IsNullOrEmpty(text))
        {
            if (feedbackText != null)
            {
                feedbackText.text = text;
                StopAllCoroutines();                          // останавливаем предыдущую очистку
                StartCoroutine(ClearMessageAfterDelay());     // запускаем очистку через время
            }
        }

        // Проигрываем озвучку
        PlayInstructionAudio(audioFileName);
    }

    /// <summary>
    /// Показывает короткую подсказку (текст и звуковой эффект).
    /// </summary>
    /// <param name="hint">Текст подсказки.</param>
    public void ShowHint(string hint)
    {
        ShowMessage(hint, hintClip);
    }

    /// <summary>
    /// Внутренний метод для показа любого сообщения с возможным звуковым эффектом.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="clip">Звуковой эффект (опционально).</param>
    private void ShowMessage(string message, AudioClip clip = null)
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(true);
        if (feedbackText != null)
        {
            feedbackText.text = message;
            StopAllCoroutines();
            StartCoroutine(ClearMessageAfterDelay());
        }

        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Корутина очистки сообщения через заданное время.
    /// </summary>
    private IEnumerator ClearMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    /// <summary>
    /// Проигрывает озвучку инструкции по имени файла из Resources/AudioTasks.
    /// Если файл не найден или источник не назначен, выводит предупреждение.
    /// </summary>
    /// <param name="audioFileName">Имя аудиофайла без расширения.</param>
    public void PlayInstructionAudio(string audioFileName)
    {
        if (string.IsNullOrEmpty(audioFileName))
            return;

        if (voiceAudioSource == null)
        {
            Debug.LogWarning("[UIFeedbackController] Voice AudioSource не назначен.");
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>("AudioTasks/" + audioFileName);
        if (clip == null)
        {
            Debug.LogWarning($"[UIFeedbackController] Аудиоклип не найден: Audio/{audioFileName}");
            return;
        }

        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
    }

    /// <summary>
    /// Останавливает проигрывание голоса (например, при завершении уровня).
    /// </summary>
    public void StopVoice()
    {
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
        }
    }

    /// <summary>
    /// Показывает финальный экран со звёздами (без кнопок).
    /// </summary>
    /// <param name="stars">Количество звёзд (0–5).</param>
    /// <param name="timeSec">Время прохождения уровня.</param>
    public void ShowFinalScreen(int stars, float timeSec)
    {
        Debug.Log($"[UIFeedbackController] ShowFinalScreen: stars={stars}, starImages.Length={starImages.Length}");

        StopVoice(); // чтобы голос не мешал финальному экрану

        if (finalScreenPanel != null)
        {
            finalScreenPanel.SetActive(true);
        }

        // Устанавливаем спрайты звёзд в зависимости от результата
        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].sprite = (i < stars) ? filledStarSprite : emptyStarSprite;
        }
    }

    /// <summary>
    /// Показывает финальный экран со звёздами и отложенным появлением кнопок.
    /// Используется в 3D-уровнях после завершения.
    /// </summary>
    /// <param name="stars">Количество звёзд.</param>
    /// <param name="timeSec">Время прохождения.</param>
    /// <param name="isLastLevel">Если true, кнопка «Следующий уровень» будет скрыта.</param>
    public void ShowFinalScreen3D(int stars, float timeSec, bool isLastLevel)
    {
        // Освобождаем курсор, чтобы можно было нажимать кнопки
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Показываем звёзды
        ShowFinalScreen(stars, timeSec);

        // Показываем/скрываем фон в зависимости от того, последний ли уровень
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(!isLastLevel);

        // Если уже была запущена корутина показа кнопок, останавливаем её
        if (delayedButtonsCoroutine != null)
            StopCoroutine(delayedButtonsCoroutine);

        // Запускаем отложенный показ кнопок
        delayedButtonsCoroutine = StartCoroutine(ShowButtonsDelayed(isLastLevel));
    }

    /// <summary>
    /// Корутина, которая через buttonsDelay секунд активирует нужные кнопки
    /// и настраивает их обработчики.
    /// </summary>
    private IEnumerator ShowButtonsDelayed(bool isLastLevel)
    {
        Debug.Log($"[ShowButtonsDelayed] Старт. Ждём {buttonsDelay} сек. isLastLevel={isLastLevel}");
        yield return new WaitForSeconds(buttonsDelay);
        Debug.Log($"[ShowButtonsDelayed] Время вышло. Активируем кнопки.");

        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(!isLastLevel); // кнопку «Следующий уровень» показываем только если не последний
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() =>
            {
                finalScreenPanel.SetActive(false);
                LevelManager.Instance.GoToNextLevel();
            });
        }
        else
        {
            Debug.Log("Нет объекта nextLevelButton");
        }

        if (endSessionButton != null)
        {
            endSessionButton.gameObject.SetActive(true);
            endSessionButton.onClick.RemoveAllListeners();
            endSessionButton.onClick.AddListener(() =>
            {
                finalScreenPanel.SetActive(false);
                LevelManager.Instance.EndSession();
            });
        }
    }
}