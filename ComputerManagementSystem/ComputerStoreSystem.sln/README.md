# BMTDStore — Phần mềm quản lý và bán máy tính

BMTDStore hỗ trợ khách hàng xem sản phẩm, mua hàng và theo dõi đơn hàng trên Web. Nhân viên quản trị sử dụng ứng dụng Desktop để quản lý sản phẩm, đơn hàng, tài khoản và xem dashboard.

## 1. Thành phần hệ thống

| Project | Vai trò |
|---|---|
| ComputerStore.API | Chạy Web bán hàng và cung cấp API cho ứng dụng Admin |
| ComputerStore.Admin | Ứng dụng quản trị Desktop bằng Windows Forms |
| ComputerStore.Application | Xử lý nghiệp vụ, định nghĩa dịch vụ và các giao tiếp |
| ComputerStore.Core | Các thực thể và thành phần dùng chung |
| ComputerStore.Infrastructure | Kết nối cơ sở dữ liệu và truy xuất dữ liệu |

Web và Admin sử dụng dữ liệu nghiệp vụ thông qua hệ thống backend kết nối SQL Server.

Giao diện Web nằm trong `ComputerStore.API`; không cần chạy project `ComputerStore.Web` riêng.

## 2. Yêu cầu môi trường

- Windows để chạy ứng dụng Admin.
- .NET SDK 9.
- Visual Studio có hai workload:
  - ASP.NET and web development.
  - .NET desktop development.
- SQL Server và SQL Server Management Studio (SSMS).
- Git để tải mã nguồn.

Kiểm tra .NET SDK:

```bash
dotnet --list-sdks
```

Danh sách cần có phiên bản `9.0.xxx`.

## 3. Tải mã nguồn

Mở Git Bash và chạy:

```bash
git clone --branch main --single-branch https://github.com/scott2613/CsharpProgramming.git BMTDStore
```

Trong cấu trúc repository hiện tại, thư mục chứa solution là:

```text
BMTDStore/
└── ComputerManagementSystem/
    └── ComputerStoreSystem.sln/       ← Thư mục
        ├── ComputerStoreSystem.sln   ← File solution
        ├── ComputerStore.API/
        ├── ComputerStore.Admin/
        ├── ComputerStore.Application/
        ├── ComputerStore.Core/
        ├── ComputerStore.Infrastructure/
        ├── Database/
        └── tests/
```

Mở **file** `ComputerStoreSystem.sln` bằng Visual Studio.

Các lệnh `dotnet` bên dưới được chạy tại thư mục chứa file solution.

Không chép đè mã nguồn mới lên bản cũ có cấu trúc khác vì file cũ có thể còn lại và gây trùng lớp hoặc phương thức.

## 4. Chuẩn bị cơ sở dữ liệu

1. Mở SSMS và kết nối SQL Server.
2. Chuẩn bị cơ sở dữ liệu `BMTDStoreDB` có đầy đủ cấu trúc bảng của hệ thống.
3. Nếu đã có database đang sử dụng, sao lưu trước khi chạy script thay đổi dữ liệu.
4. Kiểm tra nội dung từng script trong thư mục `Database` trước khi thực thi.

**Lưu ý về bộ SQL hiện tại:** file `Database/BMTDStoreDB.sql` trong bản đã kiểm tra là script xử lý dữ liệu sản phẩm trên database có sẵn, chưa phải bộ script tạo mới đầy đủ database. Không dùng riêng file này để cài đặt trên máy chưa có CSDL.

Đối với máy mới, cần bổ sung bản backup database đầy đủ hoặc script tạo schema tương ứng.

Các EF migration hiện có thuộc phiên bản cũ. Không chạy `Update-Database` hoặc `dotnet ef database update` khi chưa đối chiếu với cấu trúc database đang sử dụng.

## 5. Cấu hình kết nối SQL Server

Cấu hình khóa:

```text
ConnectionStrings:DefaultConnection
```

Có thể dùng .NET User Secrets để lưu cấu hình riêng trên máy.

Mở PowerShell tại thư mục chứa solution:

```powershell
# Khởi tạo User Secrets nếu project chưa có
dotnet user-secrets init --project ComputerStore.API

# Thay TEN_MAY\TEN_INSTANCE bằng SQL Server của bạn
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=TEN_MAY\TEN_INSTANCE;Database=BMTDStoreDB;Trusted_Connection=True;TrustServerCertificate=True;" --project ComputerStore.API
```

Ví dụ SQL Server Express thường sử dụng:

```text
Server=.\SQLEXPRESS;Database=BMTDStoreDB;Trusted_Connection=True;TrustServerCertificate=True;
```

Tên server phải trùng với server kết nối được trong SSMS.

Không đưa mật khẩu SQL Server hoặc SMTP vào GitHub.

## 6. Khôi phục thư viện và build

