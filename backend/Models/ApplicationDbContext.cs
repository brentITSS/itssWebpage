using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // User & Roles
    public DbSet<User> Users { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RoleType> RoleTypes { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    // Workstreams & Permissions
    public DbSet<Workstream> Workstreams { get; set; }
    public DbSet<PermissionType> PermissionTypes { get; set; }
    public DbSet<WorkstreamUser> WorkstreamUsers { get; set; }

    // Properties
    public DbSet<PropertyGroup> PropertyGroups { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<PropertyGroupUser> PropertyGroupUsers { get; set; }

    // Tenants & Tenancies
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Tenancy> Tenancies { get; set; }

    // Journal Logs
    public DbSet<JournalType> JournalTypes { get; set; }
    public DbSet<JournalSubType> JournalSubTypes { get; set; }
    public DbSet<JournalLog> JournalLogs { get; set; }
    public DbSet<JournalLogAttachment> JournalLogAttachments { get; set; }

    // Contact Logs
    public DbSet<ContactLogType> ContactLogTypes { get; set; }
    public DbSet<ContactLog> ContactLogs { get; set; }
    public DbSet<ContactLogAttachment> ContactLogAttachments { get; set; }

    // Tags
    public DbSet<TagType> TagTypes { get; set; }
    public DbSet<TagLog> TagLogs { get; set; }

    // Audit
    public DbSet<AuditLog> AuditLogs { get; set; }

    // Reminders & maintenance
    public DbSet<Reminder> Reminders { get; set; }
    public DbSet<ReminderPriority> ReminderPriorities { get; set; }
    public DbSet<MaintenanceType> MaintenanceTypes { get; set; }
    public DbSet<MaintenanceStatus> MaintenanceStatuses { get; set; }
    public DbSet<Maintenance> Maintenances { get; set; }
    public DbSet<CalendarAppointment> CalendarAppointments { get; set; }

    // Document Hub
    public DbSet<DocumentLabelSet> DocumentLabelSets { get; set; }
    public DbSet<DocumentClassificationLabel> DocumentClassificationLabels { get; set; }
    public DbSet<DocumentSummarisationTemplate> DocumentSummarisationTemplates { get; set; }
    public DbSet<DocumentExtractionTemplate> DocumentExtractionTemplates { get; set; }
    public DbSet<DocumentExtractionField> DocumentExtractionFields { get; set; }
    public DbSet<DocumentCorrectionFeedback> DocumentCorrectionFeedback { get; set; }
    public DbSet<DocumentWorkflowRule> DocumentWorkflowRules { get; set; }
    public DbSet<DocumentWorkflowStep> DocumentWorkflowSteps { get; set; }
    public DbSet<DocumentWorkflowAuditRun> DocumentWorkflowAuditRuns { get; set; }
    public DbSet<DocumentWorkflowExtractionSnapshot> DocumentWorkflowExtractionSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships and indexes
        modelBuilder.Entity<RoleType>(entity =>
        {
            entity.ToTable("tblRoleType");
            entity.HasKey(rt => rt.RoleTypeId);
            entity.Property(rt => rt.RoleTypeId).HasColumnName("roleTypeID");
            entity.Property(rt => rt.RoleTypeName).HasColumnName("roleType").HasMaxLength(100);
            entity.Property(rt => rt.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(rt => rt.Active).HasColumnName("active");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("tblRole");
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleId).HasColumnName("roleID");
            entity.Property(r => r.RoleName).HasColumnName("roleName").HasMaxLength(100);
            entity.Property(r => r.RoleTypeId).HasColumnName("roleTypeID");
            entity.Property(r => r.CreatedDate).HasColumnName("createdDate");
            entity.Property(r => r.ModifiedDate).HasColumnName("modifiedDate");

            entity.HasOne(r => r.RoleType)
                .WithMany(rt => rt.Roles)
                .HasForeignKey(r => r.RoleTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("tblUserRole");
            entity.HasKey(ur => ur.UserRoleId);
            entity.Property(ur => ur.UserRoleId).HasColumnName("roleID");
            entity.Property(ur => ur.UserId).HasColumnName("userID");
            entity.Property(ur => ur.RoleTypeId).HasColumnName("roleTypeID");
            entity.Property(ur => ur.Active).HasColumnName("active");

            entity.HasOne(ur => ur.RoleType)
                .WithMany()
                .HasForeignKey(ur => ur.RoleTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleTypeId })
            .IsUnique();

        modelBuilder.Entity<WorkstreamUser>()
            .HasIndex(wu => new { wu.UserId, wu.WorkstreamId })
            .IsUnique();

        modelBuilder.Entity<PropertyGroupUser>()
            .HasIndex(pgu => new { pgu.UserId, pgu.PropertyGroupId })
            .IsUnique();

        // Configure cascading deletes appropriately
        modelBuilder.Entity<Property>()
            .HasOne(p => p.PropertyGroup)
            .WithMany(pg => pg.Properties)
            .HasForeignKey(p => p.PropertyGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tenancy>()
            .HasOne(t => t.Property)
            .WithMany(p => p.Tenancies)
            .HasForeignKey(t => t.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant -> Tenancy relationship (Tenant has TenancyId)
        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.Tenancy)
            .WithMany()
            .HasForeignKey(t => t.TenancyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure TenantId as identity column
        modelBuilder.Entity<Tenant>()
            .Property(t => t.TenantId)
            .ValueGeneratedOnAdd();

        // TagLog relationships
        modelBuilder.Entity<TagLog>()
            .HasOne(tl => tl.Tenancy)
            .WithMany()
            .HasForeignKey(tl => tl.TenancyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JournalLog>()
            .HasOne(j => j.Property)
            .WithMany(p => p.JournalLogs)
            .HasForeignKey(j => j.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ContactLog>()
            .HasOne(c => c.Property)
            .WithMany(p => p.ContactLogs)
            .HasForeignKey(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JournalSubType>()
            .HasOne(jst => jst.JournalType)
            .WithMany(jt => jt.JournalSubTypes)
            .HasForeignKey(jst => jst.JournalTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // dbo.tblReminder — column names must match production (not PascalCase / legacy names).
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.ToTable("tblReminder");
            entity.HasKey(r => r.ReminderId);
            entity.Property(r => r.ReminderId).HasColumnName("reminderID");
            entity.Property(r => r.TenantId).HasColumnName("tenantID");
            entity.Property(r => r.TenancyId).HasColumnName("tenancyID");
            entity.Property(r => r.PropertyGroupId).HasColumnName("propertyGrpID");
            entity.Property(r => r.PropertyId).HasColumnName("propertyID");
            entity.Property(r => r.Title).HasColumnName("reminder").HasMaxLength(255);
            entity.Property(r => r.Notes).HasColumnName("reminderDetail").HasColumnType("nvarchar(max)");
            entity.Property(r => r.CreatedBy).HasColumnName("createdBy").HasMaxLength(255);
            entity.Property(r => r.CreatedDate).HasColumnName("createdDate");
            entity.Property(r => r.ReminderDate).HasColumnName("reminderDate");
            entity.Property(r => r.ReminderActive).HasColumnName("reminderActive");
            entity.Property(r => r.ReminderPriorityId).HasColumnName("reminderPriorityID");
            entity.Property(r => r.RowVersion).HasColumnName("SSMA_TimeStamp").IsRowVersion();

            entity.HasOne(r => r.Property)
                .WithMany(p => p.Reminders)
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.PropertyGroup)
                .WithMany(pg => pg.Reminders)
                .HasForeignKey(r => r.PropertyGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Tenancy)
                .WithMany()
                .HasForeignKey(r => r.TenancyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.ReminderPriority)
                .WithMany(p => p.Reminders)
                .HasForeignKey(r => r.ReminderPriorityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Maintenance>()
            .HasOne(m => m.Property)
            .WithMany(p => p.Maintenances)
            .HasForeignKey(m => m.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Maintenance>()
            .HasOne(m => m.PropertyGroup)
            .WithMany(pg => pg.Maintenances)
            .HasForeignKey(m => m.PropertyGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Maintenance>()
            .HasOne(m => m.MaintenanceType)
            .WithMany(t => t.Maintenances)
            .HasForeignKey(m => m.MaintenanceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Maintenance>()
            .HasOne(m => m.MaintenanceStatus)
            .WithMany(s => s.Maintenances)
            .HasForeignKey(m => m.MaintenanceStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CalendarAppointment>(entity =>
        {
            entity.ToTable("tblCalendarAppointment");
            entity.HasKey(x => x.CalendarAppointmentId);
            entity.Property(x => x.CalendarAppointmentId).HasColumnName("calendarAppointmentID");
            entity.Property(x => x.SourceType).HasColumnName("sourceType").HasMaxLength(50).IsRequired();
            entity.Property(x => x.SourceId).HasColumnName("sourceID").IsRequired();
            entity.Property(x => x.AppointmentDate).HasColumnName("appointmentDate").IsRequired();
            entity.Property(x => x.IsAllDay).HasColumnName("isAllDay");
            entity.Property(x => x.TitleOverride).HasColumnName("titleOverride").HasMaxLength(255);
            entity.Property(x => x.Notes).HasColumnName("notes").HasColumnType("nvarchar(max)");
            entity.Property(x => x.IsActive).HasColumnName("active");
            entity.Property(x => x.CreatedDate).HasColumnName("createdDate");
            entity.Property(x => x.ModifiedDate).HasColumnName("modifiedDate");

            entity.HasIndex(x => new { x.SourceType, x.SourceId })
                .IsUnique()
                .HasDatabaseName("UX_tblCalendarAppointment_source");
            entity.HasIndex(x => x.AppointmentDate)
                .HasDatabaseName("IX_tblCalendarAppointment_appointmentDate");
        });
    }
}
