using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Управляет выполнением первого 3D-уровня.
/// Наследует базовый конечный автоматBaseLevel3DManager.
/// Отвечает за запуск конкретных заданий, обработку ошибок и зон.
/// </summary>
public class Level1Manager : BaseLevel3DManager
{
    [Header("Общие настройки")]
    [SerializeField] protected float taskSuccessDelay = 1.5f;
    [SerializeField] protected bool enableVisualHints = false;

    [Header("Задание 1")]
    [SerializeField] protected string crosswalkTag = "Crosswalk";
    [SerializeField] protected string task1Instruction = "Найди ПЕШЕХОДНЫЙ ПЕРЕХОД";
    [SerializeField] protected string task1ErrorHint = "ВНИМАТЕЛЬНО! Найди ПЕШЕХОДНЫЙ ПЕРЕХОД";
    [SerializeField] protected string audioTask1Instruction = "task1_crosswalk_look";

    [Header("Задание 2")]
    [SerializeField] protected string waitingZoneTag = "WaitingZone";
    [SerializeField] protected string task2Instruction = "Иди к ПЕШЕХОДНОМУ ПЕРЕХОДУ";
    [SerializeField] protected string task2ErrorDirection = "Подойди прямо к переходу";
    [SerializeField] protected string task2ErrorRoad = "Осторожно. Это проезжая часть. Иди к ПЕШЕХОДНОМУ ПЕРЕХОДУ";
    [SerializeField] protected string audioTask2Instruction = "task2_approach_crosswalk";
    [SerializeField] protected string audioTask2ErrorDirection = "task2_error_direction";
    [SerializeField] protected string audioTask2ErrorRoad = "task2_error_road";
    [SerializeField] protected GameObject waitingZoneObject;
    [SerializeField] protected float maxDirectionAngle = 45f;
    [SerializeField] protected float waitingHoldDuration = 4f;

    // Флаг, указывающий, что игрок находится в зоне ожидания и должен стоять неподвижно.
    protected bool isWaitingInZone = false;
    // Корутина, управляющая временем нахождения в зоне ожидания.
    protected Coroutine waitingCoroutine;
    // Позиция, куда возвращается игрок при ошибке в задании 2 (начальная точка подхода).
    protected Vector3 taskStartPosition;
    // Кэшированный Transform зоны ожидания, чтобы не искать её каждый кадр.
    protected Transform waitingZoneTransform;

    [Header("Задание 3")]
    [SerializeField] protected string trafficLightFullTag = "TrafficLightFull";
    [SerializeField] protected string task3Instruction = "Найди СВЕТОФОР";
    [SerializeField] protected string task3ErrorHint = "Ищи СВЕТОФОР";
    [SerializeField] protected string audioTask3Instruction = "task3_look_traffic_light";

    [Header("Задание 4")]
    [SerializeField] protected string redLightTag = "RedLight";
    [SerializeField] protected string trafficLightTag = "TrafficLight";
    [SerializeField] protected string task4Instruction = "Сейчас КРАСНЫЙ сигнал. СТОЙ";
    [SerializeField] protected string task4ErrorLookHint = "Посмотри на КРАСНЫЙ сигнал";
    [SerializeField] protected string task4ErrorMoveHint = "КРАСНЫЙ. Стой";
    [SerializeField] protected string audioTask4Instruction = "task4_red_light_stop";
    [SerializeField] protected string audioTask4ErrorMove = "task4_red_stop_move";
    [SerializeField] protected Vector3 task4CameraPosition = new Vector3(-4.02f, 4.5f, -7.41f);

    [Header("Задание 5")]
    [SerializeField] protected string task5Instruction = "Подожди ЗЕЛЕНЫЙ свет";
    [SerializeField] protected string task5ErrorMoveHint = "Стой! Жди зеленый свет";
    [SerializeField] protected string audioTask5Instruction = "task5_wait_green";
    [SerializeField] protected string audioTask5ErrorMove = "task5_error_move";
    [SerializeField] protected string audioTask5Halfway = "task5_good_waiting";
    [SerializeField] protected float task5WaitDuration = 10f;

