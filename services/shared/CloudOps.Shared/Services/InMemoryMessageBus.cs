using System.Collections.Concurrent;
using System.Threading.Channels;
using CloudOps.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace CloudOps.Shared.Services;

public class InMemoryMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<string, Channel<TaskMessage>> _sessions = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sessionLocks = new();
    private readonly ILogger<InMemoryMessageBus> _logger;

    public InMemoryMessageBus(ILogger<InMemoryMessageBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(TaskMessage message, CancellationToken cancellationToken = default)
    {
        var channel = _sessions.GetOrAdd(message.SessionId, _ => 
            Channel.CreateUnbounded<TaskMessage>(new UnboundedChannelOptions 
            { 
                SingleReader = true,
                SingleWriter = false
            }));

        channel.Writer.TryWrite(message);
        _logger.LogInformation("Published message {Type} to session {SessionId}", message.Type, message.SessionId);
        
        return Task.CompletedTask;
    }

    public async Task<TaskMessage?> ReceiveAsync(string? sessionId = null, CancellationToken cancellationToken = default)
    {
        if (sessionId != null)
        {
            if (_sessions.TryGetValue(sessionId, out var channel))
            {
                var lockObj = _sessionLocks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
                var lockAcquired = false;
                TaskMessage? message = null;
                
                try
                {
                    await lockObj.WaitAsync(cancellationToken);
                    lockAcquired = true;
                    
                    if (await channel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        if (channel.Reader.TryRead(out message))
                        {
                            return message;
                        }
                    }
                    
                    return null;
                }
                finally
                {
                    if (lockAcquired && message == null)
                    {
                        lockObj.Release();
                    }
                }
            }
            return null;
        }

        foreach (var kvp in _sessions)
        {
            if (await kvp.Value.Reader.WaitToReadAsync(TimeSpan.Zero, cancellationToken))
            {
                var lockObj = _sessionLocks.GetOrAdd(kvp.Key, _ => new SemaphoreSlim(1, 1));
                var lockAcquired = false;
                TaskMessage? message = null;
                
                try
                {
                    lockAcquired = await lockObj.WaitAsync(0, cancellationToken);
                    if (lockAcquired)
                    {
                        if (kvp.Value.Reader.TryRead(out message))
                        {
                            return message;
                        }
                    }
                }
                finally
                {
                    if (lockAcquired && message == null)
                    {
                        lockObj.Release();
                    }
                }
            }
        }

        return null;
    }

    public Task CompleteAsync(TaskMessage message, CancellationToken cancellationToken = default)
    {
        if (_sessionLocks.TryGetValue(message.SessionId, out var lockObj))
        {
            try
            {
                lockObj.Release();
                _logger.LogInformation("Completed message {Type} from session {SessionId}", message.Type, message.SessionId);
            }
            catch (SemaphoreFullException ex)
            {
                _logger.LogError(ex, "Attempted to release semaphore that was already released for session {SessionId}", message.SessionId);
            }
        }
        
        return Task.CompletedTask;
    }
}

public static class ChannelReaderExtensions
{
    public static async ValueTask<bool> WaitToReadAsync<T>(this ChannelReader<T> reader, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            return await reader.WaitToReadAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
