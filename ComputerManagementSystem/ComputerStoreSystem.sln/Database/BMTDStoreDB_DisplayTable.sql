-- FILE XEM DU LIEU BMTDStoreDB
-- Chi doc du lieu; khong them, sua, xoa hay nap lai du lieu.
-- Mo file trong SSMS, nhan Ctrl+A roi F5 de xem tat ca ket qua.

USE [BMTDStoreDB];
GO
SET NOCOUNT ON;
GO

-- 1. PHAN QUYEN
SELECT
    RoleId AS [Mã quyền],
    RoleName AS [Tên quyền]
FROM dbo.Roles
ORDER BY RoleId;

-- 2. TAI KHOAN VA QUYEN
SELECT
    u.UserId AS [Mã tài khoản],
    u.Username AS [Tên đăng nhập],
    u.FullName AS [Họ và tên],
    u.Email,
    u.PhoneNumber AS [Số điện thoại],
    u.RoleId AS [Mã quyền],
    r.RoleName AS [Tên quyền]
FROM dbo.Users AS u
LEFT JOIN dbo.Roles AS r ON r.RoleId = u.RoleId
ORDER BY u.UserId;

-- 3. DANH MUC VA SO SAN PHAM
SELECT
    c.CategoryId AS [Mã danh mục],
    c.Name AS [Tên danh mục],
    c.Description AS [Mô tả danh mục],
    COUNT(p.ProductId) AS [Số sản phẩm]
FROM dbo.Categories AS c
LEFT JOIN dbo.Products AS p ON p.CategoryId = c.CategoryId
GROUP BY c.CategoryId, c.Name, c.Description
ORDER BY c.CategoryId;

-- 4. TAT CA SAN PHAM, BAO GOM THONG TIN CHI TIET VA LINK ANH
-- Gia giu kieu so de de sap xep, tinh toan, xuat Excel.
SELECT
    p.ProductId AS [Mã sản phẩm],
    p.Name AS [Tên sản phẩm],
    c.Name AS [Danh mục],
    p.Price AS [Giá bán (VND)],
    p.StockQuantity AS [Số lượng tồn],
    CASE WHEN p.StockQuantity > 0 THEN N'Còn hàng' ELSE N'Hết hàng' END AS [Tình trạng],
    p.Specifications AS [Mô tả và thông số kỹ thuật],
    p.ImageUrl AS [Đường dẫn ảnh]
FROM dbo.Products AS p
LEFT JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
ORDER BY p.CategoryId, p.ProductId;

-- 5. RIENG LAPTOP
SELECT
    p.ProductId AS [Mã sản phẩm],
    p.Name AS [Tên laptop],
    p.Price AS [Giá bán (VND)],
    p.StockQuantity AS [Số lượng tồn],
    p.Specifications AS [Mô tả và thông số kỹ thuật]
FROM dbo.Products AS p
INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
WHERE c.Name = N'Laptop'
ORDER BY p.ProductId;

-- 6. RIENG MAY TINH DE BAN
SELECT
    p.ProductId AS [Mã sản phẩm],
    p.Name AS [Tên PC],
    p.Price AS [Giá bán (VND)],
    p.StockQuantity AS [Số lượng tồn],
    p.Specifications AS [Mô tả và thông số kỹ thuật]
FROM dbo.Products AS p
INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
WHERE c.Name = N'PC'
ORDER BY p.ProductId;

-- 7. DON HANG
-- Sau khi nap lai du lieu demo, bang nay trong cho den khi dat don moi.
SELECT
    OrderId AS [Mã đơn hàng],
    OrderDate AS [Ngày đặt (UTC)],
    CustomerName AS [Người nhận],
    CustomerPhone AS [Số điện thoại],
    DeliveryAddress AS [Địa chỉ giao hàng],
    TotalAmount AS [Tổng tiền (VND)],
    Status AS [Trạng thái]
FROM dbo.Orders
ORDER BY OrderDate DESC, OrderId DESC;

-- 8. CHI TIET DON HANG
SELECT
    od.OrderDetailId AS [Mã chi tiết],
    od.OrderId AS [Mã đơn hàng],
    od.ProductId AS [Mã sản phẩm],
    p.Name AS [Tên sản phẩm hiện tại],
    od.Quantity AS [Số lượng],
    od.UnitPrice AS [Đơn giá lúc đặt (VND)],
    CAST(od.Quantity * od.UnitPrice AS DECIMAL(28,2)) AS [Thành tiền (VND)]
FROM dbo.OrderDetails AS od
LEFT JOIN dbo.Products AS p ON p.ProductId = od.ProductId
ORDER BY od.OrderId DESC, od.OrderDetailId;

-- 9. TONG SO DONG CUA SAU BANG
SELECT N'Roles' AS [Tên bảng], COUNT_BIG(*) AS [Số dòng] FROM dbo.Roles
UNION ALL
SELECT N'Users', COUNT_BIG(*) FROM dbo.Users
UNION ALL
SELECT N'Categories', COUNT_BIG(*) FROM dbo.Categories
UNION ALL
SELECT N'Products', COUNT_BIG(*) FROM dbo.Products
UNION ALL
SELECT N'Orders', COUNT_BIG(*) FROM dbo.Orders
UNION ALL
SELECT N'OrderDetails', COUNT_BIG(*) FROM dbo.OrderDetails;

-- 10. TONG QUAN KHO: SO MA SAN PHAM VA TONG SO MAY TON LA HAI CHI SO KHAC NHAU
SELECT
    COUNT_BIG(*) AS [Số mã sản phẩm],
    COALESCE(SUM(CAST(StockQuantity AS BIGINT)), 0) AS [Tổng số máy tồn],
    COALESCE(SUM(CASE WHEN StockQuantity > 0 THEN CAST(1 AS BIGINT) ELSE 0 END), 0) AS [Số mã còn hàng],
    COALESCE(SUM(CASE WHEN StockQuantity = 0 THEN CAST(1 AS BIGINT) ELSE 0 END), 0) AS [Số mã hết hàng]
FROM dbo.Products;

-- XEM MOT SAN PHAM: bo dau -- o hai dong duoi va doi ma neu can.
-- SELECT ProductId, Name, Price, StockQuantity, Specifications, ImageUrl
-- FROM dbo.Products WHERE ProductId = 1;

-- GIỎ HÀNG LƯU THEO TÀI KHOẢN (chỉ đọc)
SELECT c.UserId,u.Username,c.ProductId,p.Name,c.Quantity FROM dbo.ShoppingCartItems c
JOIN dbo.Users u ON u.UserId=c.UserId JOIN dbo.Products p ON p.ProductId=c.ProductId ORDER BY c.UserId,c.ProductId;
