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
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // BẮT BUỘC gọi trước, để Identity tự cấu hình bảng AspNet*

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

                // 1-1 với MemberMembership: 1 giao dịch thành công tạo đúng 1 membership
                entity.HasOne(t => t.Membership)
                      .WithOne(m => m.Transaction)
                      .HasForeignKey<Transaction>(t => t.MembershipId)
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
        }
    }
}