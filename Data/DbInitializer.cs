using OnlineMovieBooking.Models;

namespace OnlineMovieBooking.Data;

public static class DbInitializer
{
    private static readonly string[] RowLabels = { "A", "B", "C", "D", "E", "F" };

    public static void Seed(ApplicationDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.Cinemas.Any())
        {
            SeedMainData(db);
            SeedExtraMovies(db);
        }

        SeedExtraCinemas(db);
        SeedExtraShowtimes(db);
        SeedSampleUsers(db);
        SeedSampleBookings(db);
    }

    // ─── Lần chạy đầu: 2 rạp, phòng chiếu, ghế, 7 phim, suất chiếu, admin ──
    private static void SeedMainData(ApplicationDbContext db)
    {
        var cinema1 = new Cinema { Name = "MovieBooking Tuy Hòa", Location = "Tuy Hòa, Phú Yên" };
        var cinema2 = new Cinema { Name = "MovieBooking Sông Cầu", Location = "Sông Cầu, Phú Yên" };
        db.Cinemas.AddRange(cinema1, cinema2);
        db.SaveChanges();

        var room1 = new Room { CinemaId = cinema1.Id, Name = "Phòng 1", TotalRows = 5, TotalCols = 8 };
        var room2 = new Room { CinemaId = cinema1.Id, Name = "Phòng 2", TotalRows = 5, TotalCols = 8 };
        var room3 = new Room { CinemaId = cinema2.Id, Name = "Phòng 1", TotalRows = 5, TotalCols = 8 };
        db.Rooms.AddRange(room1, room2, room3);
        db.SaveChanges();

        foreach (var r in new[] { room1, room2, room3 })
            AddSeatsForRoom(db, r.Id, r.TotalRows, r.TotalCols);
        db.SaveChanges();

        var movies = new[]
        {
            new Movie { Title = "Avengers: Endgame", Genre = "Hành động, Phiêu lưu", DurationMin = 181, Status = "now_showing", ReleaseDate = new DateOnly(2019, 4, 26), Director = "Anthony & Joe Russo", PosterUrl = "https://upload.wikimedia.org/wikipedia/en/0/0d/Avengers_Endgame_poster.jpg", Description = "Sau sự kiện Infinity War, các Avengers còn lại tập hợp lại để đảo ngược thiệt hại Thanos gây ra trong vũ trụ." },
            new Movie { Title = "Joker", Genre = "Tội phạm, Kịch tính", DurationMin = 122, Status = "now_showing", ReleaseDate = new DateOnly(2019, 10, 4), Director = "Todd Phillips", PosterUrl = "https://upload.wikimedia.org/wikipedia/en/e/e1/Joker_%282019_film%29_poster.jpg", Description = "Câu chuyện về nguồn gốc của tên phản diện Joker nổi tiếng trong vũ trụ DC." },
            new Movie { Title = "Parasite", Genre = "Hài kịch đen, Kinh dị", DurationMin = 132, Status = "now_showing", ReleaseDate = new DateOnly(2019, 5, 30), Director = "Bong Joon-ho", Description = "Gia đình nghèo khó xâm nhập vào cuộc sống xa hoa của một gia đình giàu có theo những cách bất ngờ." },
            new Movie { Title = "Spider-Man: No Way Home", Genre = "Siêu anh hùng, Phiêu lưu", DurationMin = 148, Status = "now_showing", ReleaseDate = new DateOnly(2021, 12, 17), Director = "Jon Watts", PosterUrl = "https://upload.wikimedia.org/wikipedia/en/0/00/Spider-Man_No_Way_Home_official_poster.jpg", Description = "Peter Parker nhờ Doctor Strange xóa ký ức về danh tính của mình, dẫn đến hậu quả không lường trước." },
            new Movie { Title = "The Batman", Genre = "Hành động, Tội phạm", DurationMin = 176, Status = "coming_soon", ReleaseDate = new DateOnly(2025, 10, 1), Director = "Matt Reeves", Description = "Bruce Wayne điều tra một loạt tội phạm bí ẩn ở Gotham City với vai trò thám tử." },
            new Movie { Title = "Doctor Strange in the Multiverse of Madness", Genre = "Siêu anh hùng, Kinh dị", DurationMin = 126, Status = "coming_soon", ReleaseDate = new DateOnly(2025, 12, 5), Director = "Sam Raimi", Description = "Doctor Strange phiêu lưu qua đa vũ trụ cùng với America Chavez." },
            new Movie { Title = "Top Gun: Maverick", Genre = "Hành động, Kịch tính", DurationMin = 130, Status = "now_showing", ReleaseDate = new DateOnly(2022, 5, 27), Director = "Joseph Kosinski", PosterUrl = "https://upload.wikimedia.org/wikipedia/en/1/13/Top_Gun_Maverick_Poster.jpg", Description = "Phi công huyền thoại Pete Mitchell tiếp tục bứt phá giới hạn bay bổng." }
        };
        db.Movies.AddRange(movies);
        db.SaveChanges();

        var now = DateTime.Now;
        db.Showtimes.AddRange(
            new Showtime { MovieId = movies[0].Id, RoomId = room1.Id, StartTime = now.AddDays(1).Date.AddHours(10), Price = 90000, Subtitle = "Vietsub" },
            new Showtime { MovieId = movies[0].Id, RoomId = room2.Id, StartTime = now.AddDays(1).Date.AddHours(14), Price = 90000, Subtitle = "Thuyết minh" },
            new Showtime { MovieId = movies[1].Id, RoomId = room1.Id, StartTime = now.AddDays(1).Date.AddHours(17), Price = 90000, Subtitle = "Vietsub" },
            new Showtime { MovieId = movies[2].Id, RoomId = room3.Id, StartTime = now.AddDays(2).Date.AddHours(9),  Price = 90000, Subtitle = "Phụ đề Anh" },
            new Showtime { MovieId = movies[3].Id, RoomId = room2.Id, StartTime = now.AddDays(2).Date.AddHours(19), Price = 90000, Subtitle = "Vietsub" },
            new Showtime { MovieId = movies[6].Id, RoomId = room1.Id, StartTime = now.AddDays(3).Date.AddHours(15), Price = 90000, Subtitle = "Thuyết minh" }
        );
        db.SaveChanges();

        if (!db.Users.Any(u => u.Email == "admin@moviebooking.vn"))
        {
            db.Users.Add(new User
            {
                Name = "Quản trị viên",
                Email = "admin@moviebooking.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "admin",
                Phone = "0901234567",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }

    // ─── 3 rạp thêm: Đà Nẵng, Quy Nhơn, Nha Trang — mỗi rạp 3 phòng ──────
    private static void SeedExtraCinemas(ApplicationDbContext db)
    {
        var list = new[]
        {
            ("MovieBooking Đà Nẵng",   "Hải Châu, Đà Nẵng"),
            ("MovieBooking Quy Nhơn",  "Quy Nhơn, Bình Định"),
            ("MovieBooking Nha Trang", "Nha Trang, Khánh Hòa"),
        };
        foreach (var (name, location) in list)
        {
            if (db.Cinemas.Any(c => c.Name == name)) continue;
            var cinema = new Cinema { Name = name, Location = location };
            db.Cinemas.Add(cinema);
            db.SaveChanges();
            var rooms = new[]
            {
                new Room { CinemaId = cinema.Id, Name = "Phòng 1",   TotalRows = 5, TotalCols = 8  },
                new Room { CinemaId = cinema.Id, Name = "Phòng 2",   TotalRows = 5, TotalCols = 10 },
                new Room { CinemaId = cinema.Id, Name = "Phòng VIP", TotalRows = 4, TotalCols = 6  },
            };
            db.Rooms.AddRange(rooms);
            db.SaveChanges();
            foreach (var r in rooms)
                AddSeatsForRoom(db, r.Id, r.TotalRows, r.TotalCols);
            db.SaveChanges();
        }
    }

    // ─── Suất chiếu cho 9 phim Việt / châu Á mới ────────────────────────────
    private static void SeedExtraShowtimes(ApplicationDbContext db)
    {
        var allRooms = db.Rooms.OrderBy(r => r.Id).ToList();
        if (!allRooms.Any()) return;
        var today = DateTime.Now.Date;

        var plans = new (string Title, int Day, int Hour, string Sub, decimal Price)[]
        {
            ("Madames Thanh Sắc",                                              1,  9,  "Vietsub",      90000m),
            ("Madames Thanh Sắc",                                              1,  14, "Thuyết minh",  90000m),
            ("Madames Thanh Sắc",                                              2,  19, "Vietsub",      90000m),
            ("Tây Du Ký Đại Náo (Dị bản Thái Lan)",                           1,  10, "Phụ đề Anh",   95000m),
            ("Tây Du Ký Đại Náo (Dị bản Thái Lan)",                           1,  15, "Vietsub",      95000m),
            ("Tây Du Ký Đại Náo (Dị bản Thái Lan)",                           2,  20, "Thuyết minh",  95000m),
            ("Mạc Ly",                                                         2,  9,  "Vietsub",      90000m),
            ("Mạc Ly",                                                         2,  14, "Vietsub",      90000m),
            ("Đếm Ngày Xa Mẹ",                                                3,  11, "Vietsub",      90000m),
            ("Đếm Ngày Xa Mẹ",                                                3,  16, "Thuyết minh",  90000m),
            ("Đếm Ngày Xa Mẹ",                                                4,  21, "Vietsub",      90000m),
            ("Song Hỷ Lâm Nguy",                                              1,  10, "Vietsub",      85000m),
            ("Song Hỷ Lâm Nguy",                                              1,  15, "Thuyết minh",  85000m),
            ("Kỳ Tích Đường Chạy (The Golden Spike)",                         4,  13, "Vietsub",      95000m),
            ("Kỳ Tích Đường Chạy (The Golden Spike)",                         5,  18, "Vietsub",      95000m),
            ("Bí Mật Đảo Giấu Vàng (Treasure Island: The New Horizon)",       2,  10, "Vietsub",     120000m),
            ("Bí Mật Đảo Giấu Vàng (Treasure Island: The New Horizon)",       2,  14, "Phụ đề Anh",  120000m),
            ("Bí Mật Đảo Giấu Vàng (Treasure Island: The New Horizon)",       3,  18, "Thuyết minh", 120000m),
            ("Gió Thổi Bán Hạ",                                               2,  9,  "Vietsub",      90000m),
            ("Gió Thổi Bán Hạ",                                               2,  15, "Vietsub",      90000m),
            ("Thanh Xuân Không Quay Đầu",                                     3,  11, "Vietsub",      90000m),
            ("Thanh Xuân Không Quay Đầu",                                     3,  17, "Thuyết minh",  90000m),
            ("Thanh Xuân Không Quay Đầu",                                     4,  20, "Vietsub",      90000m),
        };

        int ri = 0;
        foreach (var (title, day, hour, sub, price) in plans)
        {
            var movie = db.Movies.FirstOrDefault(m => m.Title == title);
            if (movie == null) continue;
            var startTime = today.AddDays(day).AddHours(hour);
            if (db.Showtimes.Any(s => s.MovieId == movie.Id && s.StartTime == startTime)) continue;
            db.Showtimes.Add(new Showtime
            {
                MovieId   = movie.Id,
                RoomId    = allRooms[ri++ % allRooms.Count].Id,
                StartTime = startTime,
                Price     = price,
                Subtitle  = sub
            });
        }
        db.SaveChanges();
    }

    // ─── 5 người dùng mẫu ────────────────────────────────────────────────────
    private static void SeedSampleUsers(ApplicationDbContext db)
    {
        var users = new[]
        {
            ("Nguyễn Văn An",   "nguyen.van.an@gmail.com",   "0901111111", "123 Trần Phú, Tuy Hòa"),
            ("Trần Thị Bình",   "tran.thi.binh@gmail.com",   "0902222222", "456 Lê Lợi, Quy Nhơn"),
            ("Lê Hoàng Nam",    "le.hoang.nam@gmail.com",    "0903333333", "789 Nguyễn Huệ, Đà Nẵng"),
            ("Phạm Thị Lan",    "pham.thi.lan@gmail.com",    "0904444444", "321 Hùng Vương, Nha Trang"),
            ("Hoàng Minh Tuấn", "hoang.minh.tuan@gmail.com", "0905555555", "654 Phạm Văn Đồng, Sông Cầu"),
        };
        foreach (var (name, email, phone, address) in users)
        {
            if (db.Users.Any(u => u.Email == email)) continue;
            db.Users.Add(new User
            {
                Name         = name,
                Email        = email,
                Phone        = phone,
                Address      = address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                Role         = "user",
                CreatedAt    = DateTime.UtcNow.AddDays(-new Random(name.GetHashCode()).Next(30, 180))
            });
        }
        db.SaveChanges();
    }

    // ─── 12 đặt vé mẫu với chi tiết ghế và thông báo ────────────────────────
    private static void SeedSampleBookings(ApplicationDbContext db)
    {
        if (db.Bookings.Any()) return;

        var users     = db.Users.Where(u => u.Role == "user").ToList();
        var showtimes = db.Showtimes.OrderBy(s => s.Id).ToList();
        var allSeats  = db.Seats.OrderBy(s => s.Id).ToList();
        if (!users.Any() || !showtimes.Any() || !allSeats.Any()) return;

        // (userIdx, showtimeIdx, rowLabel, cols, status, paymentMethod, daysAgo)
        var defs = new (int UI, int SI, string Row, int[] Cols, string Status, string Pay, int Days)[]
        {
            (0, 0, "B", new[] { 3, 4 },    "confirmed", "MoMo",  5),
            (1, 1, "C", new[] { 5, 6 },    "confirmed", "VNPay", 3),
            (2, 2, "A", new[] { 1, 2, 3 }, "pending",   "COD",   1),
            (3, 3, "D", new[] { 3, 4 },    "confirmed", "MoMo",  7),
            (4, 4, "E", new[] { 4, 5 },    "cancelled", "VNPay", 10),
            (0, 5, "B", new[] { 1, 2 },    "confirmed", "COD",   2),
            (1, 0, "C", new[] { 3 },       "confirmed", "MoMo",  6),
            (2, 1, "A", new[] { 7, 8 },    "pending",   "VNPay", 1),
            (3, 2, "D", new[] { 2, 3 },    "confirmed", "COD",   4),
            (4, 3, "B", new[] { 5, 6 },    "confirmed", "MoMo",  8),
            (0, 4, "A", new[] { 4, 5 },    "cancelled", "VNPay", 12),
            (1, 5, "C", new[] { 1, 2 },    "confirmed", "COD",   3),
        };

        foreach (var def in defs)
        {
            var user     = users[Math.Min(def.UI, users.Count - 1)];
            var showtime = showtimes[Math.Min(def.SI, showtimes.Count - 1)];
            var seats    = allSeats
                .Where(s => s.RoomId == showtime.RoomId &&
                            s.RowLabel == def.Row &&
                            def.Cols.Contains(s.ColNumber))
                .ToList();
            if (!seats.Any()) continue;

            var details = seats.Select(s => new BookingDetail
            {
                SeatId = s.Id,
                Price  = s.Type == "vip" ? showtime.Price * 1.3m : showtime.Price
            }).ToList();

            var notifications = new List<Notification>();
            if (def.Status == "confirmed")
                notifications.Add(new Notification
                {
                    UserId    = user.Id,
                    Title     = "Đặt vé thành công",
                    Message   = $"Bạn đã đặt {seats.Count} vé. Tổng tiền: {details.Sum(d => d.Price):N0} đ.",
                    Type      = "booking",
                    IsRead    = def.Days > 3,
                    CreatedAt = DateTime.UtcNow.AddDays(-def.Days)
                });
            else if (def.Status == "cancelled")
                notifications.Add(new Notification
                {
                    UserId    = user.Id,
                    Title     = "Vé đã bị huỷ",
                    Message   = "Đơn đặt vé của bạn đã bị huỷ. Vui lòng liên hệ hỗ trợ nếu cần.",
                    Type      = "booking",
                    IsRead    = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-def.Days)
                });

            db.Bookings.Add(new Booking
            {
                UserId         = user.Id,
                ShowtimeId     = showtime.Id,
                Name           = user.Name,
                Phone          = user.Phone ?? "0900000000",
                Email          = user.Email,
                PaymentMethod  = def.Pay,
                TotalPrice     = details.Sum(d => d.Price),
                Status         = def.Status,
                CreatedAt      = DateTime.UtcNow.AddDays(-def.Days),
                BookingDetails = details,
                Notifications  = notifications
            });
        }
        db.SaveChanges();
    }

    // ─── Helper: tạo ghế cho 1 phòng ────────────────────────────────────────
    private static void AddSeatsForRoom(ApplicationDbContext db, int roomId, int totalRows, int totalCols)
    {
        for (int col = 1; col <= totalCols; col++)
            for (int row = 0; row < totalRows; row++)
                db.Seats.Add(new Seat
                {
                    RoomId    = roomId,
                    RowLabel  = RowLabels[row],
                    ColNumber = col,
                    Type      = row >= totalRows - 2 ? "vip" : "standard"
                });
    }

    // ─── 9 phim Việt / châu Á ────────────────────────────────────────────────
    private static void SeedExtraMovies(ApplicationDbContext db)
    {
        var extraMovies = new[]
        {
            new Movie
            {
                Title = "Madames Thanh Sắc",
                Genre = "Chính kịch, Nghệ thuật, Lịch sử",
                DurationMin = 115,
                Director = "Thắng Vũ",
                ReleaseDate = new DateOnly(2026, 6, 19),
                Status = "coming_soon",
                Description = "Bộ phim tâm lý - chính kịch Việt Nam đang nhận được sự chú ý lớn từ truyền thông nhờ màn đối đầu ngập tràn \"thanh sắc\" và kịch tính giữa hai tên tuổi đình đám: Hồng Ánh và Thanh Hằng. Bối cảnh cổ điển, phục trang và màu phim được đầu tư vô cùng mãn nhãn."
            },
            new Movie
            {
                Title = "Tây Du Ký Đại Náo (Dị bản Thái Lan)",
                Genre = "Hài hước, Hành động, Xuyên không",
                DurationMin = 110,
                Director = "Poj Arnon",
                ReleaseDate = new DateOnly(2026, 6, 1),
                Status = "now_showing",
                Description = "\"Ông hoàng phòng vé\" Thái Lan tái định nghĩa tác phẩm kinh điển. Tôn Ngộ Không bị quạt Ba Tiêu thổi bay... xuyên không thẳng xuống một bãi rác tại Bangkok thời hiện đại. Những màn tung hứng \"nhây - bựa - lầy\" đặc sản Thái Lan giữa Ngộ Không và Đường Tăng sẽ khiến bạn cười ra nước mắt."
            },
            new Movie
            {
                Title = "Mạc Ly",
                Genre = "Cổ trang, Tình cảm, Quyền mưu",
                DurationMin = 45,
                Director = "Cục chính kịch Tencent",
                ReleaseDate = new DateOnly(2026, 6, 9),
                Status = "now_showing",
                Description = "Được chuyển thể từ tiểu thuyết Thịnh Thế Đích Phi, bộ phim đánh dấu màn hợp tác bùng nổ phản ứng hóa học giữa Bạch Lộc và Thừa Lỗi. Câu chuyện xoay quanh Diệp Ly - trưởng nữ mang mối thù gia tộc liên thủ cùng Định Vương Mặc Tu Nghiêu phá tan các âm mưu triều chính."
            },
            new Movie
            {
                Title = "Đếm Ngày Xa Mẹ",
                Genre = "Tâm lý, Gia đình, Tình cảm",
                DurationMin = 105,
                Director = "Lý Hải",
                ReleaseDate = new DateOnly(2026, 6, 12),
                Status = "coming_soon",
                Description = "Câu chuyện cảm động lấy đi nước mắt của khán giả về hành trình của người con đi làm xa xứ, đếm ngược từng ngày để trở về bên mẹ. Phim khai thác sâu sắc tình mẫu tử, sự hy sinh thầm lặng của người phụ nữ Việt Nam và những tiếc nuối của người trẻ trong guồng quay cơm áo gạo tiền."
            },
            new Movie
            {
                Title = "Song Hỷ Lâm Nguy",
                Genre = "Hài hước, Tình cảm, Gia đình",
                DurationMin = 95,
                Director = "Nhất Trung",
                ReleaseDate = new DateOnly(2026, 6, 2),
                Status = "now_showing",
                Description = "Chuyện phim xoay quanh một đám cưới miệt vườn miền Tây \"bất ổn\" nhất màn ảnh khi hai gia đình thông gia vốn là oan gia ngõ hẹm. Hàng loạt tình huống dở khóc dở cười diễn ra ngay trong ngày rước dâu khiến ngày \"Song hỷ\" suýt biến thành \"Lâm nguy\"."
            },
            new Movie
            {
                Title = "Kỳ Tích Đường Chạy (The Golden Spike)",
                Genre = "Thể thao, Truyền cảm hứng, Tâm lý",
                DurationMin = 120,
                Director = "Nguyễn Quang Dũng",
                ReleaseDate = new DateOnly(2026, 6, 26),
                Status = "coming_soon",
                Description = "Bộ phim lấy cảm hứng từ câu chuyện có thật của các vận động viên điền kinh Việt Nam vượt qua chấn thương và hoàn cảnh khó khăn để chạm tay vào tấm huy chương vàng quốc tế. Phim mang lại năng lượng tích cực, sự kiên trì và tinh thần đồng đội cao cả."
            },
            new Movie
            {
                Title = "Bí Mật Đảo Giấu Vàng (Treasure Island: The New Horizon)",
                Genre = "Phiêu lưu, Hành động, Giả tưởng",
                DurationMin = 135,
                Director = "James Cameron (Sản xuất)",
                ReleaseDate = new DateOnly(2026, 5, 29),
                Status = "now_showing",
                Description = "Bom tấn kỹ xảo của mùa hè năm nay. Phim đưa người xem vào cuộc truy tìm kho báu nghẹt thở giữa đại dương của một nhóm bạn trẻ tình cờ tìm thấy bản đồ cổ. Những màn rượt đuổi, giải mã mật thư và kỹ xảo đại dương 3D cực kỳ mãn nhãn."
            },
            new Movie
            {
                Title = "Gió Thổi Bán Hạ",
                Genre = "Chính kịch, Lập nghiệp, Tâm lý",
                DurationMin = 45,
                Director = "Phó Đông Dục",
                ReleaseDate = new DateOnly(2026, 6, 8),
                Status = "now_showing",
                Description = "Nối tiếp câu chuyện lập nghiệp đầy mạnh mẽ của những doanh nhân ngành thép. Phim tập trung vào những cuộc đấu trí thương trường khốc liệt, tình anh em đồng cam cộng khổ và bản lĩnh của người phụ nữ trong ngành công nghiệp nặng. Triệu Lệ Dĩnh tiếp tục khiến khán giả nể phục vì diễn xuất xuất thần."
            },
            new Movie
            {
                Title = "Thanh Xuân Không Quay Đầu",
                Genre = "Học đường, Lãng mạn, Thanh xuân",
                DurationMin = 110,
                Director = "Vũ Ngọc Đãng",
                ReleaseDate = new DateOnly(2026, 6, 15),
                Status = "coming_soon",
                Description = "Một bản tình ca nhẹ nhàng, trong trẻo về những năm tháng cấp 3 tinh nghịch. Phim là những lời tỏ tình còn dang dở, những áp lực thi cử và lời hứa cùng nhau trưởng thành của nhóm bạn thân dưới mái trường hoa phượng vĩ."
            }
        };

        foreach (var movie in extraMovies)
        {
            if (!db.Movies.Any(m => m.Title == movie.Title))
            {
                movie.CreatedAt = DateTime.UtcNow;
                db.Movies.Add(movie);
            }
        }
        db.SaveChanges();
    }
}
