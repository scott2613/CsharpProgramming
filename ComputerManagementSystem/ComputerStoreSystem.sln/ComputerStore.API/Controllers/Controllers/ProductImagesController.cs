using System.Security.Cryptography;
using ComputerStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ComputerStore.API.Controllers;

[AllowAnonymous]
// Tuyến này phục vụ ảnh trong bảng ProductImages qua đường dẫn /images/catalog/{id}.jpg.
[Route("images/catalog")]
public sealed class ProductImagesController(ApplicationDbContext db) : ControllerBase
{
    // Mã SHA-256 để nhận diện ảnh demo cũ và chuyển sang ảnh giữ chỗ.
    private static readonly byte[] DemoImageHash = Convert.FromHexString("c23dfd7b01b85b9817cc65ec92ab8dcad79300acf84764c593a62ca0595f4f72");

    [HttpGet("{id:int:min(1)}.jpg")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        byte[]? jpeg;
        try
        {
            // Đọc cột JpegBytes theo mã sản phẩm. EF Core tham số hóa mã trong chuỗi nội suy.
            jpeg = await db.Database.SqlQuery<byte[]>(
                $"SELECT [JpegBytes] AS [Value] FROM [dbo].[ProductImages] WHERE [ProductId] = {id}")
                .FirstOrDefaultAsync(cancellationToken);
        }
        // Dùng ảnh giữ chỗ nếu cơ sở dữ liệu cũ chưa có bảng ảnh.
        catch (SqlException ex) when (ex.Number == 208)
        {
            return Redirect(Url.Content("~/images/product-placeholder.svg"));
        }
        // Loại dữ liệu thiếu, dữ liệu không có dấu mở đầu JPEG hoặc ảnh demo đã biết.
        if (jpeg is null || jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8 ||
            SHA256.HashData(jpeg).AsSpan().SequenceEqual(DemoImageHash))
            return Redirect(Url.Content("~/images/product-placeholder.svg"));
        // Gửi dữ liệu ảnh với kiểu nội dung image/jpeg.
        return File(jpeg, "image/jpeg");
    }
}
