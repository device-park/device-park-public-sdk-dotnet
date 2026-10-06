namespace Testinium.DevicePark.Tests;

internal static class TestClient
{
    internal static DeviceParkApiClient Create(StubHandler handler) => DeviceParkApiClient.Builder()
        .Url("https://devicepark.testinium.io")
        .Credentials(Credentials.Of("client-id", "client-secret"))
        .HttpMessageHandler(handler)
        .Build();
}

internal static class Payloads
{
    internal const string EmptyPage =
        "{\"size\":20,\"page\":0,\"totalPages\":0,\"totalElements\":0,\"data\":[]}";

    internal const string DevicePage =
        "{\"size\":20,\"page\":0,\"totalPages\":1,\"totalElements\":1,\"data\":[" +
        "{\"id\":7,\"serial\":\"ABC-1\",\"marketName\":\"Redmi 13T Pro\",\"model\":\"23078PND5G\"," +
        "\"manufacturer\":\"Xiaomi\",\"platform\":\"android\",\"platformVersion\":\"15\"," +
        "\"version\":\"15\",\"state\":\"HEALTHY\",\"isSimulator\":false,\"isPublic\":true," +
        "\"brandNewServerField\":\"ignored\"}]}";

    internal const string DefaultPoolPage =
        "{\"size\":20,\"page\":0,\"totalPages\":1,\"totalElements\":1,\"data\":[" +
        "{\"id\":\"default-pool\",\"name\":\"Default\",\"isDefault\":true}]}";
}
