# 🚀 GymPro V2 — Đề Xuất Nâng Cấp Hệ Thống Quản Lý Phòng Gym

> **Baseline**: V1 đã hoàn thành 61/61 functions (100%) — ASP.NET Core MVC thuần, Identity, VietQR + SePay Webhook  
> **Mục tiêu V2**: Nâng tầm trải nghiệm, tăng giá trị vận hành cho Owner, và xây dựng hệ sinh thái loyalty cho Member

---

## 📋 TỔNG QUAN CÁC MODULE NÂNG CẤP

| # | Module | Mức độ | Mô tả ngắn | Trạng thái |
|---|--------|--------|-------------|------------|
| 1 | 🖼️ Multi-Image Gallery | ⭐⭐ | Owner upload nhiều ảnh cho Gym, kéo thả sắp xếp, gallery carousel | ✅ **Hoàn thành** |
| 2 | 🚫 Đình Chỉ Hội Viên | ⭐⭐ | Owner tạm đình chỉ / cấm vĩnh viễn Member vi phạm tại gym | ✅ **Hoàn thành** |
| 3 | 👑 VIP Loyalty System | ⭐⭐⭐ | Hệ thống thăng hạng tự động dựa trên số lần mua vé, Owner tùy chỉnh ngưỡng | ✅ **Hoàn thành** |
| 4 | 📱 QR Check-in Thông Minh | ⭐⭐⭐ | Mỗi Member có mã QR riêng, Owner quét để xem đầy đủ thông tin hội viên; Thước đo độ đông đúc realtime | ✅ **Hoàn thành** |
| 5 | ⭐ Rating & Review | ⭐⭐ | Member đánh giá sao + viết review cho Gym đã tập, bộ lọc từ cấm | ✅ **Hoàn thành** |
| 6 | 🔔 Notification Center | ⭐⭐ | Trung tâm thông báo real-time (sắp hết hạn, đình chỉ, khuyến mãi...) | ✅ **Hoàn thành** |
| 7 | 📊 Nâng cấp Dashboard & Báo cáo | ⭐⭐ | Biểu đồ nâng cao, thống kê VIP, xuất báo cáo Excel/PDF | ⏸️ Chưa làm |

> ⭐ = Đơn giản &nbsp; ⭐⭐ = Trung bình &nbsp; ⭐⭐⭐ = Phức tạp

---

## MODULE 1: 🖼️ MULTI-IMAGE GALLERY — ✅ [ĐÃ HOÀN THÀNH]

### 1.1 Vấn đề V1
- Mỗi Gym chỉ có **1 ảnh đại diện duy nhất** (`ImageUrl` trên `Gym.cs`)
- Không đủ để thể hiện không gian, thiết bị, nội thất... → Member khó quyết định mua vé

### 1.2 Giải pháp V2 (Đã triển khai)

#### Database — Entity mới: `GymImage.cs` (Đã áp dụng Migration `AddGymImages`)

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `GymId` | int (FK → Gym) | Gym sở hữu |
| `ImageUrl` | string | Đường dẫn file ảnh |
| `DisplayOrder` | int | Thứ tự hiển thị (kéo thả) |
| `IsCover` | bool | Ảnh bìa đại diện chính |
| `UploadedAt` | DateTime | Thời điểm upload |

#### Functions đã hoàn thành

| Mã | Function | Screen | Trạng thái | Mô tả |
|----|----------|--------|------------|-------|
| OWN-21 | Upload nhiều ảnh Gym | `OwnerGym/Create` & `OwnerGym/Edit/{id}` | [x] Hoàn thành | Tích hợp trực tiếp vào trang Tạo/Sửa gym: upload tối đa **10 ảnh**, kéo thả/preview, tự động chọn ảnh bìa |
| OWN-22 | Xóa / thay đổi ảnh bìa | `OwnerGym/Edit/{id}` | [x] Hoàn thành | Xóa từng ảnh, nút ngôi sao đặt làm ảnh bìa, tự động đồng bộ `Gym.ImageUrl` mọi nơi |
| GUE-10 | Xem gallery ảnh Gym | `Gym/Details/{id}` & `OwnerGym/Details` | [x] Hoàn thành | Gallery carousel, thumbnail strip, lightbox xem ảnh lớn, tự động fallback V1 |

