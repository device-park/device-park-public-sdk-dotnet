using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Testinium.DevicePark.Errors;

namespace Testinium.DevicePark.Internal;

internal static class JsonMapper
{
    private static readonly ConcurrentDictionary<Type, DataContractJsonSerializer> Serializers =
        new ConcurrentDictionary<Type, DataContractJsonSerializer>();

    internal static T FromJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new DeviceParkSerializationException("Response body was empty", null);
        }

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var deserialized = SerializerFor(typeof(T)).ReadObject(stream);

            if (deserialized is T result)
            {
                return result;
            }

            throw new DeviceParkSerializationException("Response body did not match " + typeof(T).Name, null);
        }
        catch (DeviceParkSerializationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DeviceParkSerializationException("Failed to parse JSON response", exception);
        }
    }

    internal static string ToJson<T>(T value)
    {
        try
        {
            using var stream = new MemoryStream();
            SerializerFor(typeof(T)).WriteObject(stream, value);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (Exception exception)
        {
            throw new DeviceParkSerializationException("Failed to serialize JSON", exception);
        }
    }

    private static DataContractJsonSerializer SerializerFor(Type type) =>
        Serializers.GetOrAdd(type, created => new DataContractJsonSerializer(created));
}
