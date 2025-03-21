using DevDash.model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    // DbSets representing the tables in the database
    public DbSet<User> Users { get; set; }
    public DbSet<PersonalTask> PersonalTasks { get; set; }
    public DbSet<PrivateNote> PrivateNotes { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<UserTenant> UserTenants { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<UserProject> UserProjects { get; set; } // New DbSet for User-Project relationships
    public DbSet<Sprint> Sprints { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<IssueAssignedUser> IssueAssignedUsers { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<PinnedItem> PinnedItems { get; set; }

    public DbSet<Notification> Notifications { get; set; }

    //public DbSet<Notification> Notifications { get; set; }
    //public DbSet<Integration> Integrations { get; set; }

    // Constructor with DbContextOptions
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Configure the models and relationships using Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call the base method to ensure proper IdentityDbContext configuration
        base.OnModelCreating(modelBuilder);

        // PersonalTask - User Relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.PersonalTasks)
            .WithOne(pt => pt.User)
            .HasForeignKey(pt => pt.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Deleting a User also deletes their PersonalTasks.

        // PrivateNote - User Relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.PrivateNotes)
            .WithOne(pn => pn.User)
            .HasForeignKey(pn => pn.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Deleting a User also deletes their PrivateNotes.

        // Tenant - User Relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.OwnedTenants)
            .WithOne(t => t.Owner)
            .HasForeignKey(ut => ut.OwnerID)
            .OnDelete(DeleteBehavior.Cascade); //Deleting a User also deletes their OwnedTenants.

        // User-Tenant Many-to-Many Relationship (UserTenant entity)
        modelBuilder.Entity<Tenant>()
            .HasMany(t => t.JoinedUsers)
            .WithMany(u => u.JoinedTenants)
            .UsingEntity<UserTenant>(
                j => j.HasOne(ut => ut.User)
                    .WithMany(u => u.UserTenants)
                    .HasForeignKey(ut => ut.UserId)
                    .OnDelete(DeleteBehavior.Restrict),
                j => j.HasOne(ut => ut.Tenant)
                    .WithMany(t => t.UserTenants)
                    .HasForeignKey(ut => ut.TenantId)
                    .OnDelete(DeleteBehavior.Restrict),
                j =>
                {
                    j.HasKey(ut => new { ut.UserId, ut.TenantId });
                    j.ToTable("UserTenants");
                });

        // Tenan-Project One-To_Many Relationship
        modelBuilder.Entity<Tenant>()
            .HasMany(t => t.Projects)
            .WithOne(p => p.Tenant)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict); // Deleting a Tenant also deletes their Projects.

        // User-Project One-To-Many Relationship
        modelBuilder.Entity<User>()
            .HasMany(u=> u.ManagedProjects)
            .WithOne(p => p.Manager)
            .HasForeignKey(p => p.ProjectManagerId)
            .OnDelete(DeleteBehavior.SetNull); // If a Manager is deleted, their Projects' ManagerId is set to null.

        // User-Project One-To-Many Relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.CreatedProjects)
            .WithOne(p => p.Creator)
            .HasForeignKey(p => p.CreatorId)
            .OnDelete(DeleteBehavior.Restrict); // If a Creator is deleted, their Projects' ManagerId is set to null.

        // User-Project Many-To-Many Relationship (UserProject entity)
        modelBuilder.Entity<User>()
          .HasMany(u => u.WorkingProjects) 
          .WithMany(p => p.Users)
          .UsingEntity<UserProject>(
              j => j
                  .HasOne(up => up.Project)        
                  .WithMany(p => p.UserProjects)  
                  .HasForeignKey(up => up.ProjectId) 
                  .OnDelete(DeleteBehavior.Restrict), 
              j => j
                  .HasOne(up => up.User)          
                  .WithMany(u => u.UserProjects)  
                  .HasForeignKey(up => up.UserId) 
                  .OnDelete(DeleteBehavior.Restrict), 
              j =>
              {
                  j.HasKey(ut => new { ut.UserId, ut.ProjectId }); 
                  j.ToTable("UserProjects"); 
              });

        // Sprint - Project Relationship
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Sprints)
            .WithOne(s => s.Project)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade); // Deleting a Project also deletes their Sprints.

        // User - Sprint Relationship
        modelBuilder.Entity<User>()
            .HasMany(u=>u.SprintsCreated)
            .WithOne(s => s.CreatedBy)
            .HasForeignKey(s => s.CreatedById)
            .OnDelete(DeleteBehavior.SetNull); // Prevent deletion of a User if they created Sprints.

        // Project - Issue Relationship
        modelBuilder.Entity<Project>()
            .HasMany(p=>p.Issues)
            .WithOne(i => i.Project)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade); // Deleting a Project also deletes their Issues.

        // Sprint - Issue Relationship
        modelBuilder.Entity<Sprint>()
            .HasMany(s => s.Issues)
            .WithOne(i => i.Sprint)
            .HasForeignKey(i => i.SprintId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of a Sprint if it has Issues.

        // Issue - User Relationship One-To-Many (create)
        modelBuilder.Entity<User>()
            .HasMany(u => u.IssuesCreated)
            .WithOne(i => i.CreatedBy)
            .HasForeignKey(i => i.CreatedById)
            .OnDelete(DeleteBehavior.SetNull); // Prevent deletion of a User if they created Issues.

        // User-Issue Many-To-Many Relationship (IssueAssignedUser entity)
        modelBuilder.Entity<User>()
            .HasMany(u => u.IssuesAssigned)
            .WithMany(i => i.AssignedUsers)
            .UsingEntity<IssueAssignedUser>(
                j => j
                    .HasOne(iau => iau.Issue)
                    .WithMany(i => i.IssueAssignedUsers)
                    .HasForeignKey(iau => iau.IssueId)
                    .OnDelete(DeleteBehavior.Restrict),
                j => j
                    .HasOne(iau => iau.User)
                    .WithMany(u => u.IssueAssignedUsers)
                    .HasForeignKey(iau => iau.UserId)
                    .OnDelete(DeleteBehavior.Restrict),
                j =>
                {
                    j.HasKey(iau => new { iau.UserId, iau.IssueId });
                    j.ToTable("IssueAssignedUsers");
                });

        // Issue - Comment Relationship 
        modelBuilder.Entity<Issue>()
            .HasMany(i => i.Comments)
            .WithOne(c=>c.Issue)
            .HasForeignKey(c=>c.IssueId)
            .OnDelete(DeleteBehavior.Cascade); // Deleting an Issue also deletes its Comments.

        // user - Comment Relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.Comments)
            .WithOne(c => c.CreatedBy)
            .HasForeignKey(c => c.CreatedById)
            .OnDelete(DeleteBehavior.SetNull); // Prevent deletion of a User if they created Comments.


        // Configuring indexes and uniqueness using Fluent API
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique(); // Ensure username is unique.

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique(); // Ensure email is unique.

        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
            .IsUnique(); // Ensure phone number is unique.

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.TenantCode)
            .IsUnique(); // Ensure tenant code is unique.
        modelBuilder.Entity<Sprint>()
             .Property(p => p.CreatedAt)
             .HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<Comment>()
           .Property(p => p.CreationDate)
           .HasDefaultValueSql("GETUTCDATE()");


        modelBuilder.Entity<Project>()
      .Property(p => p.CreationDate)
      .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<Issue>()
        .Property(p => p.CreationDate)
        .HasDefaultValueSql("GETUTCDATE()");
        
        modelBuilder.Entity<UserTenant>()
        .Property(p => p.JoinedDate)
        .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<UserProject>()
        .Property(p => p.JoinedDate)
        .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<IssueAssignedUser>()
        .Property(p => p.Assign_date)
        .HasDefaultValueSql("GETUTCDATE()");
        
        modelBuilder.Entity<PinnedItem>()
        .Property(p => p.PinnedDate)
        .HasDefaultValueSql("GETUTCDATE()");
        
        modelBuilder.Entity<Notification>()
        .Property(p => p.CreatedAt)
        .HasDefaultValueSql("GETUTCDATE()");

        //modelBuilder.Entity<Integration>()
        //    .HasIndex(i => i.Name)
        //    .IsUnique(); // Ensure integration name is unique.
    }
}