    // Текущее время ожидания зелёного сигнала (обновляется в корутине).
    protected float currentWaitTime = 0f;
    // Флаг, показывающий, что игрок успешно ждёт.
    protected bool isTask5Waiting = false;
    // Озвучена ли промежуточная похвала «Хорошо ждёшь!» (чтобы не повторялась).
    protected bool hasPlayedHalfwayAudio = false;

    [Header("Задание 6")]
    [SerializeField] protected string task6Instruction = "ЗЕЛЕНЫЙ свет. ИДИ";
    [SerializeField] protected string task6TimeoutHint = "Уже ЗЕЛЕНЫЙ свет. Можно идти";
    [SerializeField] protected string task6OffPathHint = "Иди по пешеходному переходу";
    [SerializeField] protected string task6StopHint = "Продолжай идти";
    [SerializeField] protected string audioTask6Instruction = "task6_go";
    [SerializeField] protected string audioTask6Timeout = "task6_timeout_audio";
    [SerializeField] protected string audioTask6Stop = "task6_stop_audio";
    [SerializeField] protected string audioTask6OffPath = "task6_offpath_audio";
    [SerializeField] protected float task6Timeout = 20f;
    [SerializeField] protected float task6StopDelay = 3f;

    // Флаг, показывающий, что игрок начал движение по дороге.
    protected bool hasStartedMovingTask6 = false;
    // Флаг, указывающий, что игрок находится на проезжей части (в зоне дороги).
    protected bool isOnRoadTask6 = false;
    // Корутина тайм-аута: если игрок не начал движение за отведённое время.
    protected Coroutine task6TimeoutCoroutine;
    // Корутина проверки остановки посреди дороги.
    protected Coroutine task6StopCheckCoroutine;

    // Время последней ошибки направления (для предотвращения спама ошибок).
    protected float lastDirectionErrorTime = -10f;
    // Время последней любой ошибки (для ограничения частоты вывода подсказок).
    protected float lastAnyErrorTime = -10f;

    /// <summary>
    /// Запускает корутину текущего задания в зависимости от состояния конечного автомата.
    /// Каждое задание реализовано отдельным методом-корутиной.
    /// Если состояние не соответствует ни одному заданию (например, ошибочно),
    /// происходит автоматический переход к следующему заданию.
    /// </summary>
    protected override void StartTask()
    {
        switch (currentState)
        {
            case TaskState.Task1: StartCoroutine(Task1_IdentifyCrosswalk()); break;
            case TaskState.Task2: StartCoroutine(Task2_ApproachCrosswalk()); break;
            case TaskState.Task3: StartCoroutine(Task3_IdentifyTrafficLight()); break;
            case TaskState.Task5: StartCoroutine(Task5_WaitForGreenLight()); break;
            case TaskState.Task6: StartCoroutine(Task6_CrossRoad()); break;
            default: NextTask(); break;
        }
    }
    /// <summary>
    /// Выполняет очистку после завершения текущего задания перед переходом к следующему.
    /// Останавливает все активные корутины задания, сбрасывает флаги состояния,
    /// отписывается от событий ввода и взгляда, восстанавливает камеру и разблокирует движение.
    /// Вызывается автоматически в BaseLevel3DManager.NextTask.
    /// </summary>
    protected override void CleanupCurrentTask()
    {
        if (waitingCoroutine != null) StopCoroutine(waitingCoroutine);
        if (task6TimeoutCoroutine != null) StopCoroutine(task6TimeoutCoroutine);
        if (task6StopCheckCoroutine != null) StopCoroutine(task6StopCheckCoroutine);

        isWaitingInZone = false;
        isOnRoadTask6 = false;
        hasStartedMovingTask6 = false;
        waitingZoneTransform = null;
        isTransitioning = false;

        playerController.OnMovementAttempted -= OnMovementAttempted;
        playerController.OnMovementAttempted -= OnMovementAttemptedInTask4;
        playerController.OnMovementAttempted -= OnMovementAttemptedInTask5;

        gazeDetector.SetActive(false);
        gazeDetector.OnGazeComplete.RemoveAllListeners();
        gazeDetector.OnGazeWrong.RemoveAllListeners();

        if (cameraHint != null && Camera.main != null && Camera.main.fieldOfView != playerController.fov)
            StartCoroutine(cameraHint.ResetZoom(0.3f));
        if (cameraHint != null)
            StartCoroutine(cameraHint.ReturnCamera());

        playerController.EnableMovement();
    }

