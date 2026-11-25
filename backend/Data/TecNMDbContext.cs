using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data;

public sealed class TecNMDbContext(DbContextOptions<TecNMDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TutoringDeliverable> Deliverables => Set<TutoringDeliverable>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var deliverable = modelBuilder.Entity<TutoringDeliverable>();
        deliverable.ToTable("tutoring_deliverables");
        deliverable.Property(item => item.Id).HasColumnName("id");
        deliverable.Property(item => item.TeacherKey).HasColumnName("docente_clave");
        deliverable.Property(item => item.PeriodId).HasColumnName("periodo_id");
        deliverable.Property(item => item.ReportType).HasColumnName("report_type");
        deliverable.Property(item => item.ReportingMonth).HasColumnName("reporting_month");
        deliverable.Property(item => item.Status).HasColumnName("status");
        deliverable.Property(item => item.DelayDays).HasColumnName("delay_days");
        deliverable.Property(item => item.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
        deliverable.Property(item => item.UpdatedBy).HasColumnName("updated_by");
        deliverable.Property(item => item.UpdatedIp).HasColumnName("updated_ip");
        deliverable.Property(item => item.FormPayload).HasColumnName("form_payload").HasColumnType("jsonb");
        deliverable.Property(item => item.Version).IsRowVersion();
        deliverable.Property(item => item.IsDeleted).HasColumnName("is_deleted");

        var user = modelBuilder.Entity<User>();

        user.ToTable("users", table =>
            table.HasCheckConstraint("ck_users_role", "role IN ('Admin', 'User')"));
        user.HasKey(item => item.Id).HasName("pk_users");
        user.HasIndex(item => item.Username).IsUnique().HasDatabaseName("uq_users_username");
        user.HasIndex(item => item.Email).IsUnique().HasDatabaseName("uq_users_email");

        user.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        user.Property(item => item.Username).HasColumnName("username").HasMaxLength(64).IsRequired();
        user.Property(item => item.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        user.Property(item => item.Password).HasColumnName("password").HasMaxLength(512).IsRequired();
        user.Property(item => item.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        user.Property(item => item.Role).HasColumnName("role").HasMaxLength(16).IsRequired();
        user.Property(item => item.DocenteClave).HasColumnName("docente_clave").HasMaxLength(5);
        user.HasIndex(item => item.DocenteClave).IsUnique().HasDatabaseName("uq_users_docente_clave");
        user.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
        user.Property(item => item.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