Tại thư mục chứa solution:

```bash
# Tải các gói NuGet
dotnet restore ComputerStoreSystem.sln

# Biên dịch toàn bộ solution
dotnet build ComputerStoreSystem.sln -c Release
```

Trong Visual Studio cũng có thể chọn:

1. Restore NuGet Packages.
2. Build → Rebuild Solution.
3. Mở Error List và chọn bộ lọc Build để xem lỗi biên dịch.

## 7. Chạy Web và API

Lần đầu chạy HTTPS trên máy Windows:

```bash
dotnet dev-certs https --trust
```

Chạy project:

```bash
dotnet run --project ComputerStore.API --launch-profile https
```

Địa chỉ mặc định:

| Thành phần | Địa chỉ |
|---|---|
| Web bán hàng | https://localhost:7275/Product |
| API dành cho Admin | https://localhost:7275/api/ |

Giữ cửa sổ chạy API mở trong suốt quá trình sử dụng Admin.

Nếu thay đổi cổng chạy API, cần cập nhật địa chỉ kết nối tương ứng cho Admin.

## 8. Chạy ứng dụng Admin

Mở cửa sổ terminal thứ hai tại thư mục chứa solution:

```bash
dotnet run --project ComputerStore.Admin
```

Đăng nhập bằng tài khoản có quyền Admin.

Admin mặc định kết nối:

```text
https://localhost:7275/api/
```

Nếu dùng địa chỉ API khác, đặt biến môi trường trước khi chạy.

Ví dụ trong PowerShell:

```powershell
$env:COMPUTERSTORE_API_BASEURL = "https://localhost:7275/api/"
dotnet run --project ComputerStore.Admin
```

Trong Visual Studio có thể cấu hình chạy đồng thời hai project:

- ComputerStore.API.
- ComputerStore.Admin.

## 9. Tạo tài khoản Admin mới

Nếu chưa có tài khoản quản trị, sau khi chuẩn bị database đầy đủ, mở PowerShell tại thư mục chứa solution:

```powershell
$env:BootstrapAdmin__Username = Read-Host "Ten dang nhap Admin moi"
$adminSecret = Read-Host "Mat khau Admin (8-128 ky tu)" -AsSecureString
$env:BootstrapAdmin__Password = [System.Net.NetworkCredential]::new('', $adminSecret).Password

try {
    dotnet run --project ComputerStore.API --launch-profile https -- --create-admin
}
finally {
    Remove-Item Env:BootstrapAdmin__Username -ErrorAction SilentlyContinue
    Remove-Item Env:BootstrapAdmin__Password -ErrorAction SilentlyContinue
    $adminSecret.Dispose()
}
```

Lệnh này tạo tài khoản mới; không dùng để đặt lại mật khẩu hoặc nâng quyền tài khoản đã tồn tại.

Sau khi tạo xong, chạy lại API theo hướng dẫn ở mục 7.

## 10. Hướng dẫn sử dụng Web

### Tài khoản

- Đăng ký tài khoản khách hàng.
- Đăng nhập để sử dụng các chức năng cá nhân.
- Xem và cập nhật thông tin tài khoản.
- Đổi mật khẩu khi cần.

Chức năng khôi phục mật khẩu qua email cần cấu hình SMTP hoạt động.

### Xem và tìm sản phẩm

- Mở trang sản phẩm.
- Nhập từ khóa tìm kiếm hoặc sử dụng các bộ lọc.
- Chọn sản phẩm để xem thông tin chi tiết.
- Thêm sản phẩm vào giỏ hàng.

### Giỏ hàng và đặt hàng

1. Mở giỏ hàng.
2. Kiểm tra sản phẩm, số lượng và tổng tiền.
3. Điều chỉnh số lượng hoặc xóa sản phẩm nếu cần.
4. Chuyển đến trang thanh toán.
5. Nhập họ tên, số điện thoại và địa chỉ nhận hàng.
6. Chọn phương thức thanh toán.
7. Nhấn hoàn tất đặt hàng.

Các phương thức thanh toán hiện sử dụng:

- Thanh toán khi nhận hàng (COD).
- Chuyển khoản ngân hàng, nhân viên xác nhận.

Chuyển khoản cần được nhân viên kiểm tra và xác nhận; hệ thống chưa tự động đối soát với ngân hàng.

### Theo dõi đơn hàng

- Mở mục đơn hàng trong tài khoản.
- Xem trạng thái và tổng tiền của từng đơn.
- Nhấn Chi tiết để mở thông tin đơn hàng.
- Sử dụng thao tác mua lại, hủy hoặc ẩn đơn khi trạng thái đơn cho phép.

## 11. Hướng dẫn sử dụng Admin

Chọn chức năng trên menu bên trái. Nội dung tương ứng hiển thị trong khung bên phải.

### Dashboard