    #region Задание 1
    /// <summary>
    /// Реализует первое задание: опознавание пешеходного перехода.
    /// Ребёнок должен навести взгляд на объект с тегом crosswalkTag
    /// и удерживать его несколько секунд.
    /// Перед началом задания может показываться визуальная подсказка (если включено).
    /// Во время задания движение блокируется, но камера остаётся активной.
    /// </summary>
    protected virtual IEnumerator Task1_IdentifyCrosswalk()
    {
        Transform target = GameObject.FindGameObjectWithTag(crosswalkTag)?.transform;
        if (enableVisualHints && target != null)
            yield return cameraHint.PlayHint(target, playerController);

        SetupGazeTask(crosswalkTag, task1Instruction, audioTask1Instruction, OnTask1Complete, OnTask1GazeError);
        playerController.OnMovementAttempted += OnMovementAttempted;
    }
    /// <summary>
    /// Вызывается, когда ребёнок успешно удержал взгляд на пешеходном переходе.
    /// Передаёт управление общему методу завершения задания.
    /// </summary>
    protected virtual void OnTask1Complete() => FinishGazeTask();
    /// <summary>
    /// Вызывается, когда ребёнок слишком долго смотрит не на целевой объект.
    /// Регистрирует ошибку типа "wrong_target" и выводит подсказку.
    /// </summary>
    protected virtual void OnTask1GazeError() => HandleError(task1ErrorHint, audioTask1Instruction, "wrong_target");
    /// <summary>
    /// Вызывается, когда ребёнок пытается двигаться во время задания 1.
    /// Регистрирует ошибку типа "movement_attempt" и выводит подсказку.
    /// </summary>
    protected virtual void OnMovementAttempted() => HandleError(task1ErrorHint, audioTask1Instruction, "movement_attempt");
    #endregion

