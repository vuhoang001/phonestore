using PhoneStore.Application.Common;
using PhoneStore.Application.DTOs;
using PhoneStore.Application.Interfaces;
using PhoneStore.Domain.Entities;
using PhoneStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Application.Services;

public class ProductService : IProductService
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    public ProductService(IAppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<PagedResult<ProductListItemDto>> SearchAsync(ProductFilterDto filter)
    {
        var query = _db.Products.AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Where(p => p.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(kw));
        }
        if (filter.CategoryId is int catId)
        {
            // Nếu là danh mục cha, lấy luôn sản phẩm của các danh mục con.
            var catIds = await _db.Categories
                .Where(c => c.Id == catId || c.ParentId == catId)
                .Select(c => c.Id).ToListAsync();
            query = query.Where(p => catIds.Contains(p.CategoryId));
        }
        if (filter.BrandId is int brandId)
            query = query.Where(p => p.BrandId == brandId);
        if (filter.MinPrice is decimal min)
            query = query.Where(p => p.BasePrice >= min);
        if (filter.MaxPrice is decimal max)
            query = query.Where(p => p.BasePrice <= max);
        if (filter.MinRating is int rating)
            query = query.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= rating);

        query = filter.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "rating" => query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var page = Math.Max(1, filter.Page);
        var size = filter.PageSize is > 0 and <= 100 ? filter.PageSize : 12;

        var now = DateTime.UtcNow;
        var items = await query
            .Skip((page - 1) * size).Take(size)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                BasePrice = p.BasePrice,
                BrandName = p.Brand != null ? p.Brand.Name : string.Empty,
                PrimaryImage = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                    ?? p.Images.Select(i => i.Url).FirstOrDefault(),
                AverageRating = p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = p.Reviews.Count,
                TotalStock = p.Variants.Sum(v => v.StockQuantity),
                SoldCount = p.SoldCount
            })
            .ToListAsync();

        items = await ApplyFlashPricesAsync(items, now);

        // Ghi nhật ký từ khóa tìm kiếm (phục vụ báo cáo search / no-result).
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            _db.SearchLogs.Add(new Domain.Entities.SearchLog
            {
                Keyword = filter.Keyword.Trim().ToLower(),
                ResultCount = total
            });
            await _db.SaveChangesAsync();
        }

        return new PagedResult<ProductListItemDto>
        {
            Items = items, Page = page, PageSize = size, TotalItems = total
        };
    }

    public async Task<ProductDetailDto> GetBySlugAsync(string slug)
    {
        // Tăng lượt xem cho sản phẩm được mở.
        var tracked = await _db.Products.FirstOrDefaultAsync(p => p.Slug == slug);
        if (tracked != null)
        {
            tracked.ViewCount++;
            await _db.SaveChangesAsync();
        }
        return await LoadDetail(p => p.Slug == slug);
    }

    public Task<ProductDetailDto> GetByIdAsync(int id) => LoadDetail(p => p.Id == id);

    public async Task<List<ProductListItemDto>> GetRelatedAsync(int productId)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw AppException.NotFound("Không tìm thấy sản phẩm.");

        var now = DateTime.UtcNow;
        var related = await _db.Products.AsNoTracking()
            .Include(p => p.Brand).Include(p => p.Variants).Include(p => p.Images).Include(p => p.Reviews)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != productId && p.Status == ProductStatus.Active)
            .OrderByDescending(p => p.SoldCount).ThenByDescending(p => p.CreatedAt)
            .Take(6)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id, Name = p.Name, Slug = p.Slug, BasePrice = p.BasePrice,
                BrandName = p.Brand != null ? p.Brand.Name : string.Empty,
                PrimaryImage = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                    ?? p.Images.Select(i => i.Url).FirstOrDefault(),
                AverageRating = p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = p.Reviews.Count,
                TotalStock = p.Variants.Sum(v => v.StockQuantity),
                SoldCount = p.SoldCount
            })
            .ToListAsync();
        return await ApplyFlashPricesAsync(related, now);
    }

    // Gắn giá Flash Sale đang chạy vào danh sách sản phẩm (query riêng + map, đáng tin hơn subquery
    // tương quan trong projection). Chỉ gắn khi giá flash < giá gốc.
    private async Task<List<ProductListItemDto>> ApplyFlashPricesAsync(List<ProductListItemDto> items, DateTime now)
    {
        if (items.Count == 0) return items;
        var ids = items.Select(i => i.Id).ToList();
        var map = await _db.FlashSaleItems.AsNoTracking()
            .Where(fi => ids.Contains(fi.ProductId)
                && fi.FlashSale.IsActive && fi.FlashSale.StartAt <= now && fi.FlashSale.EndAt > now)
            .GroupBy(fi => fi.ProductId)
            .Select(g => new { ProductId = g.Key, Price = g.Min(x => x.FlashPrice) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Price);
        return items
            .Select(i => map.TryGetValue(i.Id, out var fp) && fp < i.BasePrice ? i with { FlashPrice = fp } : i)
            .ToList();
    }

    private async Task<ProductDetailDto> LoadDetail(System.Linq.Expressions.Expression<Func<Product, bool>> predicate)
    {
        var p = await _db.Products.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Variants)
            .Include(x => x.Images)
            .Include(x => x.Specifications)
            .Include(x => x.Reviews)
            .FirstOrDefaultAsync(predicate)
            ?? throw AppException.NotFound("Không tìm thấy sản phẩm.");

        // Giá Flash Sale đang chạy cho sản phẩm này (nếu có) → trang chi tiết hiển thị đúng giá.
        var now = DateTime.UtcNow;
        var flash = await _db.FlashSaleItems.AsNoTracking()
            .Where(fi => fi.ProductId == p.Id && fi.FlashPrice < p.BasePrice
                && fi.FlashSale.IsActive && fi.FlashSale.StartAt <= now && fi.FlashSale.EndAt > now)
            .OrderBy(fi => fi.FlashPrice)
            .Select(fi => new { fi.FlashPrice, fi.FlashSale.EndAt })
            .FirstOrDefaultAsync();
        return ToDetail(p, flash?.FlashPrice, flash?.EndAt);
    }

    public async Task<ProductDetailDto> CreateAsync(CreateProductDto dto)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            throw AppException.NotFound("Danh mục không tồn tại.");
        if (!await _db.Brands.AnyAsync(b => b.Id == dto.BrandId))
            throw AppException.NotFound("Thương hiệu không tồn tại.");

        var product = new Product
        {
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            Name = dto.Name.Trim(),
            Slug = await UniqueSlug(SlugHelper.Generate(dto.Name)),
            Description = dto.Description,
            BasePrice = dto.BasePrice,
            WarrantyMonths = dto.WarrantyMonths > 0 ? dto.WarrantyMonths : 12,
            InstallmentAvailable = dto.InstallmentAvailable,
            Status = Enum.TryParse<ProductStatus>(dto.Status, out var st) ? st : ProductStatus.Active
        };

        // Sinh biến thể theo tổ hợp Màu × Dung lượng client gửi lên.
        foreach (var v in dto.Variants)
        {
            product.Variants.Add(new ProductVariant
            {
                Sku = string.IsNullOrWhiteSpace(v.Sku) ? GenerateSku(product.Name, v) : v.Sku.Trim(),
                Color = v.Color, ColorHex = v.ColorHex, Storage = v.Storage,
                Price = v.Price, Cost = v.Cost, StockQuantity = v.StockQuantity
            });
        }
        // Nếu không khai báo biến thể, tạo 1 biến thể mặc định theo giá gốc
        if (product.Variants.Count == 0)
            product.Variants.Add(new ProductVariant { Sku = GenerateSku(product.Name, null), Price = dto.BasePrice, StockQuantity = 0 });

        foreach (var img in dto.Images)
            product.Images.Add(new ProductImage { Url = img.Url, IsPrimary = img.IsPrimary, SortOrder = img.SortOrder });

        // Thông số kỹ thuật gom nhóm (Màn hình/Chip/Camera...).
        var order = 0;
        foreach (var s in dto.Specifications)
            product.Specifications.Add(new ProductSpecification
            {
                Group = s.Group.Trim(), Name = s.Name.Trim(), Value = s.Value.Trim(), SortOrder = order++
            });

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("ProductCreated", "Product", product.Id, product.Name);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDetailDto> UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _db.Products
            .Include(x => x.Variants)
            .Include(x => x.Specifications)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw AppException.NotFound("Không tìm thấy sản phẩm.");
        if (!await _db.Brands.AnyAsync(b => b.Id == dto.BrandId))
            throw AppException.NotFound("Thương hiệu không tồn tại.");

        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;
        product.Name = dto.Name.Trim();
        product.Description = dto.Description;
        product.BasePrice = dto.BasePrice;
        product.WarrantyMonths = dto.WarrantyMonths > 0 ? dto.WarrantyMonths : product.WarrantyMonths;
        product.InstallmentAvailable = dto.InstallmentAvailable;
        product.Status = Enum.TryParse<ProductStatus>(dto.Status, out var st) ? st : product.Status;
        product.UpdatedAt = DateTime.UtcNow;

        // Đồng bộ biến thể nếu client gửi danh sách (rỗng => giữ nguyên, không đụng tồn kho cũ)
        if (dto.Variants.Count > 0)
            await SyncVariantsAsync(product, dto.Variants);

        // Đồng bộ thông số: nếu client gửi danh sách thì thay toàn bộ (rỗng => giữ nguyên).
        if (dto.Specifications.Count > 0)
        {
            _db.ProductSpecifications.RemoveRange(product.Specifications);
            product.Specifications.Clear();
            var order = 0;
            foreach (var s in dto.Specifications)
                product.Specifications.Add(new ProductSpecification
                {
                    Group = s.Group.Trim(), Name = s.Name.Trim(), Value = s.Value.Trim(), SortOrder = order++
                });
        }

        await _audit.LogAsync("ProductUpdated", "Product", id, product.Name);
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    // Cập nhật biến thể sẵn có, thêm biến thể mới (Id=0), xoá biến thể bị bỏ khỏi form.
    // Chặn xoá biến thể đã phát sinh đơn hàng để giữ toàn vẹn dữ liệu (snapshot vẫn cần FK).
    private async Task SyncVariantsAsync(Product product, List<UpdateVariantDto> variants)
    {
        var keepIds = variants.Where(v => v.Id > 0).Select(v => v.Id).ToHashSet();

        foreach (var old in product.Variants.Where(v => !keepIds.Contains(v.Id)).ToList())
        {
            if (await _db.OrderItems.AnyAsync(oi => oi.VariantId == old.Id))
                throw AppException.Conflict($"Không thể xoá biến thể \"{old.Sku}\" vì đã có đơn hàng. Hãy đặt tồn kho = 0 thay vì xoá.");
            product.Variants.Remove(old);
        }

        foreach (var v in variants)
        {
            if (v.Id > 0)
            {
                var ev = product.Variants.FirstOrDefault(x => x.Id == v.Id);
                if (ev == null) continue; // client gửi id lạ -> bỏ qua
                ev.Color = v.Color; ev.ColorHex = v.ColorHex; ev.Storage = v.Storage;
                ev.Price = v.Price; ev.Cost = v.Cost; ev.StockQuantity = v.StockQuantity;
                if (!string.IsNullOrWhiteSpace(v.Sku)) ev.Sku = v.Sku.Trim();
            }
            else
            {
                product.Variants.Add(new ProductVariant
                {
                    Sku = string.IsNullOrWhiteSpace(v.Sku)
                        ? GenerateSku(product.Name, new CreateVariantDto { Color = v.Color, Storage = v.Storage })
                        : v.Sku.Trim(),
                    Color = v.Color, ColorHex = v.ColorHex, Storage = v.Storage,
                    Price = v.Price, Cost = v.Cost, StockQuantity = v.StockQuantity
                });
            }
        }

        if (product.Variants.Count == 0)
            throw AppException.Conflict("Sản phẩm phải có ít nhất 1 biến thể.");
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id)
            ?? throw AppException.NotFound("Không tìm thấy sản phẩm.");
        // Xóa mềm: giữ lịch sử đơn hàng/đánh giá, ẩn khỏi catalog.
        product.DeletedAt = DateTime.UtcNow;
        await _audit.LogAsync("ProductDeleted", "Product", id, product.Name);
        await _db.SaveChangesAsync();
    }

    private async Task<string> UniqueSlug(string baseSlug)
    {
        var slug = baseSlug;
        var i = 1;
        while (await _db.Products.AnyAsync(p => p.Slug == slug))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    // SKU sinh theo tên máy + tổ hợp Màu/Dung lượng.
    private static string GenerateSku(string productName, CreateVariantDto? v)
    {
        var prefix = new string(SlugHelper.Generate(productName).Where(char.IsLetterOrDigit).Take(6).ToArray()).ToUpper();
        var suffix = v is null ? "STD" : $"{v.Color}{v.Storage}".ToUpper();
        var rnd = Guid.NewGuid().ToString("N")[..4].ToUpper();
        return $"{prefix}-{suffix}-{rnd}".Replace("--", "-");
    }

    // Ước tính trả góp (mock, lãi 0% demo — chia đều) cho 6/9/12 tháng khi máy hỗ trợ trả góp.
    private static readonly int[] InstallmentTerms = { 6, 9, 12 };
    private static List<InstallmentOptionDto> BuildInstallmentOptions(decimal basePrice)
    {
        if (basePrice <= 0) return new();
        return InstallmentTerms
            .Select(m => new InstallmentOptionDto(m, Math.Round(basePrice / m, 0)))
            .ToList();
    }

    private static ProductDetailDto ToDetail(Product p, decimal? flashPrice = null, DateTime? flashEndAt = null) => new()
    {
        FlashPrice = flashPrice,
        FlashEndAt = flashEndAt,
        Id = p.Id,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? string.Empty,
        BrandId = p.BrandId,
        BrandName = p.Brand?.Name ?? string.Empty,
        Name = p.Name,
        Slug = p.Slug,
        Description = p.Description,
        BasePrice = p.BasePrice,
        Status = p.Status.ToString(),
        WarrantyMonths = p.WarrantyMonths,
        InstallmentAvailable = p.InstallmentAvailable,
        Variants = p.Variants.Select(v => new ProductVariantDto
        {
            Id = v.Id, Sku = v.Sku, Color = v.Color, ColorHex = v.ColorHex, Storage = v.Storage,
            Price = v.Price, StockQuantity = v.StockQuantity
        }).ToList(),
        Images = p.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto
        {
            Id = i.Id, VariantId = i.VariantId, Url = i.Url, IsPrimary = i.IsPrimary, SortOrder = i.SortOrder
        }).ToList(),
        Specifications = p.Specifications.OrderBy(s => s.SortOrder)
            .Select(s => new ProductSpecDto(s.Group, s.Name, s.Value)).ToList(),
        // Trả góp chỉ tính giá trị khi máy bật InstallmentAvailable (lãi 0% cho demo).
        InstallmentOptions = p.InstallmentAvailable ? BuildInstallmentOptions(p.BasePrice) : new List<InstallmentOptionDto>(),
        AverageRating = p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0,
        ReviewCount = p.Reviews.Count,
        ViewCount = p.ViewCount,
        SoldCount = p.SoldCount,
        // Số lượng review theo mức sao [5,4,3,2,1]
        RatingBreakdown = Enumerable.Range(1, 5).Reverse()
            .Select(star => p.Reviews.Count(r => r.Rating == star)).ToList()
    };
}
