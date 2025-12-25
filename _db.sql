CREATE DATABASE QuanLyNhanSu;
GO

USE QuanLyNhanSu;
GO

/* ========================= 1. PHÒNG BAN ========================= */
CREATE TABLE PhongBan (
    maPB INT IDENTITY(1,1) PRIMARY KEY,      -- Mã phòng ban (tự tăng)
    tenPB NVARCHAR(100) NOT NULL             -- Tên phòng ban (VD: Nhân sự, Kế toán)
);
GO

/* ========================= 2. NHÂN VIÊN ========================= */
CREATE TABLE NhanVien (
    maNV INT IDENTITY(1,1) PRIMARY KEY,      -- Mã nhân viên
    hoTen NVARCHAR(100) NOT NULL,             -- Họ tên đầy đủ
    ngaySinh DATE NULL,                       -- Ngày sinh
    gioiTinh BIT NULL,                        -- Giới tính (1=Nam, 0=Nữ)
    diaChi NVARCHAR(255) NULL,                -- Địa chỉ liên hệ
    dienThoai VARCHAR(15) NULL,               -- Số điện thoại
    email VARCHAR(100) NULL,                  -- Email
    chucVu NVARCHAR(100) NOT NULL,            -- Chức vụ (VD: Nhân viên, Trưởng phòng, Kế toán)
    trangThaiLamViec NVARCHAR(50) DEFAULT N'Đang làm việc', -- Trạng thái làm việc
    ngayVaoLam DATE NULL,                     -- Ngày vào làm
    ngayKetThucLam DATE NULL,                 -- Ngày nghỉ việc (nếu có)
    maPB INT NULL,                            -- Phòng ban hiện tại

    CONSTRAINT FK_NhanVien_PhongBan 
        FOREIGN KEY (maPB) 
        REFERENCES PhongBan(maPB)
        ON DELETE SET NULL,                   -- Xóa phòng ban thì NV không bị xóa

    CONSTRAINT CK_NhanVien_Ngay 
        CHECK (ngayKetThucLam IS NULL 
               OR ngayKetThucLam >= ngayVaoLam) -- Ngày nghỉ ≥ ngày vào
);
GO

/* ========================= 3. HỢP ĐỒNG ========================= */
CREATE TABLE HopDong (
    maHD INT IDENTITY(1,1) PRIMARY KEY,       -- Mã hợp đồng
    maNV INT NOT NULL,                        -- Nhân viên ký hợp đồng
    loaiHD TINYINT NOT NULL,                  -- 1=Thử việc, 2=Chính thức, 3=Thời vụ
    heSoLuong DECIMAL(5,2) NOT NULL,          -- Hệ số lương
    luongCoBan DECIMAL(15,2) NOT NULL,        -- Lương cơ bản
    ngayBatDau DATE NOT NULL,                 -- Ngày bắt đầu hợp đồng
    ngayKetThuc DATE NULL,                    -- Ngày kết thúc (NULL = vô thời hạn)
    trangThai BIT DEFAULT 1 NOT NULL,         -- 1=Hiệu lực, 0=Hết hiệu lực
    CONSTRAINT FK_HopDong_NhanVien 
        FOREIGN KEY (maNV) REFERENCES NhanVien(maNV),

    CONSTRAINT CK_HopDong_Ngay 
        CHECK (ngayKetThuc IS NULL 
               OR ngayKetThuc >= ngayBatDau)
);
GO

/* ========================= 4. TÀI KHOẢN (1–1 NHÂN VIÊN) ========================= */
CREATE TABLE TaiKhoan (
    maNV INT PRIMARY KEY,                     -- Trùng mã nhân viên
    tenTK VARCHAR(50) UNIQUE NOT NULL,        -- Tên đăng nhập
    matKhau VARCHAR(255) NOT NULL,            -- Mật khẩu đã hash
    vaiTro NVARCHAR(50) NOT NULL,             -- Admin / Nhân sự / Nhân viên

    CONSTRAINT FK_TaiKhoan_NhanVien 
        FOREIGN KEY (maNV) 
        REFERENCES NhanVien(maNV)
        ON DELETE CASCADE                     -- Xóa NV → xóa tài khoản
);
GO

