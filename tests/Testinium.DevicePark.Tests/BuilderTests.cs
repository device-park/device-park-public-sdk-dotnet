using Testinium.DevicePark.Errors;
using Testinium.DevicePark.Models;
using Xunit;

namespace Testinium.DevicePark.Tests;

public sealed class BuilderTests
{
    [Fact]
    public void StartSessionBuilderRequiresTheBackendMandatoryFields()
    {
        Assert.Throws<DeviceParkConfigException>(() => new DeviceStartSessionRequest.Builder().Build());

        Assert.Throws<DeviceParkConfigException>(() => new DeviceStartSessionRequest.Builder()
            .AllocationId("alloc-1")
            .Build());

        Assert.Throws<DeviceParkConfigException>(() => new DeviceStartSessionRequest.Builder()
            .AllocationId("alloc-1")
            .UserId(42)
            .UserEmail("qa@example.com")
            .CompanyId(7)
            .CompanyName("   ")
            .Build());
    }

    [Fact]
    public void StartSessionBuilderKeepsOptionalFieldsUnset()
    {
        var request = new DeviceStartSessionRequest.Builder()
            .AllocationId("alloc-1")
            .UserId(42)
            .UserEmail("qa@example.com")
            .CompanyId(7)
            .CompanyName("Example Company")
            .Build();

        Assert.Equal("alloc-1", request.AllocationId);
        Assert.Null(request.CompanyPoolId);
        Assert.Null(request.AppiumVersion);
        Assert.Null(request.CustomVideoRecordingPath);
        Assert.False(request.VideoRecording);
        Assert.Equal(VideoRecordingScope.FULL_SESSION, request.VideoRecordingScope);
    }

    [Fact]
    public void AllocationBuilderDefaultsAndValidatesPriority()
    {
        var request = new DeviceAllocationRequest.Builder().Serial("ABC-1").Build();

        Assert.Equal(3, request.Priority);
        Assert.Equal(RemoveAppSelection.NO_REMOVE, request.RemoveApps);
        Assert.Equal("ABC-1", request.Serial);
        Assert.Null(request.DevicePoolId);

        Assert.Throws<DeviceParkConfigException>(() => new DeviceAllocationRequest.Builder().Priority(0));
        Assert.Throws<DeviceParkConfigException>(() => new DeviceAllocationRequest.Builder().Priority(6));
    }

    [Fact]
    public void CreatePoolBuilderRejectsBlankName()
    {
        Assert.Throws<DeviceParkConfigException>(() => new CreatePoolRequest.Builder().Build());
        Assert.Throws<DeviceParkConfigException>(() => new CreatePoolRequest.Builder().Name("   ").Build());

        Assert.Equal("Regression", new CreatePoolRequest.Builder().Name("Regression").Build().Name);
    }

    [Fact]
    public void BuildReturnsADefensiveCopy()
    {
        var builder = new ListDevicesRequest.Builder().Page(1).Size(5);

        var first = builder.Build();
        builder.Page(9).AddFilter(DeviceFilter.STATE, "HEALTHY", SearchOperation.EQUAL);
        var second = builder.Build();

        Assert.NotSame(first, second);
        Assert.Equal(1, first.Sorting.Page);
        Assert.Empty(first.Filters);
        Assert.Equal(9, second.Sorting.Page);
        Assert.Single(second.Filters);
    }

    [Fact]
    public void FiltersReplacesTheWholeList()
    {
        var request = new ListDevicesRequest.Builder()
            .AddFilter(DeviceFilter.PLATFORM, "android", SearchOperation.EQUAL)
            .Filters(new[]
            {
                DeviceFilterRequest.Of(DeviceFilter.SERIAL_NUMBER, "ABC-1", SearchOperation.EQUAL)
            })
            .Build();

        Assert.Single(request.Filters);
        Assert.Equal(DeviceFilter.SERIAL_NUMBER, request.Filters[0].Key);
    }

    [Fact]
    public void SortingDefaultsMatchTheOtherSdks()
    {
        var sorting = new ListPoolsRequest.Builder().Build().Sorting;

        Assert.Equal(0, sorting.Page);
        Assert.Equal(20, sorting.Size);
        Assert.Equal("ID", sorting.SortBy);
        Assert.Equal(SortDirection.DESC, sorting.Direction);
    }
}
