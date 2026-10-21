using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Testinium.DevicePark.Errors;

namespace Testinium.DevicePark.Models;

// DP-PARITY:JAVA-ONLY  Node SDK'da bu enum ve iliskili alanlar hic tanimli degil.
public enum VideoRecordingScope
{
    FULL_SESSION,
    APPIUM_SESSION
}

public enum SessionFilter
{
    ALLOCATION,
    SESSION,
    SERIAL,
    STATE
}

public sealed class DeviceSessionFilterRequest : FilterRequest
{
    public SessionFilter Key { get; set; }

    internal override string KeyName => Key.ToString();

    public static DeviceSessionFilterRequest Of(SessionFilter key, object? value, SearchOperation operation) =>
        new DeviceSessionFilterRequest { Key = key, Value = value, Operation = operation };
}

[DataContract]
public sealed class Session
{
    [DataMember(Name = "id")]
    public long? Id { get; private set; }

    [DataMember(Name = "state")]
    public string? State { get; private set; }

    [DataMember(Name = "client")]
    public string? Client { get; private set; }

    [DataMember(Name = "sessionId")]
    public string? SessionId { get; private set; }

    [DataMember(Name = "allocationId")]
    public string? AllocationId { get; private set; }

    [DataMember(Name = "startDate")]
    public string? StartDate { get; private set; }

    [DataMember(Name = "endDate")]
    public string? EndDate { get; private set; }

    [DataMember(Name = "latestInteractionTime")]
    public string? LatestInteractionTime { get; private set; }

    [DataMember(Name = "userId")]
    public long? UserId { get; private set; }

    [DataMember(Name = "userEmail")]
    public string? UserEmail { get; private set; }

    [DataMember(Name = "companyId")]
    public long? CompanyId { get; private set; }

    [DataMember(Name = "companyName")]
    public string? CompanyName { get; private set; }

    [DataMember(Name = "deviceSerial")]
    public string? DeviceSerial { get; private set; }

    [DataMember(Name = "deviceName")]
    public string? DeviceName { get; private set; }

    [DataMember(Name = "deviceModel")]
    public string? DeviceModel { get; private set; }

    [DataMember(Name = "deviceManufacturer")]
    public string? DeviceManufacturer { get; private set; }

    [DataMember(Name = "devicePlatform")]
    public string? DevicePlatform { get; private set; }

    [DataMember(Name = "deviceVersion")]
    public string? DeviceVersion { get; private set; }

    [DataMember(Name = "videoRecording")]
    public bool? VideoRecording { get; private set; }

    [DataMember(Name = "videoRecordingScope")]
    private string? VideoRecordingScopeValue { get; set; }

    // DP-PARITY:JAVA-ONLY  Node SDK'nin Session modelinde bu alan yok.
    public VideoRecordingScope? VideoRecordingScope =>
        Enum.TryParse(VideoRecordingScopeValue, out VideoRecordingScope parsed) ? parsed : null;

    [DataMember(Name = "videoRecordUrl")]
    public string? VideoRecordUrl { get; private set; }

    [DataMember(Name = "appiumVersion")]
    public string? AppiumVersion { get; private set; }

    [DataMember(Name = "createdAt")]
    public string? CreatedAt { get; private set; }

    [DataMember(Name = "updatedAt")]
    public string? UpdatedAt { get; private set; }

    [DataMember(Name = "dataAccessEndDate")]
    public string? DataAccessEndDate { get; private set; }
}

// DP-PARITY:JAVA-DEAD  Java DeviceStartSessionRequest'te bir sessionId alani ve sessionId(...) builder metodu var;
// DP-PARITY:JAVA-DEAD  dokumantasyonun kendisi "set etme" diyor ve sunucu bu degeri uretiyor. Tasinmadi.
[DataContract]
public sealed class DeviceStartSessionRequest
{
    [DataMember(Name = "allocationId")]
    private string AllocationIdValue { get; set; } = string.Empty;

    [DataMember(Name = "companyPoolId", EmitDefaultValue = false)]
    private string? CompanyPoolIdValue { get; set; }

    [DataMember(Name = "videoRecording")]
    private bool VideoRecordingValue { get; set; }

    [DataMember(Name = "videoRecordingScope")]
    private string VideoRecordingScopeValue { get; set; } = Models.VideoRecordingScope.FULL_SESSION.ToString();

    [DataMember(Name = "userId")]
    private long UserIdValue { get; set; }

    [DataMember(Name = "userEmail")]
    private string UserEmailValue { get; set; } = string.Empty;

    [DataMember(Name = "companyId")]
    private long CompanyIdValue { get; set; }

    [DataMember(Name = "companyName")]
    private string CompanyNameValue { get; set; } = string.Empty;

    [DataMember(Name = "customVideoRecordingPath", EmitDefaultValue = false)]
    private string? CustomVideoRecordingPathValue { get; set; }

    [DataMember(Name = "appiumVersion", EmitDefaultValue = false)]
    private string? AppiumVersionValue { get; set; }

    public string AllocationId => AllocationIdValue;

    public string? CompanyPoolId => CompanyPoolIdValue;

    public bool VideoRecording => VideoRecordingValue;

