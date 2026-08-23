using PhoneStore.Application.Common;
using PhoneStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.API.Controllers;

/// <summary>Nhận ảnh upload và lưu lên MinIO, trả về URL công khai.</summary>
[Authorize]
public class UploadsController : BaseApiController
{
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5MB

    private readonly IFileStorage _storage;
    public UploadsController(IFileStorage storage) => _storage = storage;

    /// <summary>Ảnh sản phẩm — chỉ admin.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> UploadImage(IFormFile file) => SaveAsync(file, "products");

    /// <summary>Ảnh đánh giá — mọi người dùng đã đăng nhập.</summary>
    [HttpPost("review-image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> UploadReviewImage(IFormFile file) => SaveAsync(file, "reviews");

    /// <summary>Ảnh đại diện — mọi người dùng đã đăng nhập.</summary>
    [HttpPost("avatar")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> UploadAvatar(IFormFile file) => SaveAsync(file, "avatars");

    private async Task<IActionResult> SaveAsync(IFormFile file, string folder)
    {
        if (file is null || file.Length == 0)
            throw new AppException("Chưa chọn tệp ảnh.");
        if (file.Length > MaxBytes)
            throw new AppException("Ảnh vượt quá 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext))
            throw new AppException("Chỉ chấp nhận ảnh JPG, PNG, WEBP, GIF.");

        await using var stream = file.OpenReadStream();
        var url = await _storage.SaveImageAsync(stream, file.Length, file.FileName, file.ContentType, folder);
        return Ok(new { url });
    }
}