#### Ghi chú kỹ thuật
- Upload file lưu vào `wwwroot/uploads/gyms/{gymId}/`
- Validate: max 10 ảnh, mỗi ảnh ≤ 5MB, chấp nhận `.jpg/.png/.webp`
- Ảnh bìa (`IsCover = true`) hiển thị ở Search results & card Gym

---

## MODULE 2: 🚫 ĐÌNH CHỈ HỘI VIÊN (MEMBER SUSPENSION) — ✅ [ĐÃ HOÀN THÀNH]

### 2.1 Vấn đề V1
- Owner chỉ có thể **xem** danh sách hội viên, KHÔNG có quyền xử lý vi phạm
- Nếu hội viên gây rối / vi phạm nội quy, Owner phải nhờ Admin can thiệp → chậm trễ

### 2.2 Giải pháp V2 (Đã triển khai)

#### Database — Entity mới: `MemberSuspension.cs` (Đã áp dụng Migration `AddMemberSuspensions`)

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `GymId` | int (FK → Gym) | Gym thực hiện đình chỉ |
| `MemberId` | string (FK → ApplicationUser) | Hội viên bị đình chỉ |
| `SuspendedByUserId` | string (FK → ApplicationUser) | Owner/Admin thực hiện |
| `Reason` | string | Lý do đình chỉ (bắt buộc nhập) |
| `SuspensionType` | enum | `Temporary` (tạm thời) / `Permanent` (vĩnh viễn) |
| `StartDate` | DateTime | Ngày bắt đầu đình chỉ |
| `EndDate` | DateTime? | Ngày kết thúc (null nếu Permanent) |
| `Status` | enum | `Active` / `Lifted` (đã gỡ) |
| `LiftedAt` | DateTime? | Thời điểm gỡ đình chỉ |
| `LiftedReason` | string? | Lý do gỡ đình chỉ |
| `CreatedAt` | DateTime | Thời gian tạo |

#### Functions đã hoàn thành

| Mã | Function | Screen | Trạng thái | Mô tả |
|----|----------|--------|------------|-------|
| OWN-23 | Đình chỉ hội viên | `OwnerMember/Suspend` | [x] Hoàn thành | Chọn hình thức kỷ luật (tạm thời/vĩnh viễn), thời hạn (7, 14, 30, 90 ngày hoặc custom), nhập lý do, tự động gửi email và ghi SystemLog |
| OWN-24 | Gỡ đình chỉ hội viên | `OwnerMember/LiftSuspension` | [x] Hoàn thành | Gỡ bỏ đình chỉ trước hạn ngay trên danh sách hoặc qua modal, nhập lý do gỡ, gửi email khôi phục & ghi SystemLog |
| OWN-25 | Xem danh sách đình chỉ | `OwnerMember/Suspensions` | [x] Hoàn thành | Quản lý toàn bộ danh sách kỷ luật theo Gym, tabs lọc Đang hiệu lực / Đã gỡ / Hết hạn, thao tác gỡ nhanh |
| MEM-17 | Xem trạng thái đình chỉ | `Member/MyMemberships` | [x] Hoàn thành | Banner cảnh báo nổi bật trên đầu trang, badge kỷ luật chi tiết trên từng thẻ gói tập, khóa nút "Gia hạn" khi bị đình chỉ |
| ADM-17 | Giám sát đình chỉ toàn hệ thống | `AdminSuspension/Index` | [x] Hoàn thành | Admin xem toàn bộ các quyết định đình chỉ trên sàn, thống kê nhanh, lọc theo gym/trạng thái/từ khóa, can thiệp gỡ bỏ |
| MEM-PUR | Chặn mua vé khi bị đình chỉ | `PurchaseController` | [x] Hoàn thành | Tự động kiểm tra và chặn toàn bộ các luồng DailyPass, Package, Checkout và Renew nếu hội viên có lệnh đình chỉ đang hiệu lực |