    #region Задание 2
    /// <summary>
    /// Реализует второе задание: подход к пешеходному переходу.
    /// Ребёнок должен переместить персонажа к зоне ожидания перед «зеброй».
    /// Во время движения проверяется направление (не уходит ли в сторону),
    /// а при попытке выйти на дорогу персонаж возвращается на стартовую позицию.
    /// </summary>
    protected virtual IEnumerator Task2_ApproachCrosswalk()
    {
        if (waitingZoneObject != null) waitingZoneObject.SetActive(true);

        waitingZoneTransform = GameObject.FindGameObjectWithTag(waitingZoneTag)?.transform;

        if (enableVisualHints && waitingZoneTransform != null)
            yield return cameraHint.PlayHint(waitingZoneTransform, playerController);

        taskStartPosition = playerController.transform.position;
        playerController.EnableMovement();
        feedbackUI?.ShowRepeatInstruction(task2Instruction, audioTask2Instruction);
        StartCoroutine(CheckDirection());
    }
    /// <summary>
    /// Проверяет, движется ли ребёнок в правильном направлении к зоне ожидания.
    /// Если угол между направлением персонажа и направлением на цель превышает
    /// допустимый (maxDirectionAngle), регистрируется ошибка.
    /// Работает только во время выполнения задания 2.
    /// </summary>
    protected virtual IEnumerator CheckDirection()
    {
        while (currentState == TaskState.Task2)
        {
            if (waitingZoneTransform != null && playerController.IsMoving())
            {
                Vector3 dirToTarget = (waitingZoneTransform.position - playerController.transform.position).normalized;
                dirToTarget.y = 0;
                Vector3 forward = playerController.transform.forward;
                forward.y = 0;

                if (Vector3.Angle(forward, dirToTarget) > maxDirectionAngle)
                    OnWrongDirection();
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    /// <summary>
    /// Обрабатывает ошибку движения в неправильном направлении.
    /// Защищает от повторных срабатываний (не чаще одного раза в 2 секунды).
    /// Регистрирует ошибку и выводит подсказку, если не превышен лимит.
    /// </summary>
    protected virtual void OnWrongDirection()
    {
        if (Time.time - lastDirectionErrorTime < 2f) return;
        lastDirectionErrorTime = Time.time;

        RegisterError("wrong_direction");
        HandleTaskError();
        if (errorCountCurrentTask < 3)
            feedbackUI?.ShowRepeatInstruction(task2ErrorDirection, audioTask2ErrorDirection);
    }
    /// <summary>
    /// Вызывается при входе игрока в зону ожидания.
    /// Останавливает персонажа и запускает корутину ожидания.
    /// </summary>
    public override void OnPlayerEnterWaitingZone()
    {
        if (currentState == TaskState.Task2 && !isTransitioning && !isWaitingInZone)
        {
            playerController.DisableMovement();
            isWaitingInZone = true;
            waitingCoroutine = StartCoroutine(WaitInZone());
        }
    }
    /// <summary>
    /// Вызывается при выходе из зоны ожидания до истечения времени.
    /// Фиксирует ошибку и возвращает персонажа на стартовую позицию.
    /// </summary>
    public override void OnPlayerExitWaitingZone()
    {
        if (currentState == TaskState.Task2 && isWaitingInZone)
        {
            ResetTask2State("left_waiting_zone", "Остановись перед переходом", audioTask2ErrorRoad);
        }
    }
    /// <summary>
    /// Вызывается при попытке выйти на проезжую часть (задание 2).
    /// Если персонаж был в зоне ожидания, отменяет ожидание.
    /// Возвращает на стартовую позицию и фиксирует ошибку "road_entry".
    /// </summary>
    public override void OnPlayerEnterRoad()
    {
        if (currentState == TaskState.Task2 && !isTransitioning)
        {
            if (isWaitingInZone)
                ResetTask2State("road_entry", task2ErrorRoad, audioTask2ErrorRoad);
            else
            {
                playerController.transform.position = taskStartPosition;
                RegisterError("road_entry");
                HandleTaskError();
                if (errorCountCurrentTask < 3)
                    feedbackUI?.ShowRepeatInstruction(task2ErrorRoad, audioTask2ErrorRoad);
            }
        }
    }
    /// <summary>
    /// Сбрасывает состояние задания 2: останавливает корутину ожидания,
    /// разблокирует движение, возвращает персонажа на старт.
    /// При необходимости регистрирует ошибку и выводит подсказку.
    /// </summary>
    protected void ResetTask2State(string errorType, string hint, string audio)
    {
        if (waitingCoroutine != null) StopCoroutine(waitingCoroutine);
        isWaitingInZone = false;
        playerController.EnableMovement();
        playerController.transform.position = taskStartPosition;

        if (!string.IsNullOrEmpty(errorType)) RegisterError(errorType);
        if (!string.IsNullOrEmpty(hint) && errorCountCurrentTask < 3)
            feedbackUI?.ShowRepeatInstruction(hint, audio);
    }
    /// <summary>
    /// Корутина ожидания в зоне: просто ждёт заданное время.
    /// Если персонаж остаётся в зоне, задание считается успешным.
    /// </summary>
    protected virtual IEnumerator WaitInZone()
    {
        yield return new WaitForSeconds(waitingHoldDuration);
        if (currentState == TaskState.Task2 && isWaitingInZone)
        {
            isTransitioning = true;
            isWaitingInZone = false;
            playerController.EnableMovement();
            feedbackUI?.ShowSuccess();
            feedbackUI?.PlayInstructionAudio("task2_success");
            StartCoroutine(DelayNextTask(taskSuccessDelay));
        }
    }
    #endregion

    #region Задание 3
    /// <summary>
    /// Реализует третье задание: опознавание светофора.
    /// Ребёнок должен навести взгляд на объект с тегом trafficLightFullTag.
    /// и удерживать его несколько секунд.
    /// При включённых визуальных подсказках камера сначала показывает светофор.
    /// Движение блокируется.
    /// </summary>
    protected virtual IEnumerator Task3_IdentifyTrafficLight()
    {
        Transform target = GameObject.FindGameObjectWithTag(trafficLightFullTag)?.transform;
        if (enableVisualHints && target != null)
            yield return cameraHint.PlayHint(target, playerController);

        SetupGazeTask(trafficLightFullTag, task3Instruction, audioTask3Instruction, OnTask3Complete, OnTask3GazeError);
    }
    /// <summary>
    /// Вызывается при успешном удержании взгляда на светофоре.
    /// Завершает задание через общий метод.
    /// </summary>
    protected virtual void OnTask3Complete() => FinishGazeTask();
    /// <summary>
    /// Вызывается при слишком долгом взгляде не на светофор.
    /// Регистрирует ошибку и выводит подсказку.
    /// </summary>
    protected virtual void OnTask3GazeError() => HandleError(task3ErrorHint, audioTask3Instruction, "wrong_target");
    #endregion

    #region Задание 4
    /// <summary>
    /// Реализует четвёртое задание: распознавание красного сигнала светофора.
    /// Ребёнок должен посмотреть на красный сигнал и не двигаться.
    /// Перед началом камера перемещается к светофору и немного зумится,
    /// чтобы красный сигнал был хорошо виден.
    /// Временно не используется (отключено в StartTask).
    /// </summary>
    protected virtual IEnumerator Task4_RecognizeStopSignal()
    {
        GameObject trafficLightObj = GameObject.FindGameObjectWithTag(trafficLightTag);
        if (cameraHint != null)
            StartCoroutine(cameraHint.ZoomTo(30f, 0.5f));
        if (trafficLightObj != null)
            yield return cameraHint.PlayHintAndStayExact(task4CameraPosition, trafficLightObj.transform, playerController);

        SetupGazeTask(redLightTag, task4Instruction, audioTask4Instruction, OnTask4Complete, OnTask4GazeError);
        playerController.OnMovementAttempted += OnMovementAttemptedInTask4;
    }
    /// <summary>
    /// Вызывается, когда ребёнок успешно удержал взгляд на красном сигнале.
    /// Возвращает камеру на место, сбрасывает зум и завершает задание.
    /// </summary>
    protected virtual void OnTask4Complete()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        playerController.OnMovementAttempted -= OnMovementAttemptedInTask4;

        if (cameraHint != null)
        {
            StartCoroutine(cameraHint.ReturnCamera());
            StartCoroutine(cameraHint.ResetZoom(0.5f));
        }

        feedbackUI?.ShowSuccess();
        playerController.EnableMovement();
        StartCoroutine(DelayNextTask(taskSuccessDelay));
    }
    /// <summary>
    /// Вызывается, когда ребёнок слишком долго смотрит не на красный сигнал.
    /// Регистрирует ошибку "wrong_target" и выводит подсказку.
    /// </summary>
    protected virtual void OnTask4GazeError() => HandleError(task4ErrorLookHint, audioTask4Instruction, "wrong_target");
    /// <summary>
    /// Вызывается при попытке движения во время красного сигнала.
    /// Регистрирует ошибку "movement_attempt_on_red" и выводит специальную подсказку.
    /// </summary>
    protected virtual void OnMovementAttemptedInTask4() => HandleError(task4ErrorMoveHint, audioTask4ErrorMove, "movement_attempt_on_red");
    #endregion

    #region Задание 5
    /// <summary>
    /// Реализует пятое задание: ожидание зелёного сигнала светофора.
    /// Ребёнок должен оставаться на месте в течение заданного времени,
    /// пока горит красный. Попытки движения сбрасывают таймер ожидания.
    /// По истечении времени включается зелёный свет, и задание завершается.
    /// </summary>
    protected virtual IEnumerator Task5_WaitForGreenLight()
    {
        if (cameraHint != null) yield return cameraHint.ReturnCamera();

        playerController.DisableMovement();
        playerController.OnMovementAttempted += OnMovementAttemptedInTask5;
        feedbackUI?.ShowRepeatInstruction(task5Instruction, audioTask5Instruction);

        currentWaitTime = 0f;
        hasPlayedHalfwayAudio = false;

        while (currentWaitTime < task5WaitDuration)
        {
            currentWaitTime += Time.deltaTime;

            if (!hasPlayedHalfwayAudio && currentWaitTime >= task5WaitDuration / 2f)
            {
                hasPlayedHalfwayAudio = true;
                feedbackUI?.PlayInstructionAudio(audioTask5Halfway);
            }
            yield return null;
        }

        if (currentWaitTime >= task5WaitDuration)
        {
            OnTask5Complete();
        }
    }
    /// <summary>
    /// Вызывается при успешном завершении ожидания.
    /// Включает зелёный свет, показывает поощрение и переходит к следующему заданию.
    /// </summary>
    protected virtual void OnTask5Complete()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        playerController.OnMovementAttempted -= OnMovementAttemptedInTask5;
        if (trafficLight != null) trafficLight.GreenLightOn();

        feedbackUI?.ShowSuccess();
        StartCoroutine(DelayNextTask(taskSuccessDelay));
    }
    /// <summary>
    /// Вызывается при попытке движения во время ожидания.
    /// Регистрирует ошибку, сбрасывает таймер ожидания и выводит подсказку.
    /// </summary>
    protected virtual void OnMovementAttemptedInTask5()
    {
        HandleError(task5ErrorMoveHint, audioTask5ErrorMove, "movement_attempt_while_waiting", resetTimer: true);
    }
    #endregion

    #region Задание 6
    /// <summary>
    /// Реализует шестое задание: переход дороги по пешеходному переходу на зелёный свет.
    /// Ребёнок должен начать движение, идти строго по «зебре» и не останавливаться надолго.
    /// Включает контроль тайм-аута (если не начал движение) и остановки посреди дороги.
    /// </summary>
    protected virtual IEnumerator Task6_CrossRoad()
    {
        playerController.EnableMovement();
        feedbackUI?.ShowRepeatInstruction(task6Instruction, audioTask6Instruction);

        hasStartedMovingTask6 = false;
        isOnRoadTask6 = false;

        task6TimeoutCoroutine = StartCoroutine(Task6TimeoutCheck());
        task6StopCheckCoroutine = StartCoroutine(Task6StopCheck());
        yield return null;
    }
    /// <summary>
    /// Проверяет, начал ли ребёнок движение в течение отведённого времени.
    /// Если движение не начато за task6Timeout секунд, регистрируется ошибка
    /// и выводится подсказка. После этого проверка запускается заново.
    /// </summary>
    protected virtual IEnumerator Task6TimeoutCheck()
    {
        float timer = 0f;
        while (timer < task6Timeout)
        {
            if (playerController.IsMoving())
            {
                hasStartedMovingTask6 = true;
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (!hasStartedMovingTask6 && !isTransitioning)
        {
            RegisterError("timeout_no_move");
            HandleTaskError();
            if (errorCountCurrentTask < 3)
                feedbackUI?.ShowRepeatInstruction(task6TimeoutHint, audioTask6Timeout);

            task6TimeoutCoroutine = StartCoroutine(Task6TimeoutCheck());
        }
    }
    /// <summary>
    /// Проверяет, не остановился ли ребёнок надолго посреди дороги.
    /// Если игрок находится на дороге и не двигается task6StopDelay секунд,
    /// выводится подсказка «Продолжай идти».
    /// </summary>
    protected virtual IEnumerator Task6StopCheck()
    {
        while (currentState == TaskState.Task6)
        {
            if (isOnRoadTask6 && hasStartedMovingTask6 && !playerController.IsMoving())
            {
                yield return new WaitForSeconds(task6StopDelay);
                if (isOnRoadTask6 && !playerController.IsMoving())
                    feedbackUI?.ShowRepeatInstruction(task6StopHint, audioTask6Stop);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
    /// <summary>
    /// Вызывается, когда игрок входит на проезжую часть (дорогу) в задании 6.
    /// Устанавливает флаг isOnRoadTask6, который используется для контроля движения.
    /// </summary>
    public override void OnPlayerEnterRoadTask6()
    {
        if (currentState == TaskState.Task6) isOnRoadTask6 = true;
    }
    /// <summary>
    /// Вызывается, когда игрок покидает проезжую часть (выходит на тротуар).
    /// Сбрасывает флаг isOnRoadTask6.
    /// </summary>
    public override void OnPlayerExitRoadTask6()
    {
        if (currentState == TaskState.Task6) isOnRoadTask6 = false;
    }
    /// <summary>
    /// Вызывается, когда игрок сходит с пешеходного перехода (зебры) на асфальт.
    /// Регистрирует ошибку «off_crosswalk», выводит подсказку и может показать стрелки.
    /// </summary>
    public override void OnPlayerStepOffCrosswalk()
    {
        if (currentState == TaskState.Task6 && !isTransitioning && isOnRoadTask6)
        {
            RegisterError("off_crosswalk");
            HandleTaskError();
            if (errorCountCurrentTask < 3)
            {
                feedbackUI?.ShowRepeatInstruction(task6OffPathHint, audioTask6OffPath);
            }
        }
    }
    /// <summary>
    /// Вызывается, когда игрок достигает финишной зоны на противоположной стороне дороги.
    /// Завершает задание 6 и весь уровень.
    /// </summary>
    public override void OnPlayerReachFinishZone()
    {
        if (currentState == TaskState.Task6 && !isTransitioning) OnTask6Complete();
    }
    /// <summary>
    /// Обрабатывает успешное завершение шестого задания.
    /// Останавливает все проверки, блокирует движение, показывает поощрение
    /// и запускает отложенный переход к завершению уровня.
    /// </summary>
    protected virtual void OnTask6Complete()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (task6TimeoutCoroutine != null) StopCoroutine(task6TimeoutCoroutine);
        if (task6StopCheckCoroutine != null) StopCoroutine(task6StopCheckCoroutine);

        playerController.DisableMovement();
        feedbackUI?.ShowSuccess();
        StartCoroutine(DelayNextTask(2.5f));
    }
    #endregion

    #region Общие вспомогательные методы
    /// <summary>
    /// Настраивает детектор взгляда для задания, где требуется смотреть на объект.
    /// Блокирует движение, выводит инструкцию, подписывается на события завершения/ошибки.
    /// </summary>
    /// <param name="tag">Тег целевого объекта.</param>
    /// <param name="text">Текст инструкции.</param>
    /// <param name="audio">Имя аудиофайла озвучки (без расширения).</param>
    /// <param name="onComplete">Метод при успешном удержании взгляда.</param>
    /// <param name="onError">Метод при ошибке взгляда.</param>
    /// <param name="ignoredTags">Теги, которые детектор должен игнорировать (опционально).</param>
    protected virtual void SetupGazeTask(string tag, string text, string audio,
    UnityEngine.Events.UnityAction onComplete, UnityEngine.Events.UnityAction onError,
    List<string> ignoredTags = null)
    {
        if (ignoredTags != null)
            gazeDetector.SetIgnoredTags(ignoredTags);
        else
            gazeDetector.SetIgnoredTags(new List<string>()); // очищаем список для других заданий

        playerController.DisableMovement();
        feedbackUI?.ShowRepeatInstruction(text, audio);
        gazeDetector.SetTargetTag(tag);
        gazeDetector.OnGazeComplete.AddListener(onComplete);
        gazeDetector.OnGazeWrong.AddListener(onError);
        gazeDetector.SetActive(true);
    }
    /// <summary>
    /// Обрабатывает ошибку задания: регистрирует её, ограничивает частоту,
    /// выводит подсказку и при необходимости сбрасывает таймер (для задания 5).
    /// </summary>
    /// <param name="hint">Текст подсказки при ошибке.</param>
    /// <param name="audio">Имя аудиофайла подсказки (без расширения).</param>
    /// <param name="errorType">Тип ошибки для метрик.</param>
    /// <param name="resetTimer">Если true, сбрасывает таймер ожидания (задание 5).</param>
    protected virtual void HandleError(string hint, string audio, string errorType, bool resetTimer = false)
    {
        if (isTransitioning) return;
        if (Time.time - lastAnyErrorTime < 1.5f) return;
        lastAnyErrorTime = Time.time;

        RegisterError(errorType);
        HandleTaskError();

        if (errorCountCurrentTask < 3)
        {
            feedbackUI?.ShowRepeatInstruction(hint, audio);
            Debug.Log($"[HandleError] hint={hint}, errorType={errorType}, errors={errorCountCurrentTask}");
        }

        if (resetTimer)
        {
            currentWaitTime = 0f;
        }
    }
    /// <summary>
    /// Завершает задание, основанное на удержании взгляда (задания 1, 3).
    /// Показывает поощрение, разблокирует движение и переходит к следующему заданию с задержкой.
    /// </summary>
    protected virtual void FinishGazeTask()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        feedbackUI?.ShowSuccess();
        playerController.EnableMovement();
        StartCoroutine(DelayNextTask(taskSuccessDelay));
    }
    /// <summary>
    /// Корутина задержки перед переходом к следующему заданию.
    /// Даёт ребёнку время увидеть поощрение и подготовиться.
    /// </summary>
    /// <param name="delay">Время задержки в секундах.</param>
    protected virtual IEnumerator DelayNextTask(float delay)
    {
        yield return new WaitForSeconds(delay);
        isTransitioning = false;
        NextTask();
    }
    #endregion
}