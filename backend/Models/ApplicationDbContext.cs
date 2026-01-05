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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships and indexes
        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();

        modelBuilder.Entity<WorkstreamUser>()
            .HasIndex(wu => new { wu.UserId, wu.WorkstreamId })
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

        modelBuilder.Entity<Tenancy>()
            .HasOne(t => t.Tenant)
            .WithMany(t => t.Tenancies)
            .HasForeignKey(t => t.TenantId)
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
    }
}
