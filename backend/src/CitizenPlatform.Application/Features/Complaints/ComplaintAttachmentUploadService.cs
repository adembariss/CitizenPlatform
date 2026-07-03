using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Application.Common.Models;
using CitizenPlatform.Application.DTOs;
using CitizenPlatform.Domain.Entities;

namespace CitizenPlatform.Application.Features.Complaints;

public sealed class ComplaintAttachmentUploadService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageMetadataReader _imageMetadataReader;

    public ComplaintAttachmentUploadService(
        IFileStorageService fileStorageService,
        IImageMetadataReader imageMetadataReader)
    {
        _fileStorageService = fileStorageService;
        _imageMetadataReader = imageMetadataReader;
    }

    public async Task<Result<IReadOnlyCollection<ComplaintAttachmentDto>>> AddUploadsAsync(
        Complaint complaint,
        IReadOnlyCollection<ComplaintAttachmentUpload>? uploads,
        CancellationToken cancellationToken)
    {
        if (uploads is null || uploads.Count == 0)
        {
            return Result<IReadOnlyCollection<ComplaintAttachmentDto>>.Success(Array.Empty<ComplaintAttachmentDto>());
        }

        var attachmentDtos = new List<ComplaintAttachmentDto>(uploads.Count);
        var savedObjectKeys = new List<string>(uploads.Count);

        foreach (var upload in uploads)
        {
            var saveResult = await SaveUploadAsync(upload, cancellationToken);
            if (!saveResult.IsSuccess || saveResult.Value is null)
            {
                await DeleteSavedFilesAsync(savedObjectKeys, cancellationToken);
                return Result<IReadOnlyCollection<ComplaintAttachmentDto>>.Failure(
                    saveResult.Error ?? "Attachment could not be saved.",
                    saveResult.Errors);
            }

            savedObjectKeys.Add(saveResult.Value.ObjectKey);
            var metadata = await ReadMetadataAsync(saveResult.Value, cancellationToken);
            var attachment = complaint.AddAttachment(
                saveResult.Value.FileName,
                saveResult.Value.OriginalFileName,
                saveResult.Value.ContentType,
                saveResult.Value.SizeInBytes,
                saveResult.Value.Sha256Hash,
                saveResult.Value.StorageProvider,
                saveResult.Value.ObjectKey,
                photoExifLocation: metadata.GpsLocation,
                photoTakenAt: metadata.TakenAt);

            attachmentDtos.Add(new ComplaintAttachmentDto(
                attachment.Id,
                attachment.FileName,
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.SizeInBytes,
                attachment.Sha256Hash,
                attachment.PhotoExifLocation?.Latitude,
                attachment.PhotoExifLocation?.Longitude,
                attachment.PhotoTakenAt));
        }

        return Result<IReadOnlyCollection<ComplaintAttachmentDto>>.Success(attachmentDtos);
    }

    private async Task<Result<FileStorageSaveResult>> SaveUploadAsync(
        ComplaintAttachmentUpload upload,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var content = upload.OpenReadStream();
            return await _fileStorageService.SaveAsync(
                new FileStorageSaveRequest(
                    content,
                    upload.FileName,
                    upload.ContentType,
                    upload.Length),
                cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Result<FileStorageSaveResult>.Failure("Attachment stream could not be read.");
        }
    }

    private async Task<ImageMetadata> ReadMetadataAsync(
        FileStorageSaveResult saveResult,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var storedContent = await _fileStorageService.OpenReadAsync(saveResult.ObjectKey, cancellationToken);
            return await _imageMetadataReader.ReadAsync(storedContent, saveResult.ContentType, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return ImageMetadata.Empty;
        }
    }

    private async Task DeleteSavedFilesAsync(
        IReadOnlyCollection<string> objectKeys,
        CancellationToken cancellationToken)
    {
        foreach (var objectKey in objectKeys)
        {
            await _fileStorageService.DeleteAsync(objectKey, cancellationToken);
        }
    }
}
