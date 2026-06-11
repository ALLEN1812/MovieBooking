# Online Movie Booking

Hệ thống đặt vé xem phim trực tuyến xây dựng bằng ASP.NET Core MVC + Entity Framework Core + SQL Server.

---

## Yêu cầu hệ thống

| Công cụ | Phiên bản |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 trở lên |
| [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) | 2019 trở lên (hoặc SQL Server Express) |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) hoặc VS Code | Tuỳ chọn |

---

## Cài đặt và chạy

### Bước 1 — Clone dự án

```bash
git clone https://github.com/ALLEN1812/MovieBooking.git
cd MovieBooking
```

### Bước 2 — Cấu hình chuỗi kết nối

Mở file `appsettings.json`, chỉnh lại chuỗi kết nối cho phù hợp với SQL Server của bạn:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=MovieBookingDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> Nếu dùng SQL Server với tài khoản (SQL Authentication), thay bằng:
> `Server=localhost;Database=MovieBookingDb;User Id=sa;Password=yourpassword;TrustServerCertificate=True;`

### Bước 3 — Khôi phục packages

```bash
dotnet restore
```

### Bước 4 — Chạy ứng dụng

```bash
dotnet run
```

Ứng dụng tự động tạo schema database khi khởi động lần đầu. Dừng lại sau khi thấy dòng `Now listening on...`

### Bước 5 — Nạp dữ liệu mẫu

```bash
sqlcmd -S localhost -d MovieBookingDb -E -C -i seed_data.sql
```

> File `seed_data.sql` chứa sẵn: 5 rạp, 9 phim, 6 người dùng, 12 phòng, 462 ghế, 25 suất chiếu, 16 đặt vé mẫu.

### Bước 6 — Chạy lại ứng dụng

```bash
dotnet run
```

Truy cập: **http://localhost:5000** (hoặc cổng hiển thị trong terminal)

---

## Tài khoản mặc định

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@moviebooking.vn | Admin@123 |
| Người dùng mẫu | nguyen.van.an@gmail.com | User@123 |
| Người dùng mẫu | tran.thi.binh@gmail.com | User@123 |

---

## Tính năng

- Xem danh sách phim đang chiếu / sắp chiếu
- Chọn suất chiếu, phòng chiếu và ghế ngồi
- Thanh toán qua MoMo hoặc chuyển khoản ngân hàng
- Quản lý đặt vé và lịch sử giao dịch
- Thông báo xác nhận / huỷ vé
- Trang quản trị (Admin): quản lý rạp, phim, suất chiếu, người dùng

---

## Cấu trúc thư mục

```
OnlineMovieBooking/
├── Controllers/        # Xử lý request
├── Data/               # DbContext và seed dữ liệu
├── Models/             # Entity và ViewModel
├── Services/           # Business logic
├── Views/              # Giao diện Razor
├── wwwroot/            # CSS, JS, hình ảnh tĩnh
├── appsettings.json    # Cấu hình ứng dụng
└── Program.cs          # Entry point
```

---

## Lưu ý

- File `moviebooking.db` chỉ dùng cho phát triển cục bộ, không dùng cho production.
- Khoá API Cloudinary và MoMo trong `appsettings.json` là môi trường demo — thay bằng khoá thật khi triển khai thực tế.
- Không commit `appsettings.Development.json` chứa thông tin nhạy cảm lên repository công khai.
