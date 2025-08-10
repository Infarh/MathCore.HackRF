namespace MathCore.HackRF.Infrastructure.Extensions;

/// <summary>Расширения для ReaderWriterLockSlim с автоматическим освобождением блокировок</summary>
internal static class ReaderWriterLockSlimEx
{
    /// <summary>Структура для автоматического освобождения блокировки записи</summary>
    public readonly ref struct WriteLocker(ReaderWriterLockSlim Lock)
    {
        /// <summary>Освобождает блокировку записи, если она удерживается</summary>
        public void Dispose()
        {
            if (!Lock.IsWriteLockHeld) return;
            Lock.ExitWriteLock();
        }
    }

    /// <summary>Входит в блокировку записи и возвращает структуру для автоматического освобождения</summary>
    /// <param name="this">Экземпляр ReaderWriterLockSlim</param>
    /// <returns>Структура WriteLocker для автоматического освобождения блокировки</returns>
    public static WriteLocker WriteLock(this ReaderWriterLockSlim @this)
    {
        @this.EnterWriteLock();
        return new(@this);
    }

    /// <summary>Структура для автоматического освобождения блокировки чтения</summary>
    public readonly ref struct ReadLocker(ReaderWriterLockSlim Lock)
    {
        /// <summary>Освобождает блокировку чтения, если она удерживается</summary>
        public void Dispose()
        {
            if (!Lock.IsReadLockHeld) return;
            Lock.ExitReadLock();
        }
    }

    /// <summary>Входит в блокировку чтения и возвращает структуру для автоматического освобождения</summary>
    /// <param name="this">Экземпляр ReaderWriterLockSlim</param>
    /// <returns>Структура ReadLocker для автоматического освобождения блокировки</returns>
    public static ReadLocker ReadLock(this ReaderWriterLockSlim @this)
    {
        @this.EnterReadLock();
        return new(@this);
    }
}
