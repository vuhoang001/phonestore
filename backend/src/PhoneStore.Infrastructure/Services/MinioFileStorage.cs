using PhoneStore.Application.Common;
using PhoneStore.Application.Interfaces;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace PhoneStore.Infrastructure.Services;

/// <summary>Lưu ảnh lên MinIO, tự tạo bucket (public-read) rồi trả URL công khai để &lt;img&gt; dùng trực tiếp.</summary>
public class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly MinioSettings _cfg;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _bucketReady;

    public MinioFileStorage(IOptions<MinioSettings> cfg)
    {
        _cfg = cfg.Value;
        _client = new MinioClient()
            .WithEndpoint(_cfg.Endpoint)
            .WithCredentials(_cfg.AccessKey, _cfg.SecretKey)
            .WithSSL(_cfg.UseSsl)
            .Build();
    }

    public async Task<string> SaveImageAsync(Stream content, long length, string originalFileName, string contentType,
        string folder = "uploads", CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim('/');
        var objectName = $"{safeFolder}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";

        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_cfg.Bucket)
            .WithObject(objectName)
            .WithStreamData(content)
            .WithObjectSize(length)
            .WithContentType(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType), ct);

        return $"{_cfg.PublicEndpoint.TrimEnd('/')}/{_cfg.Bucket}/{objectName}";
    }

    /// <summary>Tạo bucket nếu chưa có và đặt policy cho phép đọc công khai (chỉ chạy 1 lần).</summary>
    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady) return;
        await _lock.WaitAsync(ct);
        try
        {
            if (_bucketReady) return;
            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_cfg.Bucket), ct);
            if (!exists)
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_cfg.Bucket), ct);

            var policy = "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\"," +
                         "\"Principal\":{\"AWS\":[\"*\"]},\"Action\":[\"s3:GetObject\"]," +
                         $"\"Resource\":[\"arn:aws:s3:::{_cfg.Bucket}/*\"]}}]}}";
            await _client.SetPolicyAsync(new SetPolicyArgs().WithBucket(_cfg.Bucket).WithPolicy(policy), ct);

            _bucketReady = true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