/* ========================= 5. CHẤM CÔNG (THEO NGÀY) ========================= */
CREATE TABLE ChamCong (
    maChamCong INT IDENTITY(1,1) PRIMARY KEY, -- Mã chấm công
    maNV INT NOT NULL,                        -- Nhân viên
    ngayCham DATE NOT NULL,                   -- Ngày chấm công

    CONSTRAINT FK_ChamCong_NhanVien 
        FOREIGN KEY (maNV) REFERENCES NhanVien(maNV),

    CONSTRAINT UK_ChamCong UNIQUE (maNV, ngayCham) -- Không chấm công trùng ngày
);
GO

/* ========================= 6. BẢNG LƯƠNG THÁNG ========================= */
CREATE TABLE BangLuongThang (
    maBangLuongThang INT IDENTITY(1,1) PRIMARY KEY, -- Mã bảng lương
    thang INT NOT NULL CHECK (thang BETWEEN 1 AND 12), -- Tháng (1-12)
    nam INT NOT NULL CHECK (nam >= 2000), -- Năm
    ngayTao DATE DEFAULT GETDATE(),            -- Ngày tạo bảng lương
    nguoiTao NVARCHAR(100) NULL,                    -- Người tạo
    trangThai BIT DEFAULT 0 NOT NULL, -- Trạng thái: 1=Đã thanh toán / 0=Chưa thanh toán
    ghiChu NVARCHAR(500) NULL,                      -- Ghi chú

    CONSTRAINT UK_BangLuongThang UNIQUE (thang, nam)
);
GO