#### Business Rules đã bảo đảm
- **Tạm thời (`Temporary`)**: Hội viên bị chặn mua vé mới / gia hạn trong khoảng StartDate → EndDate, vé cũ vẫn trôi bình thường (không gia hạn bù ngày)
- **Vĩnh viễn (`Permanent`)**: Hội viên bị cấm tại gym đó, không thể mua vé mới tại gym đã cấm
- Khi Member bị đình chỉ → **gửi email thông báo** kèm lý do + thời hạn
- Khi gỡ đình chỉ → **gửi email thông báo** khôi phục
- Owner KHÔNG thể đình chỉ chính mình hoặc Admin
- **Ghi SystemLog** mỗi lần đình chỉ / gỡ đình chỉ

---

## MODULE 3: 👑 VIP LOYALTY SYSTEM — ✅ [ĐÃ HOÀN THÀNH]

### 3.1 Ý tưởng
- Hội viên mua vé đạt ngưỡng do Owner cài đặt → tự động được **thăng hạng VIP** tại gym đó
- VIP là **theo từng Gym** (1 Member có thể là VIP tại gym A nhưng thường ở gym B)
- Owner tự quyết ngưỡng, phần thưởng, và chính sách VIP

### 3.2 Giải pháp V2 (Đã triển khai)

#### Database — Entities mới (Đã áp dụng Migration `AddVipLoyaltySystem`)

**`VipTierSetting.cs`** — Owner cấu hình các mức VIP

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `GymId` | int (FK → Gym) | |
| `TierName` | string | Tên hạng VIP (VD: Silver, Gold, Platinum) |
| `MinPurchaseCount` | int | Số lần mua tối thiểu để đạt hạng |
| `DiscountPercent` | decimal? | % giảm giá cho vé tiếp theo (có thể null nếu không giảm) |
| `BenefitDescription` | string | Mô tả quyền lợi VIP dạng text |
| `BadgeColor` | string | Màu badge hiển thị (VD: `#C0C0C0`, `#FFD700`, `#E5E4E2`) |
| `DisplayOrder` | int | Thứ tự hạng (1 = thấp nhất) |
| `IsActive` | bool | Bật/tắt tier |

**`MemberVipStatus.cs`** — Trạng thái VIP hiện tại của Member tại mỗi Gym

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `MemberId` | string (FK → ApplicationUser) | |
| `GymId` | int (FK → Gym) | |
| `CurrentTierId` | int? (FK → VipTierSetting) | Hạng VIP hiện tại (null = chưa đạt) |
| `TotalPurchaseCount` | int | Tổng số lần mua vé thành công tại gym này |
| `AchievedAt` | DateTime? | Thời điểm đạt hạng hiện tại |
| `LastPurchaseAt` | DateTime? | Lần mua gần nhất |

#### Functions đã hoàn thành

| Mã | Function | Screen | Trạng thái | Mô tả |
|----|----------|--------|------------|-------|
| OWN-26 | Cấu hình VIP Tiers | `OwnerVip/Settings` | [x] Hoàn thành | Tạo/sửa/xóa các mức VIP, set ngưỡng mua, giảm giá, quyền lợi, khởi tạo mẫu 3 hạng Bạc/Vàng/Kim Cương |
| OWN-27 | Xem danh sách VIP Members | `OwnerVip/Members` | [x] Hoàn thành | Danh sách hội viên VIP, lọc theo gym và tier, tổng lượt mua, ngày đạt hạng |
| OWN-28 | Xem chi tiết VIP 1 hội viên | `OwnerMember/Details` | [x] Hoàn thành | Huy hiệu VIP cạnh tên, card tóm tắt hạng VIP, % giảm giá áp dụng, lịch sử mua |
| MEM-18 | Xem hạng VIP của tôi | `Member/MyVipStatus` | [x] Hoàn thành | Danh sách gym có VIP, hạng hiện tại, thanh tiến trình % và số lượt mua cần thêm để thăng hạng kế tiếp |
| MEM-19 | Xem quyền lợi VIP | `Member/VipBenefits/{gymId}` | [x] Hoàn thành | Bảng đặc quyền công khai theo từng cơ sở, danh sách mức chiết khấu và quyền lợi |
| PUR-VIP | Tự động giảm giá & thăng hạng | `PurchaseController` | [x] Hoàn thành | Tự động tính chiết khấu khi mua/gia hạn vé, thăng hạng tự động khi nhận webhook thanh toán, gửi email chúc mừng và ghi SystemLog |

