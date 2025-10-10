using CloudOps.Shared.DTOs;

namespace CloudOps.Shared.Services;

public interface IMessageBus
{
    Task PublishAsync(TaskMessage message, CancellationToken cancellationToken = default);
    Task<TaskMessage?> ReceiveAsync(string? sessionId = null, CancellationToken cancellationToken = default);
    Task CompleteAsync(TaskMessage message, CancellationToken cancellationToken = default);
}
