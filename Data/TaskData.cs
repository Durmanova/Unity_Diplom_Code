using System.Collections.Generic;

/// <summary>
/// Описывает одно задание подготовительного (2D) этапа.
/// Содержит текст инструкции, правильный объект, список отображаемых объектов,
/// параметры анимации и имя аудиофайла озвучки.
/// Загружается из JSON с помощью TaskLoader.
/// </summary>
[System.Serializable]
public class TaskData
{
    public int taskId;                         // уникальный номер задания в рамках уровня
    public string taskName;                    // название задания (для отладки)
    public string correctTargetName;           // имя правильного объекта (по нему проверяется клик)
    public string instructionText;             // текст инструкции, показываемый ребёнку
    public List<TaskObjectInfo> objectsToShow; // список объектов, отображаемых на экране
    public bool hasAnimation = false;          // нужно ли проигрывать анимацию перед заданием
    public List<string> sequentialTargets;     // последовательность правильных объектов (для многошаговых заданий)

    public string audioInstruction;            // имя аудиофайла озвучки инструкции (без расширения)
}