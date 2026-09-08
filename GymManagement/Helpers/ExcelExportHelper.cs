using ClosedXML.Excel;
using GymManagement.Helpers;

namespace GymManagement.Helpers
{
    public class OwnerRevenueExportData
    {
        public string GymName { get; set; } = "Tất cả cơ sở";
        public string PeriodLabel { get; set; } = "Tháng này";
        public decimal TotalRevenue { get; set; }
        public decimal PeriodRevenue { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public double GrowthRatePercent { get; set; }
        public int TotalSuccessfulTransactions { get; set; }
        public int TotalActiveMembers { get; set; }
        public double RenewalRatePercent { get; set; }
        public int TotalVipMembers { get; set; }
        public int SilverCount { get; set; }
        public int GoldCount { get; set; }
        public int PlatinumCount { get; set; }

        public List<OwnerTransactionExportRow> Transactions { get; set; } = new();
        public List<OwnerMemberExportRow> Members { get; set; } = new();
    }

    public class OwnerTransactionExportRow
    {
        public int Id { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string GymName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class OwnerMemberExportRow
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string GymName { get; set; } = string.Empty;
        public string CurrentPackage { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string VipTier { get; set; } = "Standard";
        public decimal TotalSpent { get; set; }
    }

    public class AdminSystemExportData
    {
        public decimal TotalGMV { get; set; }
        public decimal ThisMonthGMV { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalGyms { get; set; }
        public int ApprovedGyms { get; set; }
        public int PendingGyms { get; set; }
        public int RejectedGyms { get; set; }
        public int TotalUsers { get; set; }
        public int TotalMembers { get; set; }
        public int TotalOwners { get; set; }
        public int TotalVipMembers { get; set; }
        public int TotalActiveSuspensions { get; set; }

        public List<AdminGymExportRow> Gyms { get; set; } = new();
        public List<AdminSuspensionExportRow> Suspensions { get; set; } = new();
    }

    public class AdminGymExportRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalPackages { get; set; }
    }

    public class AdminSuspensionExportRow
    {
        public int Id { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string GymName { get; set; } = string.Empty;
        public string SuspensionType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public static class ExcelExportHelper
    {
        /// <summary>
        /// Tạo file Excel báo cáo doanh thu & hội viên của Chủ phòng (Owner)
        /// </summary>
        public static byte[] ExportOwnerRevenueReport(OwnerRevenueExportData data)
        {
            using var workbook = new XLWorkbook();

            // ══════════════════════════════════════════
            // SHEET 1: TỔNG QUAN & CHỈ SỐ KINH DOANH
            // ══════════════════════════════════════════
            var wsOverview = workbook.Worksheets.Add("Tổng quan KPI");
            wsOverview.ShowGridLines = true;

            // Title Banner
            wsOverview.Range("B2:F2").Merge();
            wsOverview.Cell("B2").Value = "BÁO CÁO DOANH THU & KINH DOANH — GYMPRO";
            wsOverview.Cell("B2").Style.Font.Bold = true;
            wsOverview.Cell("B2").Style.Font.FontSize = 16;
            wsOverview.Cell("B2").Style.Font.FontColor = XLColor.White;
            wsOverview.Cell("B2").Style.Fill.BackgroundColor = XLColor.FromHtml("#111111");
            wsOverview.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsOverview.Row(2).Height = 32;

            // Meta info
            wsOverview.Cell("B4").Value = "Cơ sở áp dụng:";
            wsOverview.Cell("C4").Value = data.GymName;
            wsOverview.Cell("B5").Value = "Kỳ báo cáo:";
            wsOverview.Cell("C5").Value = data.PeriodLabel;
            wsOverview.Cell("B6").Value = "Ngày xuất:";
            wsOverview.Cell("C6").Value = VnTime.Now.ToString("dd/MM/yyyy HH:mm");
            wsOverview.Range("B4:B6").Style.Font.Bold = true;
            wsOverview.Range("B4:B6").Style.Font.FontColor = XLColor.FromHtml("#4B5563");

            // KPI Table Header
            wsOverview.Cell("B8").Value = "Chỉ số kinh doanh";
            wsOverview.Cell("C8").Value = "Giá trị";
            wsOverview.Cell("D8").Value = "Ghi chú";
            wsOverview.Range("B8:D8").Style.Font.Bold = true;
            wsOverview.Range("B8:D8").Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            wsOverview.Range("B8:D8").Style.Font.FontColor = XLColor.FromHtml("#111111");

            var kpis = new (string label, object val, string fmt, string note)[]
            {
                ("Doanh thu kỳ này", data.PeriodRevenue, "#,##0 \"VNĐ\"", $"Kỳ: {data.PeriodLabel}"),
                ("Doanh thu kỳ trước", data.PreviousPeriodRevenue, "#,##0 \"VNĐ\"", "Kỳ liền trước"),
                ("Tăng trưởng kỳ", data.GrowthRatePercent / 100.0, "0.0%", data.GrowthRatePercent >= 0 ? "Tăng trưởng dương" : "Giảm"),
                ("Tổng doanh thu tích lũy", data.TotalRevenue, "#,##0 \"VNĐ\"", "Tất cả thời gian"),
                ("Giao dịch thành công", data.TotalSuccessfulTransactions, "#,##0", "Đơn hàng đã duyệt/thanh toán"),
                ("Hội viên đang tập", data.TotalActiveMembers, "#,##0", "Vé còn hạn hiệu lực"),
                ("Tỷ lệ gia hạn vé", data.RenewalRatePercent / 100.0, "0.0%", "Hội viên gia hạn vé tiếp"),
                ("Tổng hội viên VIP", data.TotalVipMembers, "#,##0", $"Silver: {data.SilverCount} | Gold: {data.GoldCount} | Platinum: {data.PlatinumCount}")
            };

            int rowIdx = 9;
            foreach (var kpi in kpis)
            {
                wsOverview.Cell(rowIdx, 2).Value = kpi.label;
                wsOverview.Cell(rowIdx, 3).Value = XLCellValue.FromObject(kpi.val);
                wsOverview.Cell(rowIdx, 3).Style.NumberFormat.Format = kpi.fmt;
                wsOverview.Cell(rowIdx, 3).Style.Font.Bold = true;
                wsOverview.Cell(rowIdx, 4).Value = kpi.note;
                rowIdx++;
            }

            wsOverview.Range(8, 2, rowIdx - 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsOverview.Range(8, 2, rowIdx - 1, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            wsOverview.Range(8, 2, rowIdx - 1, 4).Style.Border.OutsideBorderColor = XLColor.FromHtml("#D1D5DB");
            wsOverview.Range(8, 2, rowIdx - 1, 4).Style.Border.InsideBorderColor = XLColor.FromHtml("#E5E7EB");
            wsOverview.Columns(2, 4).AdjustToContents();

            // ══════════════════════════════════════════
            // SHEET 2: CHI TIẾT GIAO DỊCH
            // ══════════════════════════════════════════
            var wsTx = workbook.Worksheets.Add("Lịch sử giao dịch");
            wsTx.ShowGridLines = true;

            string[] txHeaders = { "Mã GD", "Hội viên", "Email", "Cơ sở Gym", "Gói dịch vụ", "Loại gói", "Số tiền (VNĐ)", "Phương thức", "Trạng thái", "Thời gian" };
            for (int i = 0; i < txHeaders.Length; i++)
            {
                var cell = wsTx.Cell(1, i + 1);
                cell.Value = txHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#111111");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            wsTx.Row(1).Height = 24;

            int txRow = 2;
            foreach (var tx in data.Transactions)
            {
                wsTx.Cell(txRow, 1).Value = $"#{tx.Id}";
                wsTx.Cell(txRow, 2).Value = tx.MemberName;
                wsTx.Cell(txRow, 3).Value = tx.MemberEmail;
                wsTx.Cell(txRow, 4).Value = tx.GymName;
                wsTx.Cell(txRow, 5).Value = tx.PackageName;
                wsTx.Cell(txRow, 6).Value = tx.PackageType == "Daily" ? "Vé ngày" : "Vé tháng/dài hạn";
                wsTx.Cell(txRow, 7).Value = tx.Amount;
                wsTx.Cell(txRow, 7).Style.NumberFormat.Format = "#,##0";
                wsTx.Cell(txRow, 7).Style.Font.Bold = true;
                wsTx.Cell(txRow, 8).Value = tx.PaymentMethod;
                wsTx.Cell(txRow, 9).Value = tx.Status == "Success" ? "Thành công" : tx.Status;
                wsTx.Cell(txRow, 10).Value = tx.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                txRow++;
            }

            if (data.Transactions.Any())
            {
                // Tổng cộng row
                wsTx.Cell(txRow, 6).Value = "TỔNG CỘNG:";
                wsTx.Cell(txRow, 6).Style.Font.Bold = true;
                wsTx.Cell(txRow, 7).FormulaA1 = $"SUM(G2:G{txRow - 1})";
                wsTx.Cell(txRow, 7).Style.Font.Bold = true;
                wsTx.Cell(txRow, 7).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                wsTx.Range(txRow, 1, txRow, txHeaders.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");
                wsTx.Range(txRow, 1, txRow, txHeaders.Length).Style.Border.TopBorder = XLBorderStyleValues.Double;
            }

            wsTx.Range(1, 1, txRow, txHeaders.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsTx.Range(1, 1, txRow, txHeaders.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            wsTx.Columns(1, txHeaders.Length).AdjustToContents();

            // ══════════════════════════════════════════
            // SHEET 3: DANH SÁCH HỘI VIÊN & HẠNG VIP
            // ══════════════════════════════════════════
            var wsMem = workbook.Worksheets.Add("Danh sách Hội viên");
            wsMem.ShowGridLines = true;

            string[] memHeaders = { "Họ và tên", "Email", "Số điện thoại", "Cơ sở Gym", "Gói đang dùng", "Ngày bắt đầu", "Ngày hết hạn", "Trạng thái vé", "Hạng VIP", "Chi tiêu tích lũy" };
            for (int i = 0; i < memHeaders.Length; i++)
            {
                var cell = wsMem.Cell(1, i + 1);
                cell.Value = memHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#111111");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            wsMem.Row(1).Height = 24;

            int memRow = 2;
            foreach (var m in data.Members)
            {
                wsMem.Cell(memRow, 1).Value = m.FullName;
                wsMem.Cell(memRow, 2).Value = m.Email;
                wsMem.Cell(memRow, 3).Value = m.PhoneNumber;
                wsMem.Cell(memRow, 4).Value = m.GymName;
                wsMem.Cell(memRow, 5).Value = m.CurrentPackage;
                wsMem.Cell(memRow, 6).Value = m.StartDate.ToString("dd/MM/yyyy");
                wsMem.Cell(memRow, 7).Value = m.EndDate.ToString("dd/MM/yyyy");
                wsMem.Cell(memRow, 8).Value = m.Status;
                wsMem.Cell(memRow, 9).Value = m.VipTier;
                wsMem.Cell(memRow, 10).Value = m.TotalSpent;
                wsMem.Cell(memRow, 10).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                memRow++;
            }

            wsMem.Range(1, 1, Math.Max(2, memRow - 1), memHeaders.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsMem.Range(1, 1, Math.Max(2, memRow - 1), memHeaders.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            wsMem.Columns(1, memHeaders.Length).AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Tạo file Excel báo cáo hệ thống toàn diện cho Quản trị viên (Admin)
        /// </summary>
        public static byte[] ExportAdminSystemReport(AdminSystemExportData data)
        {
            using var workbook = new XLWorkbook();

            // ══════════════════════════════════════════
            // SHEET 1: CHỈ SỐ TOÀN SÀN
            // ══════════════════════════════════════════
            var wsOverview = workbook.Worksheets.Add("Tổng quan Sàn GymPro");
            wsOverview.ShowGridLines = true;

            wsOverview.Range("B2:E2").Merge();
            wsOverview.Cell("B2").Value = "BÁO CÁO VẬN HÀNH & TÀI CHÍNH TOÀN SÀN — GYMPRO";
            wsOverview.Cell("B2").Style.Font.Bold = true;
            wsOverview.Cell("B2").Style.Font.FontSize = 15;
            wsOverview.Cell("B2").Style.Font.FontColor = XLColor.White;
            wsOverview.Cell("B2").Style.Fill.BackgroundColor = XLColor.FromHtml("#111111");
            wsOverview.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsOverview.Row(2).Height = 32;

            wsOverview.Cell("B4").Value = "Thời điểm xuất:";
            wsOverview.Cell("C4").Value = VnTime.Now.ToString("dd/MM/yyyy HH:mm");
            wsOverview.Range("B4:C4").Style.Font.Bold = true;

            wsOverview.Cell("B6").Value = "Chỉ số toàn sàn";
            wsOverview.Cell("C6").Value = "Giá trị";
            wsOverview.Cell("D6").Value = "Chi tiết";
            wsOverview.Range("B6:D6").Style.Font.Bold = true;
            wsOverview.Range("B6:D6").Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

            var adminKpis = new (string lbl, object val, string fmt, string note)[]
            {
                ("Tổng GMV toàn hệ thống", data.TotalGMV, "#,##0 \"VNĐ\"", "Tổng giá trị giao dịch thành công"),
                ("Doanh thu sàn tháng này", data.ThisMonthGMV, "#,##0 \"VNĐ\"", $"Tháng {VnTime.Now.Month}/{VnTime.Now.Year}"),
                ("Tổng số giao dịch thành công", data.TotalTransactions, "#,##0", "Tất cả cơ sở"),
                ("Tổng số cơ sở phòng Gym", data.TotalGyms, "#,##0", $"Đã duyệt: {data.ApprovedGyms} | Chờ: {data.PendingGyms} | Từ chối: {data.RejectedGyms}"),
                ("Tổng tài khoản người dùng", data.TotalUsers, "#,##0", $"Hội viên: {data.TotalMembers} | Chủ phòng: {data.TotalOwners}"),
                ("Tổng hội viên VIP toàn sàn", data.TotalVipMembers, "#,##0", "Đang giữ hạng VIP"),
                ("Số ca đình chỉ đang hiệu lực", data.TotalActiveSuspensions, "#,##0", "Hội viên đang bị phạt vi phạm")
            };

            int aRow = 7;
            foreach (var kpi in adminKpis)
            {
                wsOverview.Cell(aRow, 2).Value = kpi.lbl;
                wsOverview.Cell(aRow, 3).Value = XLCellValue.FromObject(kpi.val);
                wsOverview.Cell(aRow, 3).Style.NumberFormat.Format = kpi.fmt;
                wsOverview.Cell(aRow, 3).Style.Font.Bold = true;
                wsOverview.Cell(aRow, 4).Value = kpi.note;
                aRow++;
            }

            wsOverview.Range(6, 2, aRow - 1, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsOverview.Range(6, 2, aRow - 1, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            wsOverview.Columns(2, 4).AdjustToContents();

            // ══════════════════════════════════════════
            // SHEET 2: DANH SÁCH CƠ SỞ PHÒNG GYM
            // ══════════════════════════════════════════
            var wsGyms = workbook.Worksheets.Add("Cơ sở Phòng Gym");
            wsGyms.ShowGridLines = true;

            string[] gymHeaders = { "Mã Gym", "Tên cơ sở", "Chủ phòng", "Email", "Địa chỉ", "Trạng thái", "Ngày tham gia", "Số gói tập", "Doanh thu tích lũy" };
            for (int i = 0; i < gymHeaders.Length; i++)
            {
                var cell = wsGyms.Cell(1, i + 1);
                cell.Value = gymHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#111111");
            }
            wsGyms.Row(1).Height = 24;

            int gRow = 2;
            foreach (var g in data.Gyms)
            {
                wsGyms.Cell(gRow, 1).Value = $"#{g.Id}";
                wsGyms.Cell(gRow, 2).Value = g.Name;
                wsGyms.Cell(gRow, 3).Value = g.OwnerName;
                wsGyms.Cell(gRow, 4).Value = g.OwnerEmail;
                wsGyms.Cell(gRow, 5).Value = g.Address;
                wsGyms.Cell(gRow, 6).Value = g.Status switch { "Approved" => "Đã duyệt", "Pending" => "Chờ duyệt", "Rejected" => "Từ chối", _ => g.Status };
                wsGyms.Cell(gRow, 7).Value = g.CreatedAt.ToString("dd/MM/yyyy");
                wsGyms.Cell(gRow, 8).Value = g.TotalPackages;
                wsGyms.Cell(gRow, 9).Value = g.TotalRevenue;
                wsGyms.Cell(gRow, 9).Style.NumberFormat.Format = "#,##0 \"VNĐ\"";
                gRow++;
            }
            wsGyms.Range(1, 1, Math.Max(2, gRow - 1), gymHeaders.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsGyms.Range(1, 1, Math.Max(2, gRow - 1), gymHeaders.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            wsGyms.Columns(1, gymHeaders.Length).AdjustToContents();

            // ══════════════════════════════════════════
            // SHEET 3: KỶ LUẬT & ĐÌNH CHỈ
            // ══════════════════════════════════════════
            var wsSusp = workbook.Worksheets.Add("Kỷ luật & Đình chỉ");
            wsSusp.ShowGridLines = true;

            string[] suspHeaders = { "Mã lệnh", "Hội viên", "Email", "Cơ sở Gym", "Loại đình chỉ", "Ngày bắt đầu", "Ngày kết thúc", "Lý do", "Trạng thái" };
            for (int i = 0; i < suspHeaders.Length; i++)
            {
                var cell = wsSusp.Cell(1, i + 1);
                cell.Value = suspHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#111111");
            }
            wsSusp.Row(1).Height = 24;

            int sRow = 2;
            foreach (var s in data.Suspensions)
            {
                wsSusp.Cell(sRow, 1).Value = $"#{s.Id}";
                wsSusp.Cell(sRow, 2).Value = s.MemberName;
                wsSusp.Cell(sRow, 3).Value = s.MemberEmail;
                wsSusp.Cell(sRow, 4).Value = s.GymName;
                wsSusp.Cell(sRow, 5).Value = s.SuspensionType == "Permanent" ? "Vĩnh viễn" : "Tạm thời";
                wsSusp.Cell(sRow, 6).Value = s.StartDate.ToString("dd/MM/yyyy");
                wsSusp.Cell(sRow, 7).Value = s.EndDate.HasValue ? s.EndDate.Value.ToString("dd/MM/yyyy") : "—";
                wsSusp.Cell(sRow, 8).Value = s.Reason;
                wsSusp.Cell(sRow, 9).Value = s.Status == "Active" ? "Đang hiệu lực" : "Đã gỡ bỏ";
                sRow++;
            }
            wsSusp.Range(1, 1, Math.Max(2, sRow - 1), suspHeaders.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsSusp.Range(1, 1, Math.Max(2, sRow - 1), suspHeaders.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            wsSusp.Columns(1, suspHeaders.Length).AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