#### Business Rules & Logic thăng hạng
```
Khi Transaction.Status chuyển thành "Success":
  1. Tăng MemberVipStatus.TotalPurchaseCount += 1
  2. Lấy danh sách VipTierSetting của Gym, sắp xếp theo MinPurchaseCount DESC
  3. Tìm tier cao nhất mà TotalPurchaseCount >= MinPurchaseCount
  4. Nếu tier mới > tier hiện tại:
     → Cập nhật CurrentTierId, AchievedAt = DateTime.Now
     → Gửi email chúc mừng thăng hạng
     → Ghi SystemLog
  5. Khi mua vé mới, nếu có DiscountPercent → tự động áp giảm giá
```

#### Ví dụ cấu hình mẫu

| Tier | MinPurchaseCount | Discount | Badge |
|------|-----------------|----------|-------|
| 🥈 Silver | 5 lần | 5% | Bạc |
| 🥇 Gold | 15 lần | 10% | Vàng |
| 💎 Platinum | 30 lần | 15% | Kim cương |

> Owner hoàn toàn tự do thiết lập: có thể tạo 1 tier hoặc 10 tiers, tùy chiến lược kinh doanh.

---

## MODULE 4: 📱 QR CHECK-IN THÔNG MINH — ✅ [ĐÃ HOÀN THÀNH]

### 4.1 Vấn đề V1
- Không có cơ chế **check-in** khi hội viên đến tập
- Owner không biết ai đang ở trong phòng, ai đã hết hạn
- Khách hàng không biết phòng tập đang đông hay vắng tại thời điểm muốn đi tập

### 4.2 Giải pháp V2 (Đã triển khai)

#### Luồng hoạt động

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        QR CHECK-IN FLOW                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  [Member mở app/web]                                                    │
│        │                                                                │
│        ▼                                                                │
│  [Xem mã QR cá nhân]  ◄── QR chứa: MemberId + Token bảo mật          │
│  (Member/MyQrCode)         (đổi token định kỳ để chống giả mạo)        │
│        │                                                                │
│        ▼                                                                │
│  [Owner quét QR bằng camera/điện thoại]                                 │
│  (OwnerCheckin/Scan/{gymId})                                            │
│        │                                                                │
│        ▼                                                                │
│  ┌─────────────────────────────────────────────────┐                    │
│  │         THÔNG TIN HIỂN THỊ SAU KHI QUÉT         │                    │
│  ├─────────────────────────────────────────────────┤                    │
│  │  👤 Họ tên: Nguyễn Văn A                       │                    │
│  │  📧 Email: nguyenvana@gmail.com                 │                    │
│  │  📞 SĐT: 0912 345 678                          │                    │
│  │  ─────────────────────────────────               │                    │
│  │  📦 Gói tập: Gói Tháng Premium                  │                    │
│  │  📅 Hạn đến: 15/09/2026                         │                    │
│  │  ⏳ Còn lại: 17 ngày                            │                    │
│  │  👑 Hạng VIP: Gold (Giảm 10%)                   │                    │
│  │  🚫 Đình chỉ: Không                             │                    │
│  │  ─────────────────────────────────               │                    │
│  │  ✅ TRẠNG THÁI: HỢP LỆ — Cho phép vào tập     │                    │
│  │         [BẤM CHECK-IN]                           │                    │
│  └─────────────────────────────────────────────────┘                    │
│        │                                                                │
│        ▼                                                                │
│  [Ghi nhận CheckinLog] → Thời gian, GymId, MemberId                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Database — Entities mới (Đã áp dụng Migration `AddCheckinSystemAndMaxCapacity`)

