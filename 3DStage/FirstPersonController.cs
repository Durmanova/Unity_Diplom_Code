using UnityEngine;

/// <summary>
/// Контроллер персонажа от первого лица.
/// Управляет движением (WASD/стрелки) и обзором (мышь).
/// Предоставляет методы для блокировки ввода (пауза, анимации).
/// Генерирует событие OnMovementAttempted при попытке движения, когда оно запрещено.
/// </summary>
public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;

    /// <summary>Событие, вызываемое при нажатии клавиш движения, если движение запрещено.</summary>
    public event System.Action OnMovementAttempted;

    #region Camera Variables

    public Camera playerCamera;        // основная камера
    public float fov = 40f;            // базовое поле зрения
    public bool invertCamera = false;  // инвертировать ось Y мыши
    public bool cameraCanMove = true;  // разрешено ли вращение камеры
    public float mouseSensitivity = 2f; // чувствительность мыши
    public float maxLookAngle = 50f;    // максимальный угол наклона камеры по вертикали

    // Внутренние переменные
    private float yaw = 0.0f;   // угол поворота по горизонтали
    private float pitch = 0.0f; // угол наклона по вертикали

    #endregion

    #region Movement Variables

    public bool playerCanMove = true;       // разрешено ли движение
    public float walkSpeed = 2f;            // скорость ходьбы
    public float maxVelocityChange = 6f;    // максимальное изменение скорости за кадр

    #endregion

    #region Cursor Lock

    public bool lockCursor = true;          // блокировать курсор при старте

    #endregion

    // Флаг полной блокировки ввода (например, из меню паузы)
    private bool interactionBlocked = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera.fieldOfView = fov;
    }

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        // Если ввод полностью заблокирован, выходим
        if (interactionBlocked)
            return;

        // Управление камерой
        if (cameraCanMove)
        {
            yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;

            if (!invertCamera)
                pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
            else
                pitch += mouseSensitivity * Input.GetAxis("Mouse Y");

            // Ограничиваем угол наклона
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }
    }

    private void FixedUpdate()
    {
        // Движение персонажа
        if (playerCanMove && !interactionBlocked)
        {
            Vector3 targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            targetVelocity = transform.TransformDirection(targetVelocity) * walkSpeed;

            Vector3 velocity = rb.linearVelocity;
            Vector3 velocityChange = (targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
            velocityChange.y = 0;

            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
        else if (!playerCanMove && !interactionBlocked)
        {
            // Движение запрещено, но ввод не заблокирован.
            // Если игрок пытается нажать клавиши движения, сообщаем об этом.
            if (IsMoving())
            {
                OnMovementAttempted?.Invoke();
            }
        }
    }

    /// <summary>
    /// Полностью блокирует или разблокирует ввод (используется меню паузы и анимациями).
    /// Управляет также видимостью курсора.
    /// </summary>
    public void SetInteractionBlocked(bool blocked)
    {
        interactionBlocked = blocked;
        if (blocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    /// <summary>
    /// Отключает и движение, и вращение камеры (например, на время анимации).
    /// </summary>
    public void DisableControl()
    {
        cameraCanMove = false;
        playerCanMove = false;
    }

    /// <summary>
    /// Включает и движение, и вращение камеры.
    /// </summary>
    public void EnableControl()
    {
        cameraCanMove = true;
        playerCanMove = true;
    }

    /// <summary>
    /// Запрещает только движение, оставляя возможность вращать камеру.
    /// Используется в заданиях на наведение взгляда.
    /// </summary>
    public void DisableMovement()
    {
        playerCanMove = false;
    }

    /// <summary>
    /// Разрешает движение (не влияет на вращение).
    /// </summary>
    public void EnableMovement()
    {
        playerCanMove = true;
    }

    /// <summary>
    /// Возвращает true, если пользователь нажимает клавиши движения.
    /// </summary>
    public bool IsMoving()
    {
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;
    }
}