    // DP-PARITY:JAVA-ONLY  Node SDK'nin start-session istegi bu alani gondermiyor.
    public VideoRecordingScope VideoRecordingScope =>
        (VideoRecordingScope)Enum.Parse(typeof(VideoRecordingScope), VideoRecordingScopeValue);

    public long UserId => UserIdValue;

    public string UserEmail => UserEmailValue;

    public long CompanyId => CompanyIdValue;

    public string CompanyName => CompanyNameValue;

    public string? CustomVideoRecordingPath => CustomVideoRecordingPathValue;

    public string? AppiumVersion => AppiumVersionValue;

    public sealed class Builder
    {
        private string? _allocationId;
        private string? _companyPoolId;
        private bool _videoRecording;
        private VideoRecordingScope _videoRecordingScope = Models.VideoRecordingScope.FULL_SESSION;
        private long? _userId;
        private string? _userEmail;
        private long? _companyId;
        private string? _companyName;
        private string? _customVideoRecordingPath;
        private string? _appiumVersion;

        public Builder AllocationId(string allocationId)
        {
            _allocationId = allocationId;
            return this;
        }

        public Builder CompanyPoolId(string companyPoolId)
        {
            _companyPoolId = companyPoolId;
            return this;
        }

        public Builder VideoRecording(bool videoRecording)
        {
            _videoRecording = videoRecording;
            return this;
        }

        public Builder VideoRecordingScope(VideoRecordingScope videoRecordingScope)
        {
            _videoRecordingScope = videoRecordingScope;
            return this;
        }

        public Builder UserId(long userId)
        {
            _userId = userId;
            return this;
        }

        public Builder UserEmail(string userEmail)
        {
            _userEmail = userEmail;
            return this;
        }

        public Builder CompanyId(long companyId)
        {
            _companyId = companyId;
            return this;
        }

        public Builder CompanyName(string companyName)
        {
            _companyName = companyName;
            return this;
        }

        public Builder CustomVideoRecordingPath(string customVideoRecordingPath)
        {
            _customVideoRecordingPath = customVideoRecordingPath;
            return this;
        }

        public Builder AppiumVersion(string appiumVersion)
        {
            _appiumVersion = appiumVersion;
            return this;
        }

        // DP-PARITY:FROM-NODE  Java'nin build() metodu hicbir zorunlu alani dogrulamiyor.
        public DeviceStartSessionRequest Build()
        {
            if (string.IsNullOrEmpty(_allocationId))
            {
                throw new DeviceParkConfigException("allocationId is required");
            }

            if (_userId is null)
            {
                throw new DeviceParkConfigException("userId is required");
            }

            if (string.IsNullOrEmpty(_userEmail))
            {
                throw new DeviceParkConfigException("userEmail is required");
            }

            if (_companyId is null)
            {
                throw new DeviceParkConfigException("companyId is required");
            }

            if (_companyName is null)
            {
                throw new DeviceParkConfigException("companyName is required");
            }

            if (string.IsNullOrWhiteSpace(_companyName))
            {
                throw new DeviceParkConfigException("companyName cannot be blank");
            }

            return new DeviceStartSessionRequest
            {
                AllocationIdValue = _allocationId!,
                CompanyPoolIdValue = _companyPoolId,
                VideoRecordingValue = _videoRecording,
                VideoRecordingScopeValue = _videoRecordingScope.ToString(),
                UserIdValue = _userId.Value,
                UserEmailValue = _userEmail!,
                CompanyIdValue = _companyId.Value,
                CompanyNameValue = _companyName!,
                CustomVideoRecordingPathValue = _customVideoRecordingPath,
                AppiumVersionValue = _appiumVersion
            };
        }
    }
}

public sealed class DeviceSessionRequest
{
    private readonly List<DeviceSessionFilterRequest> _filters = new List<DeviceSessionFilterRequest>();

    public IReadOnlyList<DeviceSessionFilterRequest> Filters => _filters;

    public Sorting Sorting { get; private set; } = new Sorting();

    public sealed class Builder
    {
        private readonly DeviceSessionRequest _request = new DeviceSessionRequest();

        public Builder Page(int page)
        {
            _request.Sorting.Page = page;
            return this;
        }

        public Builder Size(int size)
        {
            _request.Sorting.Size = size;
            return this;
        }

        public Builder SortBy(string sortBy)
        {
            _request.Sorting.SortBy = sortBy;
            return this;
        }

        public Builder Direction(SortDirection direction)
        {
            _request.Sorting.Direction = direction;
            return this;
        }

        public Builder AddFilter(SessionFilter key, object? value, SearchOperation operation)
        {
            _request._filters.Add(DeviceSessionFilterRequest.Of(key, value, operation));
            return this;
        }

        public Builder Filters(IEnumerable<DeviceSessionFilterRequest> filters)
        {
            _request._filters.Clear();
            _request._filters.AddRange(filters);
            return this;
        }

        public DeviceSessionRequest Build()
        {
            var built = new DeviceSessionRequest { Sorting = _request.Sorting.Copy() };
            built._filters.AddRange(_request._filters);
            return built;
        }
    }
}