**`MemberQrToken.cs`** — Token bảo mật cho QR của Member

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `MemberId` | string (FK → ApplicationUser) | |
| `Token` | string | GUID/Hash token nhúng trong QR |
| `GeneratedAt` | DateTime | Thời điểm sinh token |
| `ExpiresAt` | DateTime | Hết hạn (VD: 24h sau khi sinh) |

**`CheckinLog.cs`** — Lịch sử check-in

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `MemberId` | string (FK → ApplicationUser) | |
| `GymId` | int (FK → Gym) | |
| `MembershipId` | int (FK → MemberMembership) | Vé nào được dùng |
| `CheckinTime` | DateTime | Thời điểm check-in |
| `CheckoutTime` | DateTime? | Thời điểm check-out (rời phòng) |
| `CheckedByUserId` | string (FK → ApplicationUser) | Owner/Staff quét |
| `Status` | enum | `Success` / `Expired` / `Suspended` / `NoActiveMembership` |

**Trường bổ sung trên `Gym.cs`:**
- `MaxCapacity`: Sức chứa tối đa của phòng Gym (mặc định 50, tùy chỉnh 5–2000).

#### Functions đã hoàn thành

| Mã | Function | Screen | Trạng thái | Mô tả |
|----|----------|--------|------------|-------|
| MEM-20 | Xem mã QR cá nhân | `Member/MyQrCode` | [x] Hoàn thành | Hiển thị QR Code + nút tải/lưu ảnh QR |
| MEM-21 | Lịch sử check-in | `Member/CheckinHistory` | [x] Hoàn thành | Danh sách lần check-in tại các gym, lọc theo thời gian |
| OWN-29 | Quét QR check-in | `OwnerCheckin/Scan/{gymId}` | [x] Hoàn thành | Mở camera quét QR, hiển thị thông tin Member + trạng thái |
| OWN-30 | Xác nhận check-in | `OwnerCheckin/Confirm` (POST) | [x] Hoàn thành | Bấm xác nhận cho Member vào tập, check-out khi rời phòng |
| OWN-31 | Lịch sử check-in gym | `OwnerCheckin/History/{gymId}` | [x] Hoàn thành | Danh sách check-in hôm nay / theo ngày, thống kê lượt tập |
| OWN-32 | Thống kê lượt check-in | `OwnerDashboard/CheckinStats/{gymId}` | [x] Hoàn thành | Biểu đồ check-in theo ngày/tuần/tháng, giờ cao điểm |
| GUE-12 | Thước đo độ đông đúc thời gian thực (Live Crowd Meter) | `Gym/Details/{id}` | [x] Hoàn thành | Đếm số lượng khách đang tập thực tế so với sức chứa tối đa (`MaxCapacity`), hiển thị % công suất và trạng thái: Đang vắng / Khá đông / Rất đông để người mua vé biết tình trạng trước khi đến |
| ADM-18 | Giám sát check-in toàn hệ thống | `AdminCheckin/Index` | [x] Hoàn thành | Thống kê check-in tổng quan |

#### Nội dung mã QR
```json
{
  "memberId": "user-guid-here",
  "token": "secure-token-guid",
  "generatedAt": "2026-08-29T12:00:00"
}
```
- QR được encode dưới dạng **JSON → Base64 → QR Image**
- Sinh QR bằng thư viện **QRCoder** (NuGet) — hoàn toàn server-side, không cần API bên thứ ba
- Token **đổi mỗi 24 giờ** (hoặc khi Member bấm "Làm mới QR") → chống chụp ảnh QR giả mạo

---

## MODULE 5: ⭐ RATING & REVIEW — ✅ [ĐÃ HOÀN THÀNH]

### 5.1 Ý tưởng
- Member đã từng mua vé tại gym mới được phép đánh giá
- Giúp Guest/Member khác tham khảo khi chọn gym
- Tích hợp **bộ lọc từ ngữ cấm (Profanity Filter)** để bảo đảm môi trường đánh giá văn minh, không xúc phạm hay spam

