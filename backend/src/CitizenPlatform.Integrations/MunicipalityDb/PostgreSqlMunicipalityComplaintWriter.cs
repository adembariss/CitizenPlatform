using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Enums;
using Npgsql;
using NpgsqlTypes;

namespace CitizenPlatform.Integrations.MunicipalityDb;

public sealed class PostgreSqlMunicipalityComplaintWriter : IMunicipalityComplaintWriter
{
    public MunicipalityDbProvider Provider => MunicipalityDbProvider.PostgreSql;

    public async Task<Result<MunicipalityComplaintWriteResult>> WriteComplaintCreatedAsync(
        MunicipalityDatabaseConnectionInfo connectionInfo,
        MunicipalityComplaintCreatedPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionInfo.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                WITH inserted_complaint AS (
                    INSERT INTO municipal_complaints (
                        main_complaint_id,
                        municipality_id,
                        tracking_code,
                        category_name,
                        department_name,
                        citizen_full_name,
                        citizen_phone_number,
                        citizen_email,
                        description,
                        address_text,
                        latitude,
                        longitude,
                        status,
                        priority,
                        created_at,
                        synced_at
                    )
                    VALUES (
                        @main_complaint_id,
                        @municipality_id,
                        @tracking_code,
                        @category_name,
                        @department_name,
                        @citizen_full_name,
                        @citizen_phone_number,
                        @citizen_email,
                        @description,
                        @address_text,
                        @latitude,
                        @longitude,
                        @status,
                        @priority,
                        @created_at,
                        now()
                    )
                    ON CONFLICT (main_complaint_id) DO NOTHING
                    RETURNING id, main_complaint_id, status, created_at
                ),
                inserted_log AS (
                    INSERT INTO municipal_complaint_status_logs (
                        municipal_complaint_id,
                        main_complaint_id,
                        status,
                        note,
                        created_at
                    )
                    SELECT
                        id,
                        main_complaint_id,
                        status,
                        'Complaint created in CitizenPlatform.',
                        created_at
                    FROM inserted_complaint
                    ON CONFLICT (main_complaint_id, status) DO NOTHING
                )
                SELECT EXISTS(SELECT 1 FROM inserted_complaint);
                """;

            AddParameters(command, payload);
            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var wasInserted = scalar is bool value && value;
            return Result<MunicipalityComplaintWriteResult>.Success(new MunicipalityComplaintWriteResult(wasInserted));
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return Result<MunicipalityComplaintWriteResult>.Failure(
                $"Municipality PostgreSQL write failed: {exception.Message}");
        }
    }

    public async Task<Result<MunicipalityComplaintWriteResult>> WriteComplaintStatusChangedAsync(
        MunicipalityDatabaseConnectionInfo connectionInfo,
        ComplaintStatusChangedPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionInfo.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            Guid? municipalComplaintId;
            await using (var updateCommand = connection.CreateCommand())
            {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = """
                    UPDATE municipal_complaints
                    SET status = @status
                    WHERE main_complaint_id = @main_complaint_id
                    RETURNING id;
                    """;
                updateCommand.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, payload.NewStatus);
                updateCommand.Parameters.AddWithValue("main_complaint_id", NpgsqlDbType.Uuid, payload.ComplaintId);

                var result = await updateCommand.ExecuteScalarAsync(cancellationToken);
                municipalComplaintId = result is Guid id ? id : null;
            }

            if (municipalComplaintId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<MunicipalityComplaintWriteResult>.Failure(
                    "Municipality complaint record not found yet; will retry once ComplaintCreated has synced.");
            }

            await using (var logCommand = connection.CreateCommand())
            {
                logCommand.Transaction = transaction;
                logCommand.CommandText = """
                    INSERT INTO municipal_complaint_status_logs
                        (municipal_complaint_id, main_complaint_id, status, note, created_at)
                    VALUES
                        (@municipal_complaint_id, @main_complaint_id, @status, @note, @created_at)
                    ON CONFLICT (main_complaint_id, status) DO NOTHING;
                    """;
                logCommand.Parameters.AddWithValue("municipal_complaint_id", NpgsqlDbType.Uuid, municipalComplaintId.Value);
                logCommand.Parameters.AddWithValue("main_complaint_id", NpgsqlDbType.Uuid, payload.ComplaintId);
                logCommand.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, payload.NewStatus);
                logCommand.Parameters.AddWithValue("note", NpgsqlDbType.Text, ToDbValue(payload.Note));
                logCommand.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, payload.ChangedAt);

                await logCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result<MunicipalityComplaintWriteResult>.Success(new MunicipalityComplaintWriteResult(true));
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return Result<MunicipalityComplaintWriteResult>.Failure(
                $"Municipality PostgreSQL status update failed: {exception.Message}");
        }
    }

    public async Task<Result<MunicipalityComplaintWriteResult>> WriteComplaintAssignedAsync(
        MunicipalityDatabaseConnectionInfo connectionInfo,
        ComplaintAssignedPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionInfo.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE municipal_complaints
                SET department_name = @department_name
                WHERE main_complaint_id = @main_complaint_id
                RETURNING id;
                """;
            command.Parameters.AddWithValue("department_name", NpgsqlDbType.Varchar, payload.DepartmentName);
            command.Parameters.AddWithValue("main_complaint_id", NpgsqlDbType.Uuid, payload.ComplaintId);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is not Guid)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<MunicipalityComplaintWriteResult>.Failure(
                    "Municipality complaint record not found yet; will retry once ComplaintCreated has synced.");
            }

            await transaction.CommitAsync(cancellationToken);
            return Result<MunicipalityComplaintWriteResult>.Success(new MunicipalityComplaintWriteResult(true));
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return Result<MunicipalityComplaintWriteResult>.Failure(
                $"Municipality PostgreSQL assignment update failed: {exception.Message}");
        }
    }

    private static void AddParameters(NpgsqlCommand command, MunicipalityComplaintCreatedPayload payload)
    {
        command.Parameters.AddWithValue("main_complaint_id", NpgsqlDbType.Uuid, payload.ComplaintId);
        command.Parameters.AddWithValue("municipality_id", NpgsqlDbType.Uuid, payload.MunicipalityId);
        command.Parameters.AddWithValue("tracking_code", NpgsqlDbType.Varchar, payload.TrackingCode);
        command.Parameters.AddWithValue("category_name", NpgsqlDbType.Varchar, payload.CategoryName);
        command.Parameters.AddWithValue("department_name", NpgsqlDbType.Varchar, ToDbValue(payload.DepartmentName));
        command.Parameters.AddWithValue("citizen_full_name", NpgsqlDbType.Varchar, ToDbValue(payload.CitizenFullName));
        command.Parameters.AddWithValue("citizen_phone_number", NpgsqlDbType.Varchar, ToDbValue(payload.CitizenPhoneNumber));
        command.Parameters.AddWithValue("citizen_email", NpgsqlDbType.Varchar, ToDbValue(payload.CitizenEmail));
        command.Parameters.AddWithValue("description", NpgsqlDbType.Text, payload.Description);
        command.Parameters.AddWithValue("address_text", NpgsqlDbType.Text, ToDbValue(payload.AddressText));
        command.Parameters.AddWithValue("latitude", NpgsqlDbType.Double, payload.Latitude);
        command.Parameters.AddWithValue("longitude", NpgsqlDbType.Double, payload.Longitude);
        command.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, payload.Status);
        command.Parameters.AddWithValue("priority", NpgsqlDbType.Varchar, payload.Priority);
        command.Parameters.AddWithValue("created_at", NpgsqlDbType.TimestampTz, payload.CreatedAt);
    }

    private static object ToDbValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }
}
