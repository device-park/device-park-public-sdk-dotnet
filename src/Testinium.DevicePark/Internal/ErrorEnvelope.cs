using System;
using System.Runtime.Serialization;
using Testinium.DevicePark.Errors;

namespace Testinium.DevicePark.Internal;

[DataContract]
internal sealed class ErrorEnvelope
{
    [DataMember(Name = "errorCode")]
    public string? ErrorCode { get; private set; }

    [DataMember(Name = "errorMessage")]
    public string? ErrorMessage { get; private set; }

    internal static DeviceParkHttpException ToException(int status, string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.IndexOf("errorCode", StringComparison.Ordinal) < 0)
        {
            return new DeviceParkHttpException(status, body);
        }

        try
        {
            var envelope = JsonMapper.FromJson<ErrorEnvelope>(body);
            return new DeviceParkHttpException(status, body, envelope.ErrorCode, envelope.ErrorMessage);
        }
        catch (DeviceParkSerializationException)
        {
            return new DeviceParkHttpException(status, body);
        }
    }
}