### 5.2 Giải pháp V2 (Đã triển khai)

#### Database — Entities mới (Đã áp dụng Migration `AddGymReviewsAndBannedWords`)

**`GymReview.cs`** — Đánh giá của hội viên

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `GymId` | int (FK → Gym) | Phòng gym được đánh giá |
| `MemberId` | string (FK → ApplicationUser) | Hội viên đánh giá |
| `Rating` | int | Số sao 1-5 |
| `Comment` | string? | Nội dung đánh giá (tối đa 500 ký tự) |
| `CreatedAt` | DateTime | Thời điểm đánh giá |
| `UpdatedAt` | DateTime? | Thời điểm chỉnh sửa |
| `IsVisible` | bool | Trạng thái hiển thị (Owner/Admin có thể ẩn) |
| `OwnerReply` | string? | Phản hồi của chủ phòng gym |
| `OwnerRepliedAt` | DateTime? | Thời điểm chủ phòng phản hồi |

**`BannedWord.cs`** — Danh sách từ ngữ cấm trong bình luận

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `Word` | string | Từ ngữ/cụm từ bị cấm (viết hoa/thường, không phân biệt dấu) |
| `Category` | string? | Phân loại (Thô tục, Xúc phạm, Lừa đảo, Quảng cáo) |
| `CreatedAt` | DateTime | Thời điểm thêm vào danh sách |

#### Functions đã hoàn thành

| Mã | Function | Screen | Trạng thái | Mô tả |
|----|----------|--------|------------|-------|
| MEM-22 | Viết đánh giá Gym | `Gym/Details/{id}` (nâng cấp) | [x] Hoàn thành | Form chấm sao + viết nhận xét (chỉ khi đã mua vé, tự động kiểm tra từ cấm) |
| MEM-23 | Sửa/xóa đánh giá | `Member/MyReviews` | [x] Hoàn thành | Quản lý các đánh giá đã viết, chỉnh sửa hoặc xóa |
| GUE-11 | Xem đánh giá Gym | `Gym/Details/{id}` & `Gym/Search` | [x] Hoàn thành | Danh sách review, thanh phân bổ 5⭐-1⭐, điểm trung bình sao trên card |
| OWN-33 | Quản lý review | `OwnerReview/Index/{gymId}` | [x] Hoàn thành | Xem tất cả review, ẩn review vi phạm, viết phản hồi cho khách |
| ADM-19 | Giám sát review toàn hệ thống | `AdminReview/Index` | [x] Hoàn thành | Quản lý, ẩn hoặc xóa vĩnh viễn review |
| ADM-20 | Cấu hình từ ngữ cấm | `AdminReview/BannedWords` | [x] Hoàn thành | Thiết lập danh sách từ cấm, tự động chặn bình luận vi phạm |

#### Business Rules
- **1 Member chỉ được 1 review / gym** (có thể sửa lại hoặc xóa)
- Chỉ Member **đã từng có vé Success / MemberMembership** tại gym mới được đánh giá
- **Bộ lọc từ ngữ cấm**: Khi gửi hoặc sửa review, hệ thống quét comment với danh sách `BannedWord`. Nếu phát hiện từ cấm → Chặn lưu và báo lỗi cụ thể để thành viên chỉnh sửa
- Owner có thể **ẩn review** và **phản hồi review**, nhưng KHÔNG được xóa (Admin mới được xóa)
- Điểm trung bình sao hiển thị trên card Gym ở trang Search và trang Chi tiết Gym

---

## MODULE 6: 🔔 NOTIFICATION CENTER — ✅ [ĐÃ HOÀN THÀNH]

### 6.1 Ý tưởng
- Thay thế / bổ sung hệ thống banner cảnh báo V1 bằng **trung tâm thông báo** đầy đủ
- Cả Member, Owner, Admin đều có inbox thông báo riêng

### 6.2 Giải pháp V2 (Đã triển khai)

#### Database — Entity mới: `Notification.cs` (Đã áp dụng Migration `AddNotificationCenter`)

