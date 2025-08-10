namespace MathCore.HackRF.Infrastructure.Extensions;

/// <summary>Методы расширения для исключений</summary>
internal static class ExceptionEx
{
    /// <summary>Добавляет данные к исключению</summary>
    /// <typeparam name="TException">Тип исключения</typeparam>
    /// <param name="exception">Исключение для добавления данных</param>
    /// <param name="key">Ключ данных</param>
    /// <param name="value">Значение данных</param>
    /// <returns>Исключение с добавленными данными</returns>
    public static TException WithData<TException>(this TException exception, string key, object value) where TException : Exception
    {
        if (exception.Data.Contains(key))
            exception.Data[key] = value;
        else
            exception.Data.Add(key, value);

        return exception;
    }
}