- Chọn khoảng thời gian thống kê.
- Nhấn Làm mới để cập nhật số liệu.
- Xem doanh thu của đơn hoàn thành.
- Xem số đơn mới và số đơn chờ.
- Xem biểu đồ doanh thu và trạng thái đơn hàng.
- Xem sản phẩm bán chạy và sản phẩm tồn kho thấp.

Doanh thu được thống kê theo ngày đặt của đơn đã hoàn thành. Tồn kho và số khách hàng phản ánh dữ liệu hiện tại.

### Quản lý đơn hàng

1. Tìm đơn theo mã đơn hoặc thông tin khách hàng.
2. Lọc theo trạng thái.
3. Chọn đơn để xem danh sách mặt hàng.
4. Chọn trạng thái mới và nhấn Lưu trạng thái.
5. Dùng Sửa giao hàng để cập nhật thông tin nhận hàng.
6. Chỉ xóa đơn sau khi kiểm tra và xác nhận đúng đơn.

Trạng thái chỉ được chuyển khi đáp ứng quy tắc nghiệp vụ của hệ thống.

### Quản lý sản phẩm

- Tìm và chọn sản phẩm trong danh sách.
- Nhập thông tin để thêm sản phẩm mới.
- Chỉnh sửa thông tin và lưu thay đổi.
- Cập nhật giá, tồn kho, cấu hình và đường dẫn ảnh.
- Xem chi tiết hoặc các đơn hàng liên quan.
- Xóa sản phẩm khi hệ thống cho phép.

### Quản lý tài khoản

- Tìm tài khoản theo thông tin hiển thị.
- Chọn tài khoản để xem thông tin.
- Tạo tài khoản mới hoặc cập nhật thông tin.
- Quản lý vai trò.
- Cấp lại mật khẩu khi cần.
- Xem các đơn hàng liên quan.
- Xóa tài khoản khi hệ thống cho phép.

### Đăng xuất

Nhấn Đăng xuất trên menu để kết thúc phiên quản trị.

## 12. Chạy kiểm tra nghiệp vụ

Tại thư mục chứa solution:

```bash
dotnet run --project tests/BusinessChecks/BusinessChecks.csproj -c Release
```

Các kiểm tra này sử dụng bộ lưu trữ trong bộ nhớ, không thay thế kiểm thử trên SQL Server và giao diện thực tế.

Trước khi demo, cần kiểm tra:

- Đăng nhập khách hàng và Admin.
- Tìm kiếm, lọc và xem sản phẩm.
- Thêm, sửa, xóa sản phẩm.
- Thêm giỏ hàng và thay đổi số lượng.
- Đặt hàng và kiểm tra tồn kho.
- Xem chi tiết và cập nhật trạng thái đơn.
- Kiểm tra dữ liệu đồng bộ giữa Web và Admin.
- Kiểm tra dashboard sau khi thay đổi dữ liệu.
- Kiểm tra bố cục Admin khi thay đổi kích thước cửa sổ.

## 13. Lỗi thường gặp

| Hiện tượng | Cách kiểm tra |
|---|---|
| Không kết nối được database | Kiểm tra SQL Server đang chạy, tên instance, tên database và quyền truy cập |
| Invalid object name hoặc Invalid column name | Đối chiếu cấu trúc database với phiên bản mã nguồn |
| Admin không kết nối được API | Chạy API trước, kiểm tra cổng và COMPUTERSTORE_API_BASEURL |
| Trình duyệt cảnh báo HTTPS localhost | Chạy dotnet dev-certs https --trust |
| Thiếu .NET SDK | Cài .NET SDK 9 và mở lại Visual Studio |
| Lỗi trùng lớp hoặc phương thức CS0101/CS0111 | Kiểm tra file trùng, đặc biệt thư mục lồng như Controllers/Controllers |
| Không gửi được email khôi phục mật khẩu | Kiểm tra cấu hình và kết nối SMTP |
| Không hiển thị ảnh sản phẩm | Kiểm tra đường dẫn ảnh, file trong wwwroot và URL lưu trong dữ liệu |
| Pull bị chặn do conflict | Giải quyết thay đổi đang xung đột trước khi pull tiếp |

## 14. Lưu ý khi cập nhật

- Sao lưu database trước khi thay đổi cấu trúc hoặc chạy script cập nhật dữ liệu.
- Không copy thư mục bin, obj và .vs giữa các bản source.
- Khi thay toàn bộ một project, loại bỏ bản cũ trước khi chép bản mới để tránh giữ lại file thừa.
- Giữ cấu hình kết nối và mật khẩu riêng trên từng máy.
- Build thành công chưa đồng nghĩa mọi chức năng đã được kiểm thử.
- Sau khi cập nhật GitHub, nên clone vào thư mục mới để kiểm tra khả năng cài đặt và chạy từ mã nguồn.