| Field | Type | Mô tả |
|-------|------|-------|
| `Id` | int (PK) | |
| `UserId` | string (FK → ApplicationUser) | Người nhận |
| `Title` | string | Tiêu đề thông báo |
| `Message` | string | Nội dung chi tiết |
| `Type` | enum | `Info` / `Warning` / `Success` / `Danger` |
| `Category` | enum | `Membership` / `Suspension` / `VipUpgrade` / `Payment` / `System` / `GymApproval` / `Review` |
| `LinkUrl` | string? | URL liên kết (VD: `/Member/MembershipDetails/5`) |
| `IsRead` | bool | Đã đọc chưa |
| `CreatedAt` | DateTime | Thời điểm tạo |

#### Functions đã hoàn thành

| Mã | Function | Screen | Trạng thái | Mô tả |
|----|----------|--------|------------|-------|
| MEM-24 | Xem danh sách thông báo | `Notification/Index` | [x] Hoàn thành | Inbox thông báo cá nhân, lọc đã đọc/chưa đọc, lọc phân loại, phân trang |
| MEM-25 | Đánh dấu đã đọc | Action (AJAX) | [x] Hoàn thành | Đánh dấu 1 hoặc tất cả là đã đọc, dọn dẹp thông báo cũ |
| ALL | Badge số thông báo chưa đọc & Dropdown | `_NotificationBellPartial` | [x] Hoàn thành | Biểu tượng 🔔 trên thanh nav với badge đếm và popup xổ 5 thông báo mới nhất trên cả 4 layouts |

#### Các sự kiện tự động tạo thông báo

| Sự kiện | Người nhận | Type | Mô tả |
|---------|-----------|------|-------|
| Vé sắp hết hạn (≤ 3 ngày) | Member | Warning | "Vé tại {GymName} sẽ hết hạn vào {EndDate}" |
| Vé đã hết hạn | Member | Danger | "Vé tại {GymName} đã hết hạn, gia hạn ngay!" |
| Thanh toán thành công | Member | Success | "Thanh toán {Amount} cho {PackageName} thành công" |
| Bị đình chỉ | Member | Danger | "Bạn bị đình chỉ tại {GymName}: {Reason}" |
| Gỡ đình chỉ | Member | Success | "Đình chỉ tại {GymName} đã được gỡ bỏ" |
| Thăng hạng VIP | Member | Success | "Chúc mừng! Bạn đạt hạng {TierName} tại {GymName}" |
| Có review mới | Owner | Info | "Hội viên {MemberName} đánh giá {Rating}⭐ cho {GymName}" |
| Gym được duyệt | Owner | Success | "Gym {GymName} đã được Admin phê duyệt!" |
| Gym bị từ chối | Owner | Danger | "Gym {GymName} bị từ chối: {Reason}" |
| Có giao dịch mới | Owner | Info | "{MemberName} mua {PackageName} tại {GymName}" |

---

## MODULE 7: 📊 NÂNG CẤP DASHBOARD & BÁO CÁO

### 7.1 Nâng cấp Owner Dashboard

| Function | Mô tả |
|----------|-------|
| Biểu đồ check-in theo giờ | Xác định khung giờ cao điểm (VD: 17h-20h) |
| Biểu đồ thành viên VIP | Tỷ lệ Silver / Gold / Platinum |
| Top Member tích cực | Xếp hạng theo số lần check-in tháng này |
| Tỷ lệ gia hạn | % member gia hạn vé so với tổng hết hạn |
| Xuất báo cáo Excel | Tải báo cáo doanh thu, danh sách member dạng `.xlsx` |

### 7.2 Nâng cấp Admin Dashboard

| Function | Mô tả |
|----------|-------|
| Thống kê VIP toàn hệ thống | Tổng VIP member, phân bổ theo tier |
| Thống kê đình chỉ | Số ca đình chỉ / tháng, gym nào nhiều nhất |
| Biểu đồ check-in toàn sàn | Lượt check-in tổng hợp theo thời gian |
| Top Gym đánh giá cao | Xếp hạng gym theo rating trung bình |

---

