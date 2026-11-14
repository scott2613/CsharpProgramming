# ComputerStore - bản source đã rà soát đồng bộ

## 1. Dùng bản này như thế nào

Đây là source đầy đủ, giữ 5 project chính. Giải nén vào THƯ MỤC MỚI, mở
ComputerStoreSystem.sln. Không chép đè toàn bộ lên source cũ vì các file đã gộp
có thể còn lại và gây khai báo class trùng. Giữ thư mục cũ để đối chiếu/khôi phục.
Không copy bin/obj của bản cũ. Chưa tự động deploy, kết nối hoặc sửa DB trên máy bạn.

Thứ tự áp dụng:
1. Sao lưu database, dừng API/Admin.
2. SSMS: mở BMTDStoreDB.sql trong bản mới và Execute toàn bộ trên đúng SQL Server.
   Bản mới KHÔNG reset sản phẩm/ảnh/đơn/giỏ. Nếu có cảnh báo dữ liệu định danh trùng
   hoặc cấu trúc khác, dừng và gửi lỗi; không xóa bảng để lách kiểm tra.
3. Sửa ComputerStore.API/appsettings.Development.json cho đúng instance SQL của bạn.
   Mẫu dùng Windows Authentication, localhost\MSSQLSERVER03 và BMTDStoreDB.
   Nếu dùng SQL Login, đặt ConnectionStrings__DefaultConnection qua môi trường hoặc
   cấu hình local bị gitignore; không commit mật khẩu. Chuỗi rỗng ở appsettings.json
   là chủ ý để production không dùng nhầm tài khoản SQL đã lộ trong source.
4. Cài .NET SDK 9, Visual Studio có workload ASP.NET và Desktop .NET.
5. Rebuild Solution. Chạy API profile https (https://localhost:7275).
6. Chạy Admin, dùng tài khoản Admin hiện có. Mật khẩu cũ không bị SQL đổi.
   Admin dùng https://localhost:7275/api/; nếu đổi URL đặt COMPUTERSTORE_API_BASEURL.
7. Kiểm tra các luồng trong HuongDan.txt, bao gồm cấu hình email khôi phục mật khẩu.

## 2. Tạo Admin cho database hoàn toàn mới

SQL không chứa mật khẩu mặc định. Chỉ khi cần tạo Admin MỚI, mở PowerShell tại
thư mục solution, dùng mật khẩu do bạn chọn (từ 8 đến 128 ký tự):

```powershell
$env:BootstrapAdmin__Username = Read-Host 'Ten dang nhap Admin moi'
$adminSecret = Read-Host 'Mat khau Admin (8-128 ky tu)' -AsSecureString
$env:BootstrapAdmin__Password = [System.Net.NetworkCredential]::new('', $adminSecret).Password
try {
    dotnet run --project ComputerStore.API --launch-profile https -- --create-admin
} finally {
    Remove-Item Env:BootstrapAdmin__Username -ErrorAction SilentlyContinue
    Remove-Item Env:BootstrapAdmin__Password -ErrorAction SilentlyContinue
    $adminSecret.Dispose()
}
```

Lệnh không chạy web server, không nâng quyền/reset mật khẩu tài khoản đã tồn tại,
không in mật khẩu ra log; mật khẩu được băm bằng PasswordHasher hiện có.
Nếu định danh đã tồn tại, dùng tài khoản Admin hiện tại để quản lý; không tự gán
RoleId=1 cho tài khoản khách bằng SQL.

## 3. SQL hay EF migration?

Bản triển khai này chọn BMTDStoreDB.sql làm đường nâng cấp schema hiện tại.
Các EF migration/snapshot trong Infrastructure/Migrations là LỊCH SỬ CŨ, còn thiếu
Address/UserId/ShoppingCartItems và seed danh mục không còn giống schema hiện nay.
Đã GIỮ nguyên các file này để không viết lại lịch sử hoặc giả rằng đã sinh migration.
KHÔNG chạy Update-Database/dotnet ef database update sau script SQL này.
Muốn chuyển hẳn sang EF migration cần baseline theo DB thật, tạo migration bằng
SDK và đối chiếu script trên bản sao DB; công việc đó chưa được kiểm chứng ở đây.
AddDbContext/EF truy vấn và SaveChanges vẫn dùng bình thường, không tự migrate khi chạy.

## 4. Chạy các kiểm tra nghiệp vụ

```powershell
dotnet run --project tests/BusinessChecks/BusinessChecks.csproj -c Release
```

Harness chạy Application service thật với bộ lưu trữ trong bộ nhớ. Đã chạy thành công 14 ca, gồm mua lại đơn, quyền sở hữu đơn và rollback khi thiếu tồn kho. Cần SQL Server và Windows để kiểm tra tích hợp database và giao diện WinForms.

## 5. Docker và workflow

Docker build tại thư mục solution:
```powershell
docker build -t computerstore .
```
Container chỉ chạy ComputerStore.API (đã có MVC), cổng trong container 8080 hoặc PORT,
không chạy Admin Windows bên trong Linux. SQL Server là dịch vụ ngoài container;
localhost trong container KHÔNG phải SQL Server trên máy Windows.

Production cần HTTPS ở reverse proxy và đặt địa chỉ IP proxy tin cậy qua
ReverseProxy__KnownProxies__0 (và các chỉ số tiếp theo nếu có).
Không cấu hình tin mọi proxy. Cookie production chỉ gửi qua HTTPS. Nếu chỉ thử
HTTP ở localhost, dùng Development; không công khai Development lên Internet.
Đặt ConnectionStrings__DefaultConnection cho SQL TCP host/port thật; với Docker
Desktop thường cần host.docker.internal và port SQL đã bật, không dùng Windows
Integrated Authentication mặc định của appsettings.Development.json trong container.
Giữ volume cho /home/app/.aspnet/DataProtection-Keys nếu cần cookie còn hợp lệ sau
recreate container; thư mục chạy dưới user app. Chưa kiểm thử hosting/proxy cụ thể.

Workflow .github/workflows/main.yml:
- Windows: restore/build cả solution, chạy business checks, publish Admin artifact.
- Linux: restore/publish API artifact.
- Dùng .NET9; không còn ComputerStore.Web. Đây là CI/package, CHƯA có CD tự triển khai.

## 6. Danh mục và ảnh

BMTDStoreDB.sql nạp bộ 30 mẫu của bạn khi sản phẩm, đơn và giỏ đều trống, đồng thời kiểm tra mã danh mục. Bản gửi thiếu ảnh sản phẩm thật trong wwwroot. Đã khôi phục CSS, JavaScript và ảnh dự phòng từ bản source trước. Hãy chép ảnh riêng của bạn vào ComputerStore.API/wwwroot/images hoặc nhập URL công khai trong Admin. Đường dẫn /images/catalog/{id}.jpg có thể đọc JPEG từ SQL nếu có ảnh thật. JPEG mẫu 1×1 được thay bằng ảnh dự phòng khi hiển thị.

## 7. Kiểm tra trên môi trường đóng gói

- Build toàn bộ ComputerStoreSystem.sln bằng SDK .NET 9, với EnableWindowsTargeting trên Linux: 0 lỗi, 0 cảnh báo.
- 14 ca BusinessChecks đạt.
- Kiểm tra HTTP trang đăng nhập, quên mật khẩu khi thiếu SMTP, liên kết reset sai và file SVG dự phòng.
- Còn cần thử kết nối SQL Server thực tế, gửi SMTP thật và chạy giao diện Admin trên Windows.