/* ========================= 7. CHI TIẾT BẢNG LƯƠNG ========================= */
CREATE TABLE ChiTietBangLuong (
    maChiTiet INT IDENTITY(1,1) PRIMARY KEY,  -- Mã chi tiết
    maBangLuongThang INT NOT NULL,             -- Thuộc bảng lương nào
    maNV INT NOT NULL,                         -- Nhân viên
    luongThucNhan DECIMAL(15,2) NOT NULL,     -- Lương thực nhận

    CONSTRAINT FK_ChiTiet_BangLuongThang 
        FOREIGN KEY (maBangLuongThang)
        REFERENCES BangLuongThang(maBangLuongThang)
        ON DELETE CASCADE,

    CONSTRAINT FK_ChiTiet_NhanVien 
        FOREIGN KEY (maNV) REFERENCES NhanVien(maNV),

    CONSTRAINT UK_ChiTiet UNIQUE (maBangLuongThang, maNV)
);
GO

    /* ========================= DỮ LIỆU MẪU ========================= */
    INSERT INTO PhongBan (tenPB) VALUES
        (N'Ban Giám đốc'),
        (N'Nhân sự'),
        (N'Kế toán'),
        (N'Chăm sóc khách hàng'),
        (N'Truyền thông'),
        (N'Công nghệ thông tin'),
        (N'Kinh doanh'),
        (N'Sản xuất'),
        (N'Logistics'),
        (N'Nghiên cứu & phát triển');

    INSERT INTO NhanVien (hoTen, ngaySinh, gioiTinh, diaChi, dienThoai, email, chucVu, ngayVaoLam, maPB) VALUES
        (N'Huỳnh Minh Tâm', '1982-05-14', 1, N'Quận 1, TP.HCM', '0905123456', 'tam.huynh@example.com', N'Tổng giám đốc', '2010-01-05', 1),
        (N'Phạm Ngọc Lan', '1990-02-21', 0, N'Quận 3, TP.HCM', '0905345678', 'lan.pham@example.com', N'Giám đốc nhân sự', '2012-04-12', 2),
        (N'Nguyễn Thanh Long', '1991-11-09', 1, N'Quận Bình Thạnh, TP.HCM', '0912123456', 'long.nguyen@example.com', N'Chuyên viên nhân sự', '2016-09-01', 2),
        (N'Lê Tú Anh', '1994-06-30', 0, N'Quận 7, TP.HCM', '0913123456', 'tuanh.le@example.com', N'Kế toán trưởng', '2013-07-18', 3),
        (N'Võ Đức Minh', '1993-12-11', 1, N'Quận Thủ Đức, TP.HCM', '0909345678', 'minh.vo@example.com', N'Chuyên viên kế toán', '2018-11-05', 3),
        (N'Bùi Thanh Hương', '1996-03-08', 0, N'Quận 5, TP.HCM', '0908567890', 'huong.bui@example.com', N'Chuyên viên CSKH', '2019-02-15', 4),
        (N'Trần Quang Huy', '1995-12-25', 1, N'Quận 11, TP.HCM', '0919345678', 'huy.tran@example.com', N'Chuyên viên truyền thông', '2020-05-20', 5),
        (N'Đặng Thị Phương', '1998-01-19', 0, N'Quận Gò Vấp, TP.HCM', '0909123456', 'phuong.dang@example.com', N'Lập trình viên', '2021-08-02', 6),
        (N'Hồ Văn Dũng', '1992-04-02', 1, N'Quận 10, TP.HCM', '0909765432', 'dung.ho@example.com', N'Sales executive', '2023-03-06', 7),
        (N'Lý Thị Ngọc', '1997-09-14', 0, N'TP.Thủ Dầu Một, Bình Dương', '0918456789', 'ngoc.ly@example.com', N'Chuyên viên sản xuất', '2024-01-15', 8);

    INSERT INTO HopDong (maNV, loaiHD, heSoLuong, luongCoBan, ngayBatDau, trangThai) VALUES
        (1, 2, 5.00, 25000000, '2010-01-05', 1),
        (2, 2, 4.20, 22000000, '2012-04-12', 1),
        (3, 1, 3.00, 15000000, '2016-09-01', 1),
        (4, 2, 3.20, 16000000, '2013-07-18', 1),
        (5, 2, 2.90, 14500000, '2018-11-05', 1),
        (6, 1, 2.70, 13500000, '2019-02-15', 1),
        (7, 2, 3.10, 15500000, '2020-05-20', 1),
        (8, 2, 3.40, 16500000, '2021-08-02', 1),
        (9, 2, 3.00, 15000000, '2023-03-06', 1),
        (10, 3, 2.60, 13000000, '2024-01-15', 1);

    INSERT INTO TaiKhoan (maNV, tenTK, matKhau, vaiTro) VALUES
        (1, 'admin', 'hashed-admin-100', N'Admin'),
        (2, 'nhansu', 'hashed-nvs-100', N'Nhân sự'),
        (3, 'nv.tuyendung02', 'hashed-nv-101', N'Nhân viên'),
        (4, 'nv.kehoach', 'hashed-nv-102', N'Nhân viên'),
        (5, 'nv.kehoach02', 'hashed-nv-103', N'Nhân viên'),
        (6, 'nv.cskh', 'hashed-nv-104', N'Nhân viên'),
        (7, 'nv.truyenthong', 'hashed-nv-105', N'Nhân viên'),
        (8, 'nv.it', 'hashed-nv-106', N'Nhân viên'),
        (9, 'nv.sales', 'hashed-nv-107', N'Nhân viên'),
        (10, 'nv.sanxuat', 'hashed-nv-108', N'Nhân viên');

    INSERT INTO ChamCong (maNV, ngayCham) VALUES
        (1, '2025-12-01'),
        (2, '2025-12-01'),
        (3, '2025-12-01'),
        (4, '2025-12-01'),
        (5, '2025-12-02'),
        (6, '2025-12-02'),
        (7, '2025-12-02'),
        (8, '2025-12-03'),
        (9, '2025-12-03'),
        (10, '2025-12-03');

    INSERT INTO BangLuongThang (thang, nam, nguoiTao, trangThai, ghiChu) VALUES
        (1, 2025, N'Huỳnh Minh Tâm', 1, N'Thanh toán trước Tết'),
        (2, 2025, N'Phạm Ngọc Lan', 1, N'Tháng 2'),
        (3, 2025, N'Phạm Ngọc Lan', 1, N'Tháng 3'),
        (4, 2025, N'Lê Tú Anh', 1, N'Tháng 4'),
        (5, 2025, N'Lê Tú Anh', 1, N'Tháng 5'),
        (6, 2025, N'Lê Tú Anh', 0, N'Tháng 6'),
        (7, 2025, N'Support Team', 0, N'Tháng 7'),
        (8, 2025, N'Support Team', 0, N'Tháng 8'),
        (9, 2025, N'Support Team', 0, N'Tháng 9'),
        (10, 2025, N'Support Team', 0, N'Tháng 10');

	select * from BangLuongThang
	select * from ChiTietBangLuong

    INSERT INTO ChiTietBangLuong (maBangLuongThang, maNV, luongThucNhan) VALUES
        (1, 1, 32000000),
        (2, 2, 28000000),
        (3, 3, 16000000),
        (4, 4, 19000000),
        (5, 5, 17000000),
        (6, 6, 15000000),
        (7, 7, 18000000),
        (8, 8, 19500000),
        (9, 9, 17000000),
        (10, 10, 15000000);

