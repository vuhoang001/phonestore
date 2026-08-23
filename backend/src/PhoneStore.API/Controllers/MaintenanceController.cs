using System.Collections.Concurrent;
using PhoneStore.Application.Common;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace PhoneStore.API.Controllers;

/// <summary>Tác vụ bảo trì cho admin (di trú dữ liệu...).</summary>
[Authorize(Roles = "Admin")]
public class MaintenanceController : BaseApiController
{
    private readonly IAppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IHttpClientFactory _httpFactory;
    private readonly MinioSettings _minio;
    private readonly IWebHostEnvironment _env;

    public MaintenanceController(IAppDbContext db, IFileStorage storage,
        IHttpClientFactory httpFactory, IOptions<MinioSettings> minio, IWebHostEnvironment env)
    {
        _db = db;
        _storage = storage;
        _httpFactory = httpFactory;
        _minio = minio.Value;
        _env = env;
    }

    /// <summary>
    /// Đẩy ảnh sản phẩm cũ lên MinIO rồi cập nhật URL. Nhận cả nguồn ngoài (http, tải về)
    /// lẫn ảnh local phục vụ qua /uploads (đọc từ wwwroot). Idempotent — bỏ qua ảnh đã ở MinIO.
    /// </summary>
    [HttpPost("migrate-product-images")]
    public async Task<IActionResult> MigrateProductImages(CancellationToken ct)
    {
        var publicHost = _minio.PublicEndpoint.TrimEnd('/');
        var pending = await _db.ProductImages
            .Where(i => (i.Url.StartsWith("http") && !i.Url.StartsWith(publicHost)) || i.Url.StartsWith("/uploads"))
            .ToListAsync(ct);

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(30);

        var results = new ConcurrentDictionary<int, string>();
        await Parallel.ForEachAsync(pending,
            new ParallelOptions { MaxDegreeOfParallelism = 6, CancellationToken = ct },
            async (img, token) =>
            {
                try
                {
                    byte[] bytes;
                    string ext;
                    if (img.Url.StartsWith('/'))
                    {
                        // Ảnh local phục vụ qua static files → đọc trực tiếp từ wwwroot.
                        var path = Path.Combine(webRoot, img.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (!System.IO.File.Exists(path)) return;
                        bytes = await System.IO.File.ReadAllBytesAsync(path, token);
                        ext = Path.GetExtension(path);
                    }
                    else
                    {
                        bytes = await http.GetByteArrayAsync(img.Url, token);
                        ext = Path.GetExtension(new Uri(img.Url).AbsolutePath);
                    }
                    if (bytes.Length == 0) return;
                    using var ms = new MemoryStream(bytes);
                    var newUrl = await _storage.SaveImageAsync(ms, ms.Length, $"img{ext}", ContentTypeFor(ext), "products", token);
                    results[img.Id] = newUrl;
                }
                catch { /* lỗi ảnh nào thì giữ URL cũ ảnh đó */ }
            });

        foreach (var img in pending)
            if (results.TryGetValue(img.Id, out var url)) img.Url = url;
        await _db.SaveChangesAsync(ct);

        return Ok(new { candidates = pending.Count, migrated = results.Count, failed = pending.Count - results.Count });
    }

    private static string ContentTypeFor(string ext) => ext.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "image/jpeg"
    };
}
