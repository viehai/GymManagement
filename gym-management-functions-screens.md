# Danh sách Function & Screen theo Role — Hệ thống Quản lý Phòng Gym

## Quy ước
- Mỗi function được đánh mã theo role: `ADM-xx` (Admin), `OWN-xx` (Owner), `MEM-xx` (Member), `GUE-xx` (Guest)
- Mỗi function liệt kê: **Screen liên quan**, **Mô tả**, **Trạng thái thực hiện**

---

## A. GUEST (chưa đăng nhập)

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| GUE-01 | Xem trang chủ | `Home/Index` | Banner giới thiệu, danh sách gym nổi bật | ✅ Đã làm |
| GUE-02 | Tìm kiếm / lọc gym | `Gym/Search` | Lọc theo khu vực, khoảng giá, tên gym | ✅ Đã làm |
| GUE-03 | Xem chi tiết gym | `Gym/Details/{id}` | Ảnh, mô tả, địa chỉ, danh sách equipment, danh sách package + giá | ✅ Đã làm |
| GUE-04 | Đăng ký tài khoản | `Account/Register` | Form nhập Email, Password, FullName, Phone | ✅ Đã làm |
| GUE-05 | Đăng nhập | `Account/Login` | Form Email + Password (điều hướng theo Role) | ✅ Đã làm |
| GUE-06 | Quên mật khẩu — nhập email | `Account/ForgotPassword` | Nhập email để nhận OTP | ✅ Đã làm |
| GUE-07 | Quên mật khẩu — nhập OTP | `Account/VerifyOtp` | Nhập 6 số OTP | ✅ Đã làm |
| GUE-08 | Đặt lại mật khẩu | `Account/ResetPassword` | Nhập password mới sau khi OTP hợp lệ | ✅ Đã làm |
| GUE-09 | Xác nhận email | `Account/ConfirmEmail` | Xử lý khi click link trong mail đăng ký | ✅ Đã làm |

> Guest xem được **mọi thông tin công khai** (gym, equipment, giá package) nhưng khi bấm "Mua vé / Đăng ký gói" sẽ bị chặn bởi `[Authorize]` và redirect sang `Account/Login`.

---

## B. MEMBER (hội viên — sau khi đăng nhập)

### Nhóm 1: Tài khoản cá nhân

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| MEM-01 | Xem/sửa hồ sơ cá nhân | `Member/Profile` | Trang cá nhân, nút Đăng ký mở phòng Gym | ✅ Đã làm |
| MEM-02 | Đổi mật khẩu | `Member/ChangePassword` | Nhập password cũ + mới (khi đã đăng nhập) | ✅ Đã làm |
| MEM-03 | Đăng xuất | — (action) | Xóa cookie session | ✅ Đã làm |

### Nhóm 2: Tìm & xem gym

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| MEM-04 | Tìm kiếm / lọc gym | `Gym/Search` | Giống Guest nhưng thêm gợi ý gym đã tập | ✅ Đã làm |
| MEM-05 | Xem chi tiết gym | `Gym/Details/{id}` | Giống Guest, thêm nút "Mua vé ngày" / "Đăng ký gói" | ✅ Đã làm |

### Nhóm 3: Mua vé & thanh toán

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| MEM-06 | Mua vé ngày | `Purchase/DailyPass/{gymId}` | Xác nhận gym, giá, bấm thanh toán | ✅ Đã làm |
| MEM-07 | Chọn gói tháng | `Purchase/Package/{gymId}` | Danh sách package của gym kèm giá | ✅ Đã làm |
| MEM-08 | Xác nhận đơn hàng | `Purchase/Checkout` | Tóm tắt đơn hàng trước thanh toán | ✅ Đã làm |
| MEM-09 | Thanh toán qua VNPay | Redirect `VNPay Sandbox` | Chuyển hướng thanh toán | ✅ Đã làm (Mock) |
| MEM-10 | Kết quả thanh toán | `Purchase/Result` | Hiện thành công/thất bại sau callback | ✅ Đã làm |
| MEM-11 | Lịch sử giao dịch | `Member/TransactionHistory` | Danh sách các lần mua, trạng thái, số tiền | ✅ Đã làm |
| MEM-12 | Xem & tải hóa đơn | `Member/InvoiceDetails/{id}` | Chi tiết hóa đơn, in qua browser | ✅ Đã làm |

### Nhóm 4: Theo dõi vé / hội viên

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| MEM-13 | Xem vé đang active | `Member/MyMemberships` | Danh sách gym đang có vé còn hạn, ngày hết hạn | ✅ Đã làm |
| MEM-14 | Xem chi tiết 1 vé | `Member/MembershipDetails/{id}` | Gym, package, StartDate, EndDate | ✅ Đã làm |
| MEM-15 | Gia hạn vé | `Purchase/Renew/{membershipId}` | Chọn gói mới, áp rule cộng dồn ngày | ✅ Đã làm |
| MEM-16 | Thông báo sắp hết hạn | Banner / Badge Cảnh báo | Cảnh báo khi vé còn ≤ 3 ngày | ✅ Đã làm |

---

## C. OWNER (chủ phòng gym)

### Nhóm 1: Quản lý hồ sơ Gym

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| OWN-01 | Tạo gym mới | `OwnerGym/Create` | Nhập Name, Address, Description, upload ảnh | ✅ Đã làm |
| OWN-02 | Danh sách gym của tôi | `OwnerGym/Index` | Danh sách các Gym thuộc quyền sở hữu của Owner | ✅ Đã làm |
| OWN-03 | Sửa thông tin gym | `OwnerGym/Edit/{id}` | Cập nhật mô tả, ảnh, địa chỉ | ✅ Đã làm |
| OWN-04 | Xóa gym | `OwnerGym/Delete/{id}` | Xóa phòng Gym | ✅ Đã làm |
| OWN-05 | Xem trạng thái duyệt | `OwnerGym/Index` (badge) | Pending / Approved / Rejected bởi Admin | ✅ Đã làm |

### Nhóm 2: Quản lý Equipment

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| OWN-06 | Xem danh mục catalog | `OwnerEquipment/Catalog` | Danh sách máy tập gốc do Admin quản lý | ✅ Đã làm |
| OWN-07 | Bật/tắt equipment | Action `OwnerEquipment` | Toggle `IsVisible` cho `GymEquipment` | ✅ Đã làm |
| OWN-08 | Thêm equipment custom | `OwnerEquipment/CreateCustom` | Nhập CustomName, upload CustomImage | ✅ Đã làm |
| OWN-09 | Sửa/xóa equipment custom | `OwnerEquipment/EditCustom/{id}` | Quản lý thiết bị tự thêm | ✅ Đã làm |
| OWN-10 | Danh sách equipment của gym | `OwnerEquipment/Index/{gymId}` | Tổng hợp catalog + custom | ✅ Đã làm |

### Nhóm 3: Quản lý gói vé (Package)

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| OWN-11 | Danh sách package | `OwnerPackage/Index/{gymId}` | Liệt kê Daily Pass + các gói tháng (có lọc theo Gym) | ✅ Đã làm |
| OWN-12 | Tạo package mới | `OwnerPackage/Create` | Loại Daily/Monthly, nhập giá tự động format phẩy | ✅ Đã làm |
| OWN-13 | Sửa giá/thông tin package | `OwnerPackage/Edit/{id}` | Cập nhật thông tin gói dịch vụ | ✅ Đã làm |
| OWN-14 | Bật/tắt (Ẩn) package | `OwnerPackage/ToggleActive` | Kích hoạt hoặc tạm dừng bán gói | ✅ Đã làm |

### Nhóm 4: Quản lý hội viên & doanh thu

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| OWN-15 | Danh sách hội viên | `OwnerMember/Index/{gymId}` | Tên, gói đang dùng, ngày hết hạn | ✅ Đã làm |
| OWN-16 | Chi tiết 1 hội viên | `OwnerMember/Details/{memberId}` | Lịch sử mua vé tại gym này | ✅ Đã làm |
| OWN-17 | Danh sách giao dịch | `OwnerTransaction/Index/{gymId}` | Lọc theo ngày, trạng thái thanh toán | ✅ Đã làm |
| OWN-18 | Báo cáo doanh thu | `OwnerDashboard/Revenue/{gymId}` | Biểu đồ doanh thu, số lượng hội viên active | ✅ Đã làm |

### Nhóm 5: Tài khoản cá nhân

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| OWN-19 | Xem/sửa hồ sơ | `Member/Profile` | Dùng chung với Member | ✅ Đã làm |
| OWN-20 | Đổi mật khẩu | `Member/ChangePassword` | Dùng chung với Member | ✅ Đã làm |

---

## D. ADMIN (quản trị nền tảng)

### Nhóm 1: Quản lý Owner & Gym

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| ADM-01 | Danh sách Owner đăng ký | `AdminUser/Index?filter=owner` | Lọc theo trạng thái tài khoản | ✅ Đã làm |
| ADM-02 | Duyệt / khóa tài khoản Owner | `AdminUser/Index` (Action) | Approve/Reject/Lock | ✅ Đã làm |
| ADM-03 | Danh sách toàn bộ Gym | `Admin/AllGyms` | Lọc theo trạng thái (Pending, Approved, Rejected...) | ✅ Đã làm |
| ADM-04 | Duyệt gym mới tạo | `Admin/PendingGyms` | Approve (nâng role Owner + gửi mail) / Reject (kèm lý do) | ✅ Đã làm |
| ADM-05 | Khóa/gỡ 1 gym vi phạm | `AdminGym/Suspend/{id}` | Đình chỉ / Mở lại Gym vi phạm | ✅ Đã làm |

### Nhóm 2: Quản lý danh mục Equipment gốc

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| ADM-06 | Danh sách equipment catalog | `AdminEquipment/Index` | Toàn bộ máy tập gốc | ✅ Đã làm |
| ADM-07 | Thêm equipment catalog | `AdminEquipment/Create` | Name, Description, ImageUrl, Category | ✅ Đã làm |
| ADM-08 | Sửa equipment catalog | `AdminEquipment/Edit/{id}` | Cập nhật thông tin máy tập | ✅ Đã làm |
| ADM-09 | Xóa equipment catalog | `AdminEquipment/Delete/{id}` | Kiểm tra ràng buộc trước khi xóa | ✅ Đã làm |

### Nhóm 3: Quản lý người dùng chung

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| ADM-10 | Danh sách tất cả User | `AdminUser/Index` | Lọc theo Role, trạng thái | ✅ Đã làm |
| ADM-11 | Khóa/mở khóa tài khoản | `AdminUser/ToggleLock` | Ban/Unban tài khoản bằng LockoutEnd | ✅ Đã làm |
| ADM-12 | Phân quyền / gán Role | `AdminUser/EditRole/{id}` | Chỉnh role thủ công (Admin/Owner/Member) | ✅ Đã làm |

### Nhóm 4: Giám sát hệ thống

| Mã | Function | Screen | Mô tả | Trạng thái |
|---|---|---|---|---|
| ADM-13 | Xem nhật ký (System Log) | `AdminLog/Index` | Lọc theo User, Action, Level | ⏳ Chưa làm |
| ADM-14 | Xem chi tiết 1 log entry | `AdminLog/Details/{id}` | Description đầy đủ, Entity liên quan | ⏳ Chưa làm |
| ADM-15 | Xem thống kê tổng quan | `Admin/Dashboard` | Thống kê số lượng Gym, Pending, Approved, User | ✅ Đã làm |
| ADM-16 | Danh sách giao dịch toàn hệ thống | `AdminTransaction/Index` | Lọc theo gym, trạng thái thanh toán | ⏳ Chưa làm |

---

## 📈 BÁO CÁO TIẾN ĐỘ THỰC HIỆN

| Role | Tổng số Function | Đã hoàn thành | Chưa thực hiện | Tỷ lệ hoàn thành |
|---|---|---|---|---|
| **Guest** | 9 | 9 | 0 | **100.0%** |
| **Member** | 16 | 16 | 0 | **100.0%** 🚀 |
| **Owner** | 20 | 20 | 0 | **100.0%** 🚀 |
| **Admin** | 16 | 13 | 3 | **81.3%** |
| **TỔNG CỘNG** | **61** | **58** | **3** | **95.1%** |

> Lưu ý: Các function không có screen riêng (như MEM-03 Đăng xuất) vẫn được tính vào danh sách phân tích chức năng nghiệp vụ.
