using System;
using System.Threading.Tasks;
using QuestNav.Config;
using static QuestNav.Config.Config;

namespace QuestNav.Tests
{
    /// <summary>
    /// In-memory IConfigManager that only supports raising the pose reset timeout event
    /// </summary>
    public class FakeConfigManager : IConfigManager
    {
        public event Action<int> OnAllowedPoseResetTimeoutMsChanged;

        public void RaiseAllowedPoseResetTimeoutMs(int timeoutMs) =>
            OnAllowedPoseResetTimeoutMsChanged?.Invoke(timeoutMs);

#pragma warning disable CS0067 // Unused events
        public event Action<int> OnTeamNumberChanged;
        public event Action<string> OnDebugIpOverrideChanged;
        public event Action<bool> OnEnableAutoStartOnBootChanged;
        public event Action<bool> OnEnablePassthroughStreamChanged;
        public event Action<StreamMode> OnPassthroughStreamModeChanged;
        public event Action<bool> OnEnableHighQualityStreamsChanged;
        public event Action<bool> OnEnableAprilTagDetectorChanged;
        public event Action<AprilTagDetectorMode> OnAprilTagDetectorModeChanged;
        public event Action<int> OnAprilTagConfidencePresetChanged;
        public event Action<double> OnAprilTagNoiseScaleChanged;
        public event Action<bool> OnEnableDebugLoggingChanged;
#pragma warning restore CS0067

        public Task OpenAsync() => throw new NotSupportedException();

        public Task InitializeAsync() => throw new NotSupportedException();

        public Task CloseAsync() => throw new NotSupportedException();

        public Task<int> GetTeamNumberAsync() => throw new NotSupportedException();

        public Task<string> GetDebugIpOverrideAsync() => throw new NotSupportedException();

        public Task<int> GetAllowedPoseResetTimeoutMsAsync() => throw new NotSupportedException();

        public Task<bool> GetEnableAutoStartOnBootAsync() => throw new NotSupportedException();

        public Task<bool> GetEnablePassthroughStreamAsync() => throw new NotSupportedException();

        public Task<StreamMode> GetPassthroughStreamModeAsync() =>
            throw new NotSupportedException();

        public Task<bool> GetEnableHighQualityStreamsAsync() => throw new NotSupportedException();

        public Task<bool> GetEnableAprilTagDetectorAsync() => throw new NotSupportedException();

        public Task<AprilTagDetectorMode> GetAprilTagDetectorModeAsync() =>
            throw new NotSupportedException();

        public Task<string> GetAprilTagFieldLayoutFileAsync() => throw new NotSupportedException();

        public Task<int> GetAprilTagConfidencePresetAsync() => throw new NotSupportedException();

        public Task<double> GetAprilTagNoiseScaleAsync() => throw new NotSupportedException();

        public Task<bool> GetEnableDebugLoggingAsync() => throw new NotSupportedException();

        public Task SetTeamNumberAsync(int teamNumber) => throw new NotSupportedException();

        public Task SetDebugIpOverrideAsync(string ipOverride) => throw new NotSupportedException();

        public Task SetAllowedPoseResetTimeoutMsAsync(int timeoutMs) =>
            throw new NotSupportedException();

        public Task SetEnableAutoStartOnBootAsync(bool autoStart) =>
            throw new NotSupportedException();

        public Task SetEnablePassthroughStreamAsync(bool enable) =>
            throw new NotSupportedException();

        public Task SetPassthroughStreamModeAsync(StreamMode mode) =>
            throw new NotSupportedException();

        public Task SetEnableHighQualityStreamsAsync(bool enabled) =>
            throw new NotSupportedException();

        public Task SetEnableAprilTagDetectorAsync(bool enable) =>
            throw new NotSupportedException();

        public Task SetAprilTagDetectorModeAsnyc(AprilTagDetectorMode mode) =>
            throw new NotSupportedException();

        public Task SetAprilTagFieldLayoutFileAsync(string fileName) =>
            throw new NotSupportedException();

        public Task SetAprilTagConfidencePresetAsync(int preset) =>
            throw new NotSupportedException();

        public Task SetAprilTagNoiseScaleAsync(double scale) => throw new NotSupportedException();

        public Task SetEnableDebugLoggingAsync(bool enableDebugLogging) =>
            throw new NotSupportedException();

        public Task ResetToDefaultsAsync() => throw new NotSupportedException();
    }
}
