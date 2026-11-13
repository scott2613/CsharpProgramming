/* Dọn danh mục BMTDStoreDB về 24 mẫu.
   Danh mục gồm ba mẫu đang có tham chiếu trong đơn hàng hoặc giỏ hàng.
   Giữ mã, giá, tồn kho và ảnh của mẫu đã có. Mẫu thiếu được thêm theo dữ liệu demo.
   Xóa sản phẩm ngoài danh sách và ảnh của chúng nếu không có đơn hàng hoặc giỏ tham chiếu.
   Nếu có tham chiếu, hiển thị các dòng cản trở rồi hoàn tác toàn bộ.
*/
USE [BMTDStoreDB];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- Giữ danh mục sản phẩm ổn định trong khi chọn các dòng cần giữ.
    DECLARE @ProductCountBefore BIGINT;
    SELECT @ProductCountBefore = COUNT_BIG(*) FROM dbo.Products WITH (TABLOCKX, HOLDLOCK);

    -- Tên danh mục được đối chiếu với dữ liệu hiện có để lấy CategoryId.
    DECLARE @CatalogCategories TABLE (
        Name NVARCHAR(100) COLLATE DATABASE_DEFAULT PRIMARY KEY,
        Description NVARCHAR(255) NOT NULL
    );
    INSERT @CatalogCategories (Name, Description) VALUES
        (N'PC', N'Máy tính để bàn cho văn phòng và chơi game'),
        (N'Mini PC', N'Máy tính để bàn kích thước nhỏ gọn'),
        (N'All-in-One', N'Máy tính tích hợp màn hình'),
        (N'Laptop', N'Máy tính xách tay cho học tập, làm việc và chơi game');

    INSERT dbo.Categories (Name, Description)
    SELECT s.Name, s.Description
    FROM @CatalogCategories AS s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Categories AS c WITH (UPDLOCK, HOLDLOCK)
        WHERE c.Name = s.Name
    );

    DECLARE @CatalogProducts TABLE (
        Name NVARCHAR(200) COLLATE DATABASE_DEFAULT PRIMARY KEY,
        CategoryName NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        StockQuantity INT NOT NULL,
        Specifications NVARCHAR(MAX) NOT NULL,
        LegacyName NVARCHAR(200) COLLATE DATABASE_DEFAULT NULL
    );
    INSERT @CatalogProducts (Name, CategoryName, Price, StockQuantity, Specifications, LegacyName) VALUES
        (N'ASUS ExpertCenter D5 Mini Tower D500ME', N'PC', 10990000, 12, N'Nhóm: PC văn phòng | Core i3-13100 | RAM 8 GB | SSD 256 GB', NULL),
        (N'Dell OptiPlex 7020 Tower', N'PC', 13990000, 8, N'Nhóm: PC văn phòng | Core i5-14500 | RAM 8 GB | SSD 512 GB', NULL),
        (N'HP Pro Tower 400 G9', N'PC', 15490000, 6, N'Nhóm: PC văn phòng | Core i5-14500 | RAM 16 GB | SSD 512 GB', NULL),
        (N'E-Power Office 43 AMD Ryzen 5 5500GT', N'PC', 9690000, 3, N'Nhóm: PC văn phòng | AMD Ryzen 5 5500GT', NULL),
        (N'E-Power E-Sport 05 AMD Ryzen 5 5600X', N'PC', 18790000, 2, N'Nhóm: PC gaming | AMD Ryzen 5 5600X | Card đồ họa rời', NULL),
        (N'Acer Predator Orion 3000 PO3-650', N'PC', 25990000, 3, N'Nhóm: PC gaming | Core i5-13400F | RTX 4060 | RAM 16 GB | SSD 512 GB', NULL),
        (N'Lenovo Legion Tower 5 26IRB8', N'PC', 31990000, 4, N'Nhóm: PC gaming | Core i7-13700F | RTX 4060 | RAM 16 GB | SSD 1 TB', NULL),
        (N'ASUS ROG Strix G16CH', N'PC', 39990000, 2, N'Nhóm: PC gaming | Core i7-13700F | RTX 4070 | RAM 32 GB | SSD 1 TB', NULL),
        (N'MSI Cubi N ADL', N'Mini PC', 6490000, 10, N'Nhóm: Mini PC | Intel N100 | RAM 8 GB | SSD 256 GB', NULL),
        (N'Lenovo ThinkCentre M70q Gen 5', N'Mini PC', 11990000, 7, N'Nhóm: Mini PC | Core i5-14400T | RAM 16 GB | SSD 512 GB', NULL),
        (N'ASUS NUC 14 Pro', N'Mini PC', 14490000, 5, N'Nhóm: Mini PC | Core Ultra 5 125H | RAM 16 GB | SSD 512 GB', NULL),
        (N'Apple Mac mini M4 (2024)', N'Mini PC', 15990000, 9, N'Nhóm: Mini PC | Apple M4 | Bộ nhớ 16 GB | SSD 256 GB', N'Apple Mac mini M4 2024 (10-CPU/10-GPU/16GB/256GB)'),
        (N'HP All-in-One 24-cr1024d', N'All-in-One', 16990000, 6, N'Nhóm: Máy tính All-in-One | RAM 8 GB | SSD 512 GB | Màn hình 23,8 inch', NULL),
        (N'SINGPC AIO S27-i713700 27 inch', N'All-in-One', 22490000, 5, N'Nhóm: Máy tính All-in-One | Core i7-13700 | RAM 16 GB | SSD 512 GB | Màn hình 27 inch', NULL),
        (N'ASUS ExpertCenter AIO P440VAK', N'All-in-One', 29490000, 3, N'Nhóm: Máy tính All-in-One | Core 5 210H | RAM 16 GB | SSD 512 GB | Màn hình 23,8 inch', NULL),
        (N'Apple iMac 24 inch M4 (2024)', N'All-in-One', 40990000, 2, N'Nhóm: Máy tính All-in-One | Apple M4 | Bộ nhớ 16 GB | SSD 256 GB | Màn hình Retina 4.5K', N'Apple iMac 24 inch M4 (8-CPU/8-GPU/16GB/256GB)'),
        (N'ASUS Vivobook 15 X1504VA', N'Laptop', 11490000, 12, N'Nhóm: Laptop học tập và văn phòng | Core i3-1315U | RAM 8 GB | SSD 512 GB | Màn hình 15,6 inch', NULL),
        (N'Dell Inspiron 15 3530', N'Laptop', 14490000, 8, N'Nhóm: Laptop học tập và văn phòng | Core i5-1334U | RAM 16 GB | SSD 512 GB | Màn hình 15,6 inch', NULL),
        (N'Lenovo IdeaPad Slim 3 15IRH8', N'Laptop', 15990000, 6, N'Nhóm: Laptop học tập và văn phòng | Core i5-13420H | RAM 16 GB | SSD 512 GB | Màn hình 15,6 inch', NULL),
        (N'Acer Aspire 5 A515-58M', N'Laptop', 18990000, 0, N'Nhóm: Laptop học tập và văn phòng | Core i7-1355U | RAM 16 GB | SSD 512 GB | Màn hình 15,6 inch', NULL),
        (N'Acer Nitro V 15 ANV15-51', N'Laptop', 18990000, 8, N'Nhóm: Laptop gaming | Core i5-13420H | RTX 4050 | RAM 16 GB | SSD 512 GB', NULL),
        (N'ASUS TUF Gaming A15 FA507NU', N'Laptop', 22990000, 5, N'Nhóm: Laptop gaming | Ryzen 7 7735HS | RTX 4050 | RAM 16 GB | SSD 512 GB', NULL),
        (N'Lenovo LOQ 15IRX9', N'Laptop', 26490000, 4, N'Nhóm: Laptop gaming | Core i5-13450HX | RTX 4060 | RAM 16 GB | SSD 512 GB', NULL),
        (N'MSI Katana 15 B13VFK-676VN', N'Laptop', 28990000, 0, N'Nhóm: Laptop gaming | Core i7-13620H | RTX 4060 | RAM 16 GB | SSD 1 TB', NULL);

    INSERT dbo.Products (CategoryId, Name, Specifications, Price, StockQuantity, ImageUrl)
    SELECT c.CategoryId, s.Name, s.Specifications, s.Price, s.StockQuantity, NULL
    FROM @CatalogProducts AS s
    CROSS APPLY (
        SELECT TOP (1) c.CategoryId
        FROM dbo.Categories AS c
        WHERE c.Name = s.CategoryName
        ORDER BY c.CategoryId
    ) AS c
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Products AS p WITH (UPDLOCK, HOLDLOCK)
        WHERE p.Name = s.Name OR p.Name = s.LegacyName
    );
    DECLARE @InsertedCatalogProducts INT = @@ROWCOUNT;

    DECLARE @KeptProducts TABLE (ProductId INT PRIMARY KEY);
    INSERT @KeptProducts (ProductId)
    SELECT picked.ProductId
    FROM @CatalogProducts AS s
    CROSS APPLY (
        SELECT TOP (1) p.ProductId
        FROM dbo.Products AS p
        WHERE p.Name = s.Name OR p.Name = s.LegacyName
        ORDER BY CASE
            WHEN EXISTS (SELECT 1 FROM dbo.OrderDetails AS d WITH (HOLDLOCK) WHERE d.ProductId = p.ProductId)
              OR EXISTS (SELECT 1 FROM dbo.ShoppingCartItems AS i WITH (HOLDLOCK) WHERE i.ProductId = p.ProductId)
            THEN 0 ELSE 1 END,
            CASE WHEN p.Name = s.Name THEN 0 ELSE 1 END,
            p.ProductId
    ) AS picked;

    IF (SELECT COUNT(*) FROM @KeptProducts) <> 24
        THROW 51400, N'Không xác định được đúng 24 mẫu. Đã dừng để bảo vệ dữ liệu.', 1;

    DECLARE @RemovedProducts TABLE (ProductId INT PRIMARY KEY);
    INSERT @RemovedProducts (ProductId)
    SELECT p.ProductId
    FROM dbo.Products AS p
    WHERE NOT EXISTS (SELECT 1 FROM @KeptProducts AS k WHERE k.ProductId = p.ProductId);

    DECLARE @BlockedProducts TABLE (
        ProductId INT PRIMARY KEY,
        OrderLines BIGINT NOT NULL,
        CartLines BIGINT NOT NULL
    );
    INSERT @BlockedProducts (ProductId, OrderLines, CartLines)
    SELECT x.ProductId, orders.LineCount, carts.LineCount
    FROM @RemovedProducts AS x
    CROSS APPLY (
        SELECT COUNT_BIG(*) AS LineCount FROM dbo.OrderDetails AS d WITH (HOLDLOCK)
        WHERE d.ProductId = x.ProductId
    ) AS orders
    CROSS APPLY (
        SELECT COUNT_BIG(*) AS LineCount FROM dbo.ShoppingCartItems AS i WITH (HOLDLOCK)
        WHERE i.ProductId = x.ProductId
    ) AS carts
    WHERE orders.LineCount > 0 OR carts.LineCount > 0;

    IF EXISTS (SELECT 1 FROM @BlockedProducts)
    BEGIN
        SELECT p.ProductId AS [Mã SP], p.Name AS [Tên máy],
               b.OrderLines AS [Dòng đơn hàng], b.CartLines AS [Mục giỏ hàng]
        FROM @BlockedProducts AS b
        INNER JOIN dbo.Products AS p ON p.ProductId = b.ProductId
        ORDER BY p.ProductId;

        THROW 51401, N'Có sản phẩm ngoài 24 mẫu đang được đơn hàng hoặc giỏ hàng sử dụng. Đã hoàn tác toàn bộ.', 1;
    END;

    DELETE image
    FROM dbo.ProductImages AS image
    INNER JOIN @RemovedProducts AS x ON x.ProductId = image.ProductId;

    DELETE p
    FROM dbo.Products AS p
    INNER JOIN @RemovedProducts AS x ON x.ProductId = p.ProductId;
    DECLARE @DeletedCatalogProducts INT = @@ROWCOUNT;

    IF (SELECT COUNT_BIG(*) FROM dbo.Products) <> 24
        THROW 51402, N'Kết quả không đúng 24 sản phẩm. Đã dừng để bảo vệ dữ liệu.', 1;

    COMMIT TRANSACTION;

    SELECT @ProductCountBefore AS [Trước khi dọn], @InsertedCatalogProducts AS [Đã thêm],
           @DeletedCatalogProducts AS [Đã xóa], 24 AS [Sản phẩm còn lại];
    SELECT p.ProductId AS [Mã SP], p.Name AS [Tên máy], p.Price AS [Đơn giá],
           p.StockQuantity AS [Tồn kho], c.Name AS [Danh mục], p.Specifications AS [Cấu hình],
           p.ImageUrl AS [URL ảnh]
    FROM dbo.Products AS p
    INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
    ORDER BY c.Name, p.Name;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
