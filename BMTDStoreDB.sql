/* BMTDStoreDB - NÂNG CẤP KHÔNG XÓA DỮ LIỆU
   Dừng API/Admin và sao lưu trước khi nâng cấp schema.
   Giữ Users, mật khẩu, Products, ảnh, Orders, OrderDetails và giỏ đã có.
   Chỉ tạo 30 sản phẩm demo khi catalog, đơn hàng và giỏ đều trống.
   Giá/số lượng demo là dữ liệu tham khảo, không phải giá/tồn kho trực tiếp.
   Không chạy lẫn Update-Database với bộ SQL thủ công này; xem README.
*/
USE [master];
GO
IF DB_ID(N'BMTDStoreDB') IS NULL EXEC(N'CREATE DATABASE [BMTDStoreDB]');
GO
USE [BMTDStoreDB];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
        CREATE TABLE dbo.Roles (
            RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
            RoleName NVARCHAR(50) NOT NULL CONSTRAINT UQ_Roles_RoleName UNIQUE
        );

    IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
        CREATE TABLE dbo.Users (
            UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
            Username NVARCHAR(50) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
            PasswordHash NVARCHAR(255) NOT NULL,
            FullName NVARCHAR(100) NOT NULL,
            Email NVARCHAR(100) NULL,
            PhoneNumber NVARCHAR(20) NULL,
            Address NVARCHAR(255) NOT NULL CONSTRAINT DF_Users_Address DEFAULT(N''),
            RoleId INT NOT NULL,
            CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(RoleId)
        );

    IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
        CREATE TABLE dbo.Categories (
            CategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
            Name NVARCHAR(100) NOT NULL,
            Description NVARCHAR(255) NULL
        );

    IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
        CREATE TABLE dbo.Products (
            ProductId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
            CategoryId INT NOT NULL,
            Name NVARCHAR(200) NOT NULL,
            Specifications NVARCHAR(MAX) NULL,
            Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_Price CHECK(Price >= 0),
            StockQuantity INT NOT NULL CONSTRAINT DF_Products_Stock DEFAULT(5)
                CONSTRAINT CK_Products_Stock CHECK(StockQuantity >= 0),
            ImageUrl NVARCHAR(500) NULL,
            CONSTRAINT FK_Products_Categories FOREIGN KEY(CategoryId) REFERENCES dbo.Categories(CategoryId)
        );

    IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
        CREATE TABLE dbo.Orders (
            OrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
            OrderDate DATETIME2 NOT NULL CONSTRAINT DF_Orders_Date DEFAULT(SYSUTCDATETIME()),
            CustomerName NVARCHAR(100) NOT NULL,
            CustomerPhone NVARCHAR(20) NOT NULL,
            DeliveryAddress NVARCHAR(500) NOT NULL,
            UserId INT NULL,
            TotalAmount DECIMAL(18,2) NOT NULL,
            Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT(N'Chờ xử lý')
        );

    IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NULL
        CREATE TABLE dbo.OrderDetails (
            OrderDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY,
            OrderId INT NOT NULL,
            ProductId INT NOT NULL,
            Quantity INT NOT NULL CONSTRAINT CK_OrderDetails_Quantity CHECK(Quantity > 0),
            UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_OrderDetails_Price CHECK(UnitPrice >= 0),
            CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY(OrderId)
                REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
            CONSTRAINT FK_OrderDetails_Products FOREIGN KEY(ProductId)
                REFERENCES dbo.Products(ProductId)
        );


