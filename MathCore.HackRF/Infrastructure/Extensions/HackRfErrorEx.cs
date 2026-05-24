namespace MathCore.HackRF.Infrastructure.Extensions;

internal static class HackRfErrorEx
{
    public static void ThrowIfNotSuccess(this HackRfError error, string Message)
    {
        if (error == HackRfError.Success) return;
        throw new InvalidOperationException($"{Message}. Код: {error}");
    }
}
