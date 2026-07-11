using System.Text.Json;
using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.Outbox;

public sealed class OutboxProcessingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IIntegrationOutboxRepository _outboxRepository;
    private readonly IMunicipalityDatabaseConnectionResolver _connectionResolver;
    private readonly IReadOnlyCollection<IMunicipalityComplaintWriter> _municipalityComplaintWriters;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OutboxProcessingOptions _options;

    public OutboxProcessingService(
        IIntegrationOutboxRepository outboxRepository,
        IMunicipalityDatabaseConnectionResolver connectionResolver,
        IEnumerable<IMunicipalityComplaintWriter> municipalityComplaintWriters,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        OutboxProcessingOptions options)
    {
        _outboxRepository = outboxRepository;
        _connectionResolver = connectionResolver;
        _municipalityComplaintWriters = municipalityComplaintWriters.ToArray();
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<int> ProcessDueMessagesAsync(CancellationToken cancellationToken)
    {
        var batchSize = _options.BatchSize > 0 ? _options.BatchSize : 20;
        var messages = await _outboxRepository.GetDueAsync(
            _dateTimeProvider.UtcNow,
            batchSize,
            cancellationToken);

        var processedCount = 0;
        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, cancellationToken);
            processedCount++;
        }

        return processedCount;
    }

    public async Task ProcessMessageAsync(
        IntegrationOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var attempt = message.MarkProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = await DispatchSafelyAsync(message, cancellationToken);
        if (result.IsSuccess)
        {
            attempt.Complete();
            message.MarkCompleted();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var failureReason = result.Error ?? "Outbox message could not be processed.";
        attempt.Fail(failureReason);

        if (message.AttemptCount >= GetMaxRetryCount())
        {
            message.MarkFailed(failureReason);
        }
        else
        {
            message.MarkRetryScheduled(failureReason, CalculateNextRetryAt(message.AttemptCount));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result> DispatchAsync(
        IntegrationOutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (string.Equals(message.MessageType, "AdminCommentAdded", StringComparison.Ordinal))
        {
            // Admin comments (internal or citizen-visible) are not currently mirrored to
            // municipality databases - the sample municipality schema has no comments table.
            // The outbox message still exists so the main-DB write and audit trail stay
            // consistent with the no-dual-write rule; there is simply nothing external to
            // dispatch for this message type yet.
            return Result.Success();
        }

        var connectionResult = await _connectionResolver.ResolveAsync(message.MunicipalityId, cancellationToken);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            return Result.Failure(connectionResult.Error ?? "Municipality database connection could not be resolved.");
        }

        var writer = _municipalityComplaintWriters.FirstOrDefault(candidate => candidate.Provider == connectionResult.Value.Provider);
        if (writer is null)
        {
            return Result.Failure($"No municipality complaint writer is registered for {connectionResult.Value.Provider}.");
        }

        return message.MessageType switch
        {
            "ComplaintCreated" => await DispatchComplaintCreatedAsync(writer, connectionResult.Value, message, cancellationToken),
            "ComplaintStatusChanged" => await DispatchComplaintStatusChangedAsync(writer, connectionResult.Value, message, cancellationToken),
            "ComplaintAssigned" => await DispatchComplaintAssignedAsync(writer, connectionResult.Value, message, cancellationToken),
            _ => Result.Failure($"Unsupported outbox message type: {message.MessageType}.")
        };
    }

    private static async Task<Result> DispatchComplaintCreatedAsync(
        IMunicipalityComplaintWriter writer,
        MunicipalityDatabaseConnectionInfo connectionInfo,
        IntegrationOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payloadResult = DeserializePayload<MunicipalityComplaintCreatedPayload>(message.Payload);
        if (!payloadResult.IsSuccess || payloadResult.Value is null)
        {
            return Result.Failure(payloadResult.Error ?? "Outbox payload is empty.");
        }

        var writeResult = await writer.WriteComplaintCreatedAsync(connectionInfo, payloadResult.Value, cancellationToken);
        return writeResult.IsSuccess
            ? Result.Success()
            : Result.Failure(writeResult.Error ?? "Municipality complaint writer failed.");
    }

    private static async Task<Result> DispatchComplaintStatusChangedAsync(
        IMunicipalityComplaintWriter writer,
        MunicipalityDatabaseConnectionInfo connectionInfo,
        IntegrationOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payloadResult = DeserializePayload<ComplaintStatusChangedPayload>(message.Payload);
        if (!payloadResult.IsSuccess || payloadResult.Value is null)
        {
            return Result.Failure(payloadResult.Error ?? "Outbox payload is empty.");
        }

        var writeResult = await writer.WriteComplaintStatusChangedAsync(connectionInfo, payloadResult.Value, cancellationToken);
        return writeResult.IsSuccess
            ? Result.Success()
            : Result.Failure(writeResult.Error ?? "Municipality complaint writer failed.");
    }

    private static async Task<Result> DispatchComplaintAssignedAsync(
        IMunicipalityComplaintWriter writer,
        MunicipalityDatabaseConnectionInfo connectionInfo,
        IntegrationOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var payloadResult = DeserializePayload<ComplaintAssignedPayload>(message.Payload);
        if (!payloadResult.IsSuccess || payloadResult.Value is null)
        {
            return Result.Failure(payloadResult.Error ?? "Outbox payload is empty.");
        }

        var writeResult = await writer.WriteComplaintAssignedAsync(connectionInfo, payloadResult.Value, cancellationToken);
        return writeResult.IsSuccess
            ? Result.Success()
            : Result.Failure(writeResult.Error ?? "Municipality complaint writer failed.");
    }

    private static Result<T> DeserializePayload<T>(string payload)
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(payload, JsonOptions);
            return value is null
                ? Result<T>.Failure("Outbox payload is empty.")
                : Result<T>.Success(value);
        }
        catch (JsonException exception)
        {
            return Result<T>.Failure($"Outbox payload is invalid JSON: {exception.Message}");
        }
    }

    private async Task<Result> DispatchSafelyAsync(
        IntegrationOutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            return await DispatchAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result.Failure($"Outbox dispatch failed unexpectedly: {exception.Message}");
        }
    }

    private DateTimeOffset CalculateNextRetryAt(int attemptCount)
    {
        var initialDelaySeconds = _options.InitialRetryDelaySeconds > 0
            ? _options.InitialRetryDelaySeconds
            : 30;

        var maxDelaySeconds = _options.MaxRetryDelaySeconds > 0
            ? _options.MaxRetryDelaySeconds
            : 15 * 60;

        var exponent = Math.Max(0, attemptCount - 1);
        var delaySeconds = initialDelaySeconds * Math.Pow(2, exponent);
        var cappedDelaySeconds = Math.Min(delaySeconds, maxDelaySeconds);

        return _dateTimeProvider.UtcNow.AddSeconds(cappedDelaySeconds);
    }

    private int GetMaxRetryCount()
    {
        return _options.MaxRetryCount > 0 ? _options.MaxRetryCount : 5;
    }
}
