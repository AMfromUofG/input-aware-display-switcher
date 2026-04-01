namespace InputAwareDisplaySwitcher.Core.Domain.Switching;

public sealed record SwitchExecutionResult
{
    public string? DisplayProfileId { get; init; }

    public required SwitchExecutionStatus Status { get; init; }

    public required string ExecutionPath { get; init; }

    public string? ErrorMessage { get; init; }

    public int? ErrorCode { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool WasAttempted => Status is SwitchExecutionStatus.Succeeded or SwitchExecutionStatus.Failed;

    public bool Success => Status == SwitchExecutionStatus.Succeeded;

    public static SwitchExecutionResult NotAttempted(string? displayProfileId, string message)
    {
        return new SwitchExecutionResult
        {
            DisplayProfileId = displayProfileId,
            Status = SwitchExecutionStatus.NotAttempted,
            ExecutionPath = "Not attempted",
            ErrorMessage = message
        };
    }

    public static SwitchExecutionResult Unsupported(string? displayProfileId, string executionPath, string message)
    {
        return new SwitchExecutionResult
        {
            DisplayProfileId = displayProfileId,
            Status = SwitchExecutionStatus.Unsupported,
            ExecutionPath = executionPath,
            ErrorMessage = message
        };
    }

    public static SwitchExecutionResult Failure(string? displayProfileId, string executionPath, string message, int? errorCode = null)
    {
        return new SwitchExecutionResult
        {
            DisplayProfileId = displayProfileId,
            Status = SwitchExecutionStatus.Failed,
            ExecutionPath = executionPath,
            ErrorMessage = message,
            ErrorCode = errorCode
        };
    }

    public static SwitchExecutionResult Succeeded(string? displayProfileId, string executionPath)
    {
        return new SwitchExecutionResult
        {
            DisplayProfileId = displayProfileId,
            Status = SwitchExecutionStatus.Succeeded,
            ExecutionPath = executionPath
        };
    }
}
