[System.Serializable]
///<summary>
/// Хранить данные ошибок, которые может совершить
/// ребенок во время игровой сессии
///</summary>
public class SessionErrorData
{
    public string errorType;   // "wrong_color", "off_path" и т.п.
    public float errorTime;    // время от начала уровня в секундах
}