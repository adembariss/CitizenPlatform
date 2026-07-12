using CitizenPlatform.Application.Abstractions;
using CitizenPlatform.Domain.Enums;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace CitizenPlatform.Infrastructure.Storage;

public sealed class MinioFileStorageService : FileStorageServiceBase, IDisposable
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly SemaphoreSlim _bucketCheckLock = new(1, 1);
    private volatile bool _bucketEnsured;

    public MinioFileStorageService(
        IOptions<ObjectStorageOptions> options,
        IFileSafetyScanner fileSafetyScanner)
        : base(options.Value, fileSafetyScanner)
    {
        var storageOptions = options.Value;

        if (string.IsNullOrWhiteSpace(storageOptions.Endpoint))
        {
            throw new InvalidOperationException("ObjectStorage:Endpoint must be configured for the Minio provider.");
        }

        if (string.IsNullOrWhiteSpace(storageOptions.BucketName))
        {
            throw new InvalidOperationException("ObjectStorage:BucketName must be configured for the Minio provider.");
        }

        var (host, port, useSsl) = ParseEndpoint(storageOptions.Endpoint, storageOptions.UseSsl);
        _bucketName = storageOptions.BucketName;

        _client = new MinioClient()
            .WithEndpoint(host, port)
            .WithCredentials(storageOptions.AccessKey, storageOptions.SecretKey)
            .WithSSL(useSsl)
            .Build();
    }

    protected override StorageProvider Provider => StorageProvider.Minio;

    protected override async Task PersistAsync(
        string objectKey,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        await using var stream = new MemoryStream(content, writable: false);
        await _client.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithStreamData(stream)
                .WithObjectSize(content.LongLength)
                .WithContentType(contentType),
            cancellationToken);
    }

    public override async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await _client.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithCallbackStream((stream, callbackCancellation) => stream.CopyToAsync(buffer, callbackCancellation)),
            cancellationToken);

        buffer.Position = 0;
        return buffer;
    }

    public override async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        await _client.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey),
            cancellationToken);
    }

    public void Dispose()
    {
        _bucketCheckLock.Dispose();
        _client.Dispose();
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured)
        {
            return;
        }

        await _bucketCheckLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketEnsured)
            {
                return;
            }

            var exists = await _client.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName),
                cancellationToken);

            if (!exists)
            {
                await _client.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucketName),
                    cancellationToken);
            }

            _bucketEnsured = true;
        }
        finally
        {
            _bucketCheckLock.Release();
        }
    }

    private static (string Host, int Port, bool UseSsl) ParseEndpoint(string endpoint, bool useSslOption)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return (uri.Host, uri.Port, uri.Scheme == Uri.UriSchemeHttps);
        }

        var parts = endpoint.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && int.TryParse(parts[1], out var port))
        {
            return (parts[0], port, useSslOption);
        }

        return (endpoint, useSslOption ? 443 : 9000, useSslOption);
    }
}