-- Khoa cac bang quyen/tai khoan trong luc chuyen doi.
    DECLARE @UsersBefore BIGINT, @AdminsBefore BIGINT, @RoleRows BIGINT;
    SELECT @RoleRows=COUNT_BIG(*) FROM dbo.Roles WITH(TABLOCKX,HOLDLOCK);
    SELECT @UsersBefore=COUNT_BIG(*) FROM dbo.Users WITH(TABLOCKX,HOLDLOCK);
    SELECT @AdminsBefore=COUNT_BIG(*) FROM dbo.Users WHERE RoleId=1;

    IF EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleId NOT IN(1,2,3))
        OR EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleId=1 AND RoleName<>N'Admin')
        OR EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleId=2 AND RoleName NOT IN(N'Staff',N'Customer'))
        OR EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleId=3 AND RoleName<>N'Customer')
        THROW 51200,N'Sơ đồ quyền khác dữ liệu đã biết. Dừng để kiểm tra, không tự gán quyền.',1;

    -- Khong xoa role neu con bang tham chieu khac ngoai Users.RoleId.
    IF EXISTS(SELECT 1 FROM sys.foreign_key_columns f
        INNER JOIN sys.columns c ON c.object_id=f.parent_object_id AND c.column_id=f.parent_column_id
        WHERE f.referenced_object_id=OBJECT_ID(N'dbo.Roles') AND
             (f.parent_object_id<>OBJECT_ID(N'dbo.Users') OR c.name<>N'RoleId'))
        THROW 51201,N'Có bảng khác tham chiếu Roles. Cần kiểm tra trước khi chuyển mã quyền.',1;

    -- Doi ten tam truoc de khong vi pham UNIQUE(RoleName).
    UPDATE dbo.Roles SET RoleName=N'Customer_Legacy_3' WHERE RoleId=3;
    SET IDENTITY_INSERT dbo.Roles ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleId=1)
        INSERT dbo.Roles(RoleId,RoleName) VALUES(1,N'Admin');
    IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleId=2)
        INSERT dbo.Roles(RoleId,RoleName) VALUES(2,N'Customer');
    SET IDENTITY_INSERT dbo.Roles OFF;
    UPDATE dbo.Roles SET RoleName=N'Admin' WHERE RoleId=1;
    UPDATE dbo.Roles SET RoleName=N'Customer' WHERE RoleId=2;

    -- Chuyen Users truoc khi xoa Roles=3, tranh ON DELETE CASCADE xoa tai khoan.
    UPDATE dbo.Users SET RoleId=2 WHERE RoleId=3;
    IF EXISTS(SELECT 1 FROM dbo.Users WHERE RoleId NOT IN(1,2))
        THROW 51202,N'Còn tài khoản có quyền chưa được nhận diện. Đã dừng chuyển đổi.',1;
    DELETE FROM dbo.Roles WHERE RoleId=3;

    IF (SELECT COUNT_BIG(*) FROM dbo.Users)<>@UsersBefore
        OR (SELECT COUNT_BIG(*) FROM dbo.Users WHERE RoleId=1)<>@AdminsBefore
        OR (SELECT COUNT_BIG(*) FROM dbo.Roles)<>2
        THROW 51203,N'Kiểm tra số tài khoản/quyền không khớp. Hoàn tác chuyển đổi.',1;

    -- Tài khoản cũ thiếu cột xác thực cần được cấp lại mật khẩu.
    IF COL_LENGTH(N'dbo.Users', N'PasswordHash') IS NULL
    BEGIN
        ALTER TABLE dbo.Users ADD PasswordHash NVARCHAR(255) NOT NULL
            CONSTRAINT DF_Users_PasswordHash_Upgrade DEFAULT (N'');
        ALTER TABLE dbo.Users DROP CONSTRAINT DF_Users_PasswordHash_Upgrade;
    END;

    IF COL_LENGTH(N'dbo.Users', N'Address') IS NULL
        ALTER TABLE dbo.Users ADD Address NVARCHAR(255) NOT NULL CONSTRAINT DF_Users_Address_Upgrade DEFAULT(N'');
    IF COL_LENGTH(N'dbo.Orders', N'UserId') IS NULL
        ALTER TABLE dbo.Orders ADD UserId INT NULL;
    -- Không tự gán chủ đơn lịch sử bằng tên/điện thoại.
    EXEC(N'IF EXISTS(SELECT 1 FROM dbo.Orders o LEFT JOIN dbo.Users u ON u.UserId=o.UserId WHERE o.UserId IS NOT NULL AND u.UserId IS NULL)
        THROW 51301,N''Đơn hàng có UserId không tồn tại. Dừng để kiểm tra.'',1;');
    UPDATE dbo.Users SET Email=NULL WHERE Email IS NOT NULL AND LTRIM(RTRIM(Email))=N'';
    UPDATE dbo.Users SET PhoneNumber=NULL WHERE PhoneNumber IS NOT NULL AND LTRIM(RTRIM(PhoneNumber))=N'';
    IF EXISTS (
        SELECT LOWER(LTRIM(RTRIM(k.Value))) FROM dbo.Users u
        CROSS APPLY (VALUES(u.Username),(u.Email),(u.PhoneNumber)) k(Value)
        WHERE NULLIF(LTRIM(RTRIM(k.Value)),N'') IS NOT NULL
        GROUP BY LOWER(LTRIM(RTRIM(k.Value))) HAVING COUNT(DISTINCT u.UserId)>1)
        THROW 51302,N'Định danh đăng nhập/email/SĐT trùng giữa các tài khoản. Không tự sửa; kiểm tra dữ liệu.',1;
    EXEC(N'IF EXISTS(SELECT 1 FROM dbo.Users WHERE DATALENGTH([Username])>100) THROW 51303,N''Dữ liệu Users.Username quá dài; không tự cắt nội dung.'',1; IF COL_LENGTH(N''dbo.Users'',N''Username'')<>100 ALTER TABLE dbo.Users ALTER COLUMN [Username] NVARCHAR(50) NOT NULL;');
    EXEC(N'IF EXISTS(SELECT 1 FROM dbo.Users WHERE DATALENGTH([FullName])>200) THROW 51303,N''Dữ liệu Users.FullName quá dài; không tự cắt nội dung.'',1; IF COL_LENGTH(N''dbo.Users'',N''FullName'')<>200 ALTER TABLE dbo.Users ALTER COLUMN [FullName] NVARCHAR(100) NOT NULL;');
    EXEC(N'IF EXISTS(SELECT 1 FROM dbo.Users WHERE DATALENGTH([PasswordHash])>510) THROW 51303,N''Dữ liệu Users.PasswordHash quá dài; không tự cắt nội dung.'',1; IF COL_LENGTH(N''dbo.Users'',N''PasswordHash'')<>510 ALTER TABLE dbo.Users ALTER COLUMN [PasswordHash] NVARCHAR(255) NOT NULL;');
    EXEC(N'IF EXISTS(SELECT 1 FROM dbo.Users WHERE DATALENGTH([Email])>200) THROW 51303,N''Dữ liệu Users.Email quá dài; không tự cắt nội dung.'',1; IF COL_LENGTH(N''dbo.Users'',N''Email'')<>200 ALTER TABLE dbo.Users ALTER COLUMN [Email] NVARCHAR(100) NULL;');
    EXEC(N'IF EXISTS(SELECT 1 FROM dbo.Users WHERE DATALENGTH([PhoneNumber])>40) THROW 51303,N''Dữ liệu Users.PhoneNumber quá dài; không tự cắt nội dung.'',1; IF COL_LENGTH(N''dbo.Users'',N''PhoneNumber'')<>40 ALTER TABLE dbo.Users ALTER COLUMN [PhoneNumber] NVARCHAR(20) NULL;');
    EXEC(N'IF EXISTS(SELECT 1 FROM dbo.Users WHERE DATALENGTH([Address])>510) THROW 51303,N''Dữ liệu Users.Address quá dài; không tự cắt nội dung.'',1; IF COL_LENGTH(N''dbo.Users'',N''Address'')<>510 ALTER TABLE dbo.Users ALTER COLUMN [Address] NVARCHAR(255) NOT NULL;');
    EXEC(N'IF EXISTS(SELECT 1 FROM dbo.Orders WHERE DATALENGTH([DeliveryAddress])>1000) THROW 51303,N''Dữ liệu Orders.DeliveryAddress quá dài; không tự cắt nội dung.'',1; IF COL_LENGTH(N''dbo.Orders'',N''DeliveryAddress'')<>1000 ALTER TABLE dbo.Orders ALTER COLUMN [DeliveryAddress] NVARCHAR(500) NOT NULL;');

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Users') AND name=N'IX_Users_Username')
        CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Users') AND name=N'IX_Users_Email')
        CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email) WHERE Email IS NOT NULL;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Users') AND name=N'IX_Users_PhoneNumber')
        CREATE UNIQUE INDEX IX_Users_PhoneNumber ON dbo.Users(PhoneNumber) WHERE PhoneNumber IS NOT NULL;
    EXEC(N'IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N''dbo.Orders'') AND name=N''IX_Orders_UserId'')
        CREATE INDEX IX_Orders_UserId ON dbo.Orders(UserId);');

    IF OBJECT_ID(N'dbo.ShoppingCartItems',N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ShoppingCartItems (
            UserId INT NOT NULL, ProductId INT NOT NULL, Quantity INT NOT NULL,
            CONSTRAINT PK_ShoppingCartItems PRIMARY KEY(UserId,ProductId),
            CONSTRAINT CK_ShoppingCartItems_Quantity CHECK(Quantity BETWEEN 1 AND 999),
            CONSTRAINT FK_ShoppingCartItems_Users_UserId FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
            CONSTRAINT FK_ShoppingCartItems_Products_ProductId FOREIGN KEY(ProductId) REFERENCES dbo.Products(ProductId) ON DELETE CASCADE
        );
        CREATE INDEX IX_ShoppingCartItems_ProductId ON dbo.ShoppingCartItems(ProductId);
    END
    ELSE IF (SELECT COUNT(*) FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.ShoppingCartItems')
        AND name IN(N'UserId',N'ProductId',N'Quantity') AND system_type_id=56 AND is_nullable=0)<>3
        THROW 51304,N'Bảng ShoppingCartItems khác cấu trúc đã biết. Không ghi đè.',1;

    DECLARE @drop_Users NVARCHAR(MAX)=N'';
    SELECT @drop_Users=@drop_Users+N'ALTER TABLE dbo.Users DROP CONSTRAINT '+QUOTENAME(f.name)+N';'
    FROM sys.foreign_keys f JOIN sys.foreign_key_columns k ON k.constraint_object_id=f.object_id
    JOIN sys.columns c ON c.object_id=k.parent_object_id AND c.column_id=k.parent_column_id
    WHERE f.parent_object_id=OBJECT_ID(N'dbo.Users') AND c.name=N'RoleId' AND f.referenced_object_id=OBJECT_ID(N'dbo.Roles');
    EXEC sys.sp_executesql @drop_Users;
    EXEC(N'ALTER TABLE dbo.Users WITH CHECK ADD CONSTRAINT FK_Users_Roles_RoleId FOREIGN KEY(RoleId) REFERENCES dbo.Roles(RoleId);');

    DECLARE @drop_Products NVARCHAR(MAX)=N'';
    SELECT @drop_Products=@drop_Products+N'ALTER TABLE dbo.Products DROP CONSTRAINT '+QUOTENAME(f.name)+N';'
    FROM sys.foreign_keys f JOIN sys.foreign_key_columns k ON k.constraint_object_id=f.object_id
    JOIN sys.columns c ON c.object_id=k.parent_object_id AND c.column_id=k.parent_column_id
    WHERE f.parent_object_id=OBJECT_ID(N'dbo.Products') AND c.name=N'CategoryId' AND f.referenced_object_id=OBJECT_ID(N'dbo.Categories');
    EXEC sys.sp_executesql @drop_Products;
    EXEC(N'ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT FK_Products_Categories_CategoryId FOREIGN KEY(CategoryId) REFERENCES dbo.Categories(CategoryId);');

    DECLARE @drop_OrderDetails NVARCHAR(MAX)=N'';
    SELECT @drop_OrderDetails=@drop_OrderDetails+N'ALTER TABLE dbo.OrderDetails DROP CONSTRAINT '+QUOTENAME(f.name)+N';'
    FROM sys.foreign_keys f JOIN sys.foreign_key_columns k ON k.constraint_object_id=f.object_id
    JOIN sys.columns c ON c.object_id=k.parent_object_id AND c.column_id=k.parent_column_id
    WHERE f.parent_object_id=OBJECT_ID(N'dbo.OrderDetails') AND c.name=N'ProductId' AND f.referenced_object_id=OBJECT_ID(N'dbo.Products');
    EXEC sys.sp_executesql @drop_OrderDetails;
    EXEC(N'ALTER TABLE dbo.OrderDetails WITH CHECK ADD CONSTRAINT FK_OrderDetails_Products_ProductId FOREIGN KEY(ProductId) REFERENCES dbo.Products(ProductId);');

    DECLARE @drop_Orders NVARCHAR(MAX)=N'';
    SELECT @drop_Orders=@drop_Orders+N'ALTER TABLE dbo.Orders DROP CONSTRAINT '+QUOTENAME(f.name)+N';'
    FROM sys.foreign_keys f JOIN sys.foreign_key_columns k ON k.constraint_object_id=f.object_id
    JOIN sys.columns c ON c.object_id=k.parent_object_id AND c.column_id=k.parent_column_id
    WHERE f.parent_object_id=OBJECT_ID(N'dbo.Orders') AND c.name=N'UserId' AND f.referenced_object_id=OBJECT_ID(N'dbo.Users');
    EXEC sys.sp_executesql @drop_Orders;
    EXEC(N'ALTER TABLE dbo.Orders WITH CHECK ADD CONSTRAINT FK_Orders_Users_UserId FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId);');
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Products') AND name=N'CK_Products_Stock_Nonnegative') ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT CK_Products_Stock_Nonnegative CHECK(StockQuantity >= 0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Products') AND name=N'CK_Products_Price_Nonnegative') ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT CK_Products_Price_Nonnegative CHECK(Price >= 0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.OrderDetails') AND name=N'CK_OrderDetails_PositiveQuantity') ALTER TABLE dbo.OrderDetails WITH CHECK ADD CONSTRAINT CK_OrderDetails_PositiveQuantity CHECK(Quantity > 0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.OrderDetails') AND name=N'CK_OrderDetails_UnitPrice_Nonnegative') ALTER TABLE dbo.OrderDetails WITH CHECK ADD CONSTRAINT CK_OrderDetails_UnitPrice_Nonnegative CHECK(UnitPrice >= 0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Orders') AND name=N'CK_Orders_Total_Nonnegative') ALTER TABLE dbo.Orders WITH CHECK ADD CONSTRAINT CK_Orders_Total_Nonnegative CHECK(TotalAmount >= 0);

    IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductImages (
            ProductId INT NOT NULL CONSTRAINT PK_ProductImages PRIMARY KEY,
            JpegBytes VARBINARY(MAX) NOT NULL,
            CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductId)
                REFERENCES dbo.Products(ProductId) ON DELETE CASCADE
        );
    END

    IF NOT EXISTS(SELECT 1 FROM dbo.Products)
        AND NOT EXISTS(SELECT 1 FROM dbo.Orders)
        AND NOT EXISTS(SELECT 1 FROM dbo.ShoppingCartItems)
    BEGIN
        IF EXISTS (
            SELECT 1 FROM dbo.Categories
            WHERE (CategoryId=1 AND Name<>N'PC')
               OR (CategoryId=2 AND Name<>N'Mini PC')
               OR (CategoryId=3 AND Name<>N'All-in-One')
        )
            PRINT N'Không nạp mẫu vì mã danh mục hiện có khác bộ mẫu.';
        ELSE
        BEGIN
            SET IDENTITY_INSERT dbo.Categories ON;

            IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = 1)
                INSERT INTO dbo.Categories (CategoryId, Name, Description) VALUES (1, N'PC', N'Máy tính để bàn dạng thùng (Tower)');
            IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = 2)
                INSERT INTO dbo.Categories (CategoryId, Name, Description) VALUES (2, N'Mini PC', N'Máy tính để bàn kích thước siêu nhỏ gọn');
            IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = 3)
                INSERT INTO dbo.Categories (CategoryId, Name, Description) VALUES (3, N'All-in-One', N'Máy tính tất cả trong một (Tích hợp màn hình)');

            SET IDENTITY_INSERT dbo.Categories OFF;

            -- Nạp 30 sản phẩm mẫu
            SET IDENTITY_INSERT dbo.Products ON;

            MERGE INTO dbo.Products AS Target
            USING (VALUES 
                -- Apple
                (1, 2, N'Apple Mac mini M4 2024 (10-CPU/10-GPU/16GB/256GB)', N'Thương hiệu: Apple | Loại: Mini PC | Nhu cầu: Học tập - văn phòng | CPU: Apple M4 | RAM: 16GB | SSD: 256GB', 14990000, 10, NULL),
                (2, 2, N'Apple Mac mini M4 Pro 2024 (12-CPU/16-GPU/24GB/512GB)', N'Thương hiệu: Apple | Loại: Mini PC | Nhu cầu: Đồ họa, Đa nhiệm | CPU: Apple M4 Pro | RAM: 24GB | SSD: 512GB', 34990000, 5, NULL),
                (3, 3, N'Apple iMac 24 inch M4 (8-CPU/8-GPU/16GB/256GB)', N'Thương hiệu: Apple | Loại: All-in-One | Nhu cầu: Đồ họa, Văn phòng | CPU: Apple M4 | RAM: 16GB | SSD: 256GB | Màn hình: 24" 4.5K', 34990000, 8, NULL),
                (4, 2, N'Apple Studio Desktop M6 NextGen Concept', N'Thương hiệu: Apple | Loại: Mini PC / Workstation | Nhu cầu: Kỹ thuật, Đồ họa nặng | CPU: Apple M6 | RAM: 64GB | SSD: 1TB', 69990000, 3, NULL),

                -- ASUS
                (5, 1, N'ASUS ROG Strix G16 CHR-i713700', N'Thương hiệu: ASUS | Loại: PC | Nhu cầu: Gaming | CPU: Intel Core i7 | RAM: 16GB | SSD: 512GB | VGA: RTX 4060 8GB', 28990000, 7, NULL),
                (6, 2, N'ASUS NUC 14 Pro Tall Core i5-1340P', N'Thương hiệu: ASUS | Loại: Mini PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 8GB | SSD: 256GB', 9890000, 15, NULL),
                (7, 3, N'ASUS AIO A5402WV-BA021W 23.8 inch', N'Thương hiệu: ASUS | Loại: All-in-One | Nhu cầu: Học tập - văn phòng | CPU: Intel Core 5 | RAM: 16GB | SSD: 512GB', 17490000, 6, NULL),
                (8, 1, N'ASUS ExpertCenter D5 D500SDR Core i5', N'Thương hiệu: ASUS | Loại: PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 8GB | SSD: 512GB', 12490000, 12, NULL),
                (9, 3, N'ASUS AIO A3402WBA Core i7-1255U', N'Thương hiệu: ASUS | Loại: All-in-One | Nhu cầu: Đồ họa, Kỹ thuật | CPU: Intel Core i7 | RAM: 16GB | SSD: 512GB', 21990000, 4, NULL),

                -- HP
                (10, 1, N'HP Slim Desktop S01-pF2023d Core i3', N'Thương hiệu: HP | Loại: PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i3 | RAM: 8GB | SSD: 256GB', 8990000, 20, NULL),
                (11, 1, N'HP Pro Tower 400 G9 Core i5-13500', N'Thương hiệu: HP | Loại: PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 16GB | SSD: 512GB', 14590000, 10, NULL),
                (12, 3, N'HP AIO 24-cr0002d Core i5-1335U', N'Thương hiệu: HP | Loại: All-in-One | Nhu cầu: Học tập - văn phòng | CPU: Intel Core 5 | RAM: 16GB | SSD: 512GB', 18990000, 5, NULL),
                (13, 2, N'HP Elite Mini 800 G9 Core i7-13700', N'Thương hiệu: HP | Loại: Mini PC | Nhu cầu: Kỹ thuật, Đa nhiệm | CPU: Intel Core i7 | RAM: 16GB | SSD: 512GB', 23490000, 6, NULL),
                (14, 3, N'HP AIO 27-cr0010d Core i7-1355U', N'Thương hiệu: HP | Loại: All-in-One | Nhu cầu: Đồ họa, Kỹ thuật | CPU: Intel Core 7 | RAM: 16GB | SSD: 1TB', 26990000, 3, NULL),

                -- MSI (Đã sửa lỗi gạch đỏ ở dòng 17 bên dưới)
                (15, 1, N'MSI MAG Infinite S3 Core i5-14400F', N'Thương hiệu: MSI | Loại: PC | Nhu cầu: Gaming | CPU: Intel Core i5 | RAM: 16GB | SSD: 512GB | VGA: RTX 3050', 19990000, 8, NULL),
                (16, 1, N'MSI Gaming Aegis RS Core i7-14700KF', N'Thương hiệu: MSI | Loại: PC | Nhu cầu: Gaming, Kỹ thuật | CPU: Intel Core i7 | RAM: 32GB | SSD: 1TB | VGA: RTX 4070', 45990000, 4, NULL),
                (17, 2, N'MSI Cubi 5 12M Core i5-1235U', N'Thương hiệu: MSI | Loại: Mini PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 8GB | SSD: 256GB', 11290000, 14, NULL),
                (18, 2, N'MSI Cubi N ADL N200 Mini PC', N'Thương hiệu: MSI | Loại: Mini PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core N200 | RAM: 8GB | SSD: 128GB', 6490000, 18, NULL),
                (19, 3, N'MSI Modern AM242P 12M Core i7-1260P', N'Thương hiệu: MSI | Loại: All-in-One | Nhu cầu: Đồ họa, Văn phòng | CPU: Intel Core i7 | RAM: 16GB | SSD: 512GB', 20490000, 5, NULL),

                -- ROSA
                (20, 1, N'ROSA Office Pro R301 Core i3-12100', N'Thương hiệu: ROSA | Loại: PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i3 | RAM: 8GB | SSD: 256GB', 7290000, 25, NULL),
                (21, 1, N'ROSA Office Pro R501 Core i5-12400', N'Thương hiệu: ROSA | Loại: PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 16GB | SSD: 512GB', 11490000, 15, NULL),
                (22, 1, N'ROSA TechCAD R701 Core i7-13700', N'Thương hiệu: ROSA | Loại: PC | Nhu cầu: Kỹ thuật, Đồ họa | CPU: Intel Core i7 | RAM: 16GB | SSD: 512GB | VGA: GTX 1650', 18990000, 7, NULL),
                (23, 3, N'ROSA All-In-One R24-i5 Core i5-1135G7', N'Thương hiệu: ROSA | Loại: All-in-One | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 8GB | SSD: 256GB', 12990000, 9, NULL),
                (24, 2, N'ROSA NUC Mini Box R100 Core i5', N'Thương hiệu: ROSA | Loại: Mini PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 8GB | SSD: 256GB', 8790000, 11, NULL),

                -- SINGPC
                (25, 1, N'SINGPC Office M600 Core i3-10105', N'Thương hiệu: SINGPC | Loại: PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i3 | RAM: 8GB | SSD: 256GB', 6890000, 30, NULL),
                (26, 1, N'SINGPC Professional M800 Core i5-12400', N'Thương hiệu: SINGPC | Loại: PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 16GB | SSD: 512GB', 11990000, 16, NULL),
                (27, 3, N'SINGPC AIO S22-i310105 21.5 inch', N'Thương hiệu: SINGPC | Loại: All-in-One | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i3 | RAM: 8GB | SSD: 256GB', 9490000, 12, NULL),
                (28, 3, N'SINGPC AIO S24-i512400 23.8 inch', N'Thương hiệu: SINGPC | Loại: All-in-One | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 8GB | SSD: 512GB', 13990000, 10, NULL),
                (29, 3, N'SINGPC AIO S27-i713700 27 inch', N'Thương hiệu: SINGPC | Loại: All-in-One | Nhu cầu: Đồ họa, Kỹ thuật | CPU: Intel Core i7 | RAM: 16GB | SSD: 512GB', 22490000, 5, NULL),
                (30, 2, N'SINGPC Mini X-Box M500 Core i5', N'Thương hiệu: SINGPC | Loại: Mini PC | Nhu cầu: Học tập - văn phòng | CPU: Intel Core i5 | RAM: 8GB | SSD: 256GB', 8990000, 14, NULL)
            ) AS Source (ProductId, CategoryId, Name, Specifications, Price, StockQuantity, ImageUrl)
            ON Target.ProductId = Source.ProductId
            WHEN NOT MATCHED THEN
                INSERT (ProductId, CategoryId, Name, Specifications, Price, StockQuantity, ImageUrl)
                VALUES (Source.ProductId, Source.CategoryId, Source.Name, Source.Specifications, Source.Price, Source.StockQuantity, Source.ImageUrl);

            SET IDENTITY_INSERT dbo.Products OFF;

            PRINT N'Đã nạp 30 sản phẩm mẫu vào catalog trống.';
        END
    END
    ELSE PRINT N'Đã giữ dữ liệu danh mục, sản phẩm và giao dịch hiện có.';

COMMIT TRANSACTION;
PRINT N'Đã đồng bộ schema, Admin=1/Customer=2. Không thay mật khẩu hiện có.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    BEGIN TRY
        SET IDENTITY_INSERT dbo.Products OFF;
        SET IDENTITY_INSERT dbo.Categories OFF;
        SET IDENTITY_INSERT dbo.Roles OFF;
    END TRY
    BEGIN CATCH
        PRINT N'Mở cửa sổ truy vấn mới nếu cần chạy lại.';
    END CATCH;
    THROW;
END CATCH;
GO
SELECT DB_NAME() AS DatabaseName;
SELECT RoleId,RoleName FROM dbo.Roles ORDER BY RoleId;
SELECT COUNT(*) AS Products FROM dbo.Products;
SELECT COUNT(*) AS Orders FROM dbo.Orders;
GO
