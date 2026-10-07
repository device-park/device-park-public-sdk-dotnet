using System;

namespace Testinium.DevicePark.Errors;

// DP-PARITY:FROM-NODE  Java SDK'da tipli hata hiyerarsisi yok; tum hatalar RuntimeException olarak firlatiliyor.
// DP-PARITY:JAVA-DEAD  Java SdkResponse modeli Apache HttpStatus tipini public yuzeye sizdiriyor, getter'i yok ve hic kullanilmiyor; tasinmadi.
public class DeviceParkException : Exception
{
    public DeviceParkException(string message)
        : base(message)
    {
    }

    public DeviceParkException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

public sealed class DeviceParkConfigException : DeviceParkException
{
    public DeviceParkConfigException(string message)
        : base(message)
    {
    }
}

public sealed class DeviceParkSerializationException : DeviceParkException
{
    public DeviceParkSerializationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

public sealed class DeviceParkHttpException : DeviceParkException
{
    public DeviceParkHttpException(int status, string body)
        : this(status, body, null, null)
    {
    }

    public DeviceParkHttpException(int status, string body, string? errorCode, string? errorMessage)
        : base(Describe(status, errorCode, errorMessage))
    {
        Status = status;
        Body = body;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public int Status { get; }

    public string Body { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    private static string Describe(int status, string? errorCode, string? errorMessage)
    {
        var description = "HTTP request failed with status " + status;

        if (errorCode is null && errorMessage is null)
        {
            return description;
        }

        return description + " (" + (errorCode ?? "?") + ": " + (errorMessage ?? string.Empty) + ")";
    }
}