PRINT N'Tạo database QuanLyNhanSu thành công!';
GO


SELECT 
    nv.maNV,
    nv.hoTen,
    nv.ngaySinh,
    CASE WHEN nv.gioiTinh = 1 THEN N'Nam' ELSE N'Nữ' END AS gioiTinh,
    nv.diaChi,
    nv.dienThoai,
    nv.email,
    nv.chucVu,
    nv.trangThaiLamViec,
    nv.ngayVaoLam,
    nv.ngayKetThucLam,
    pb.tenPB AS phongBan,

    -- Hợp đồng hiện tại (đang hiệu lực)
    hd.loaiHD,
    CASE hd.loaiHD 
        WHEN 1 THEN N'Thử việc'
        WHEN 2 THEN N'Chính thức'
        WHEN 3 THEN N'Thời vụ'
        ELSE N'Không xác định'
    END AS tenLoaiHD,
    hd.heSoLuong,
    hd.luongCoBan,
    hd.ngayBatDau AS ngayBatDauHD,
    hd.ngayKetThuc AS ngayKetThucHD,

    -- Tài khoản
    tk.tenTK,
    tk.vaiTro,

    -- Danh sách ngày chấm công (mảng JSON)
    (
        SELECT cc.ngayCham
        FROM ChamCong cc
        WHERE cc.maNV = nv.maNV
        ORDER BY cc.ngayCham
        FOR JSON PATH
    ) AS danhSachChamCong,

    -- Danh sách lương theo tháng (mảng JSON)
    (
        SELECT 
            blt.thang,
            blt.nam,
            ct.luongThucNhan,
            CASE WHEN blt.trangThai = 1 THEN N'Đã thanh toán' ELSE N'Chưa thanh toán' END AS trangThaiThanhToan,
            blt.ghiChu
        FROM ChiTietBangLuong ct
        JOIN BangLuongThang blt ON ct.maBangLuongThang = blt.maBangLuongThang
        WHERE ct.maNV = nv.maNV
        ORDER BY blt.nam, blt.thang
        FOR JSON PATH
    ) AS lichSuLuong

FROM NhanVien nv
LEFT JOIN PhongBan pb ON nv.maPB = pb.maPB
LEFT JOIN HopDong hd ON nv.maNV = hd.maNV AND hd.trangThai = 1  -- Chỉ lấy hợp đồng hiệu lực
LEFT JOIN TaiKhoan tk ON nv.maNV = tk.maNV
ORDER BY nv.maNV;