## 🗃️ TỔNG HỢP DATABASE CHANGES (V2)

### Entities mới cần tạo

| # | Entity | Bảng DB |
|---|--------|---------|
| 1 | `GymImage.cs` | GymImages |
| 2 | `MemberSuspension.cs` | MemberSuspensions |
| 3 | `VipTierSetting.cs` | VipTierSettings |
| 4 | `MemberVipStatus.cs` | MemberVipStatuses |
| 5 | `MemberQrToken.cs` | MemberQrTokens |
| 6 | `CheckinLog.cs` | CheckinLogs |
| 7 | `GymReview.cs` | GymReviews |
| 8 | `Notification.cs` | Notifications |

### Entities V1 cần sửa

| Entity | Thay đổi |
|--------|----------|
| `Gym.cs` | Thêm navigation: `ICollection<GymImage>`, `ICollection<GymReview>`, `ICollection<VipTierSetting>` |
| `ApplicationUser.cs` | Thêm navigation: `ICollection<MemberVipStatus>`, `ICollection<CheckinLog>`, `ICollection<Notification>` |
| `GymDbContext.cs` | Thêm 8 DbSet mới + cấu hình relationships |

---

## 📊 TỔNG HỢP FUNCTIONS V2

| Role | Functions V1 | Functions mới V2 | Tổng V2 |
|------|-------------|-------------------|---------|
| **Guest** | 9 | +2 | 11 |
| **Member** | 16 | +9 | 25 |
| **Owner** | 20 | +13 | 33 |
| **Admin** | 16 | +3 | 19 |
| **TỔNG** | **61** | **+27** | **88** |

---

## 🗓️ ĐỀ XUẤT THỨ TỰ TRIỂN KHAI

### Phase 1 — Nền tảng (Làm trước)
1. [x] **Module 1**: Multi-Image Gallery — ✅ **Đã hoàn thành 100%**
2. [x] **Module 2**: Đình chỉ hội viên (Member Suspension) — ✅ **Đã hoàn thành 100%**
3. [x] **Module 6**: Notification Center — ✅ **Đã hoàn thành 100%**

### Phase 2 — Core V2 Features
4. [x] **Module 3**: VIP Loyalty System — ✅ **Đã hoàn thành 100%**
5. [x] **Module 4**: QR Check-in & Live Crowd Meter — ✅ **Đã hoàn thành 100%**

### Phase 3 — Polish & Analytics
6. [x] **Module 5**: Rating & Review — ✅ **Đã hoàn thành 100%** (Đánh giá, phản hồi & bộ lọc từ cấm)
7. [ ] **Module 7**: Nâng cấp Dashboard — tổng hợp dữ liệu từ tất cả module mới (Tiếp theo)

---

## 📦 THƯ VIỆN NUGET CẦN THÊM (V2)

| Package | Mục đích |
|---------|----------|
| `QRCoder` | Sinh QR Code server-side cho Member QR |
| `ClosedXML` | Xuất báo cáo Excel (.xlsx) |
| *(Tùy chọn)* `SignalR` | Real-time notification badge (nếu muốn real-time, không bắt buộc — có thể dùng polling AJAX) |

---

## ⚙️ GIỮ NGUYÊN KIẾN TRÚC V1

> **Quan trọng**: V2 vẫn giữ nguyên kiến trúc **MVC thuần** của V1:
> - Controller gọi thẳng `GymDbContext` — KHÔNG thêm Service/Repository layer
> - Logic dùng chung đặt trong `Helpers/` (VD: `VipHelper.cs`, `QrHelper.cs`, `NotificationHelper.cs`)
> - UI phong cách **Nike Brutal Minimalism** — mở rộng Design System sẵn có
> - Thanh toán vẫn dùng **VietQR + SePay Webhook**

---

> 📝 **Ghi chú**: Đây là bản đề xuất chi tiết. Bạn có thể chọn triển khai toàn bộ hoặc chọn từng module theo ưu tiên. Mỗi module được thiết kế **độc lập** nên có thể làm riêng lẻ mà không phá vỡ V1.
