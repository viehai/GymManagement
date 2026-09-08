using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Models
{
    public class GymDbContext : IdentityDbContext<ApplicationUser>
    {
        public GymDbContext(DbContextOptions<GymDbContext> options) : base(options) { }

        public DbSet<Gym> Gyms { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<GymEquipment> GymEquipments { get; set; }
        public DbSet<MembershipPackage> MembershipPackages { get; set; }
        public DbSet<MemberMembership> MemberMemberships { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<GymImage> GymImages { get; set; }
        public DbSet<MemberSuspension> MemberSuspensions { get; set; }
        public DbSet<VipTierSetting> VipTierSettings { get; set; }
        public DbSet<MemberVipStatus> MemberVipStatuses { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // BẮT BUỘC gọi trước, để Identity tự cấu hình bảng AspNet*

            // ===================== GYM IMAGE =====================
            modelBuilder.Entity<GymImage>(entity =>
            {
                entity.HasOne(gi => gi.Gym)
                      .WithMany(g => g.GymImages)
                      .HasForeignKey(gi => gi.GymId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===================== GYM =====================
            modelBuilder.Entity<Gym>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_Gyms_Status",
                    "[Status] IN ('Pending','Approved','Rejected','Suspended')"));

                entity.HasOne(g => g.Owner)
                      .WithMany(u => u.Gyms)
                      .HasForeignKey(g => g.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict); // Không cho xóa User nếu còn Gym
            });

            // ===================== GYM EQUIPMENT =====================
            modelBuilder.Entity<GymEquipment>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_GymEquipments_CustomLogic",
                    "([IsCustom] = 1 AND [EquipmentId] IS NULL AND [CustomName] IS NOT NULL) " +
                    "OR ([IsCustom] = 0 AND [EquipmentId] IS NOT NULL)"));

                entity.HasOne(ge => ge.Gym)
                      .WithMany(g => g.GymEquipments)
                      .HasForeignKey(ge => ge.GymId)
                      .OnDelete(DeleteBehavior.Cascade); // Xóa Gym thì xóa luôn GymEquipment liên quan

                entity.HasOne(ge => ge.Equipment)
                      .WithMany(e => e.GymEquipments)
                      .HasForeignKey(ge => ge.EquipmentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===================== MEMBERSHIP PACKAGE =====================
            modelBuilder.Entity<MembershipPackage>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_MembershipPackages_Price", "[Price] > 0");
                    tb.HasCheckConstraint("CK_MembershipPackages_Type", "[PackageType] IN ('Daily','Monthly')");
                    tb.HasCheckConstraint(
                        "CK_MembershipPackages_Duration",
                        "([PackageType] = 'Daily' AND [DurationInMonths] IS NULL) " +
                        "OR ([PackageType] = 'Monthly' AND [DurationInMonths] > 0)");
                });

                entity.HasOne(p => p.Gym)
                      .WithMany(g => g.MembershipPackages)
                      .HasForeignKey(p => p.GymId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===================== MEMBER MEMBERSHIP =====================
            modelBuilder.Entity<MemberMembership>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_MemberMemberships_Dates", "[EndDate] > [StartDate]");
                    tb.HasCheckConstraint("CK_MemberMemberships_Price", "[PriceAtPurchase] > 0");
                });

                entity.HasOne(m => m.Member)
                      .WithMany(u => u.MemberMemberships)
                      .HasForeignKey(m => m.MemberId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Gym)
                      .WithMany(g => g.MemberMemberships)
                      .HasForeignKey(m => m.GymId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Package)
                      .WithMany(p => p.MemberMemberships)
                      .HasForeignKey(m => m.PackageId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===================== TRANSACTION =====================
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Transactions_Amount", "[Amount] > 0");
                    tb.HasCheckConstraint("CK_Transactions_Status", "[Status] IN ('Pending','Success','Failed')");
                });

                entity.HasOne(t => t.Member)
                      .WithMany(u => u.Transactions)
                      .HasForeignKey(t => t.MemberId)
                      .OnDelete(DeleteBehavior.Restrict);

                // 1 Membership có thể có nhiều Transaction (Giao dịch mua ban đầu + các giao dịch gia hạn)
                entity.HasOne(t => t.Membership)
                      .WithMany(m => m.Transactions)
                      .HasForeignKey(t => t.MembershipId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ===================== INVOICE =====================
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceCode).IsUnique();

                entity.HasOne(i => i.Transaction)
                      .WithOne(t => t.Invoice)
                      .HasForeignKey<Invoice>(i => i.TransactionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===================== PASSWORD RESET OTP =====================
            modelBuilder.Entity<PasswordResetOtp>(entity =>
            {
                entity.HasOne(o => o.User)
                      .WithMany()
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===================== SYSTEM LOG =====================
            modelBuilder.Entity<SystemLog>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_SystemLogs_Level",
                    "[Level] IN ('Info','Warning','Error')"));

                entity.HasOne(l => l.User)
                      .WithMany()
                      .HasForeignKey(l => l.UserId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ===================== MEMBER SUSPENSION =====================
            modelBuilder.Entity<MemberSuspension>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_MemberSuspensions_Type", "[SuspensionType] IN ('Temporary','Permanent')");
                    tb.HasCheckConstraint("CK_MemberSuspensions_Status", "[Status] IN ('Active','Lifted')");
                });

                entity.HasOne(ms => ms.Gym)
                      .WithMany(g => g.MemberSuspensions)
                      .HasForeignKey(ms => ms.GymId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ms => ms.Member)
                      .WithMany(u => u.Suspensions)
                      .HasForeignKey(ms => ms.MemberId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ms => ms.SuspendedByUser)
                      .WithMany(u => u.ExecutedSuspensions)
                      .HasForeignKey(ms => ms.SuspendedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===================== VIP TIER SETTING =====================
            modelBuilder.Entity<VipTierSetting>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_VipTierSettings_MinPurchaseCount", "[MinPurchaseCount] > 0");
                    tb.HasCheckConstraint("CK_VipTierSettings_DiscountPercent", "[DiscountPercent] IS NULL OR ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100)");
                });

                entity.HasOne(ts => ts.Gym)
                      .WithMany(g => g.VipTierSettings)
                      .HasForeignKey(ts => ts.GymId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===================== MEMBER VIP STATUS =====================
            modelBuilder.Entity<MemberVipStatus>(entity =>
            {
                entity.HasIndex(s => new { s.MemberId, s.GymId }).IsUnique();

                entity.HasOne(s => s.Gym)
                      .WithMany(g => g.MemberVipStatuses)
                      .HasForeignKey(s => s.GymId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Member)
                      .WithMany(u => u.MemberVipStatuses)
                      .HasForeignKey(s => s.MemberId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.CurrentTier)
                      .WithMany(t => t.MemberVipStatuses)
                      .HasForeignKey(s => s.CurrentTierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===================== NOTIFICATION =====================
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => new { n.UserId, n.IsRead });
                entity.HasIndex(n => new { n.UserId, n.CreatedAt });

                entity.HasOne(n => n.User)
                      .WithMany(u => u.Notifications)
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}