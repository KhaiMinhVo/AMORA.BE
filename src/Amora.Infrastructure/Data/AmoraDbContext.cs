using Amora.Domain.Entities;
using Amora.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Amora.Infrastructure.Data;

public sealed class AmoraDbContext : DbContext
{
    public static readonly Guid SeedUserAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SeedUserBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SeedUserCId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid SeedPost1Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid SeedPost2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public AmoraDbContext(DbContextOptions<AmoraDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<VoicePost> VoicePosts => Set<VoicePost>();

    public DbSet<VoiceComment> VoiceComments => Set<VoiceComment>();

    public DbSet<MatchConnection> MatchConnections => Set<MatchConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AvatarUrl).HasMaxLength(1000).IsRequired();

            entity.HasData(
                new AppUser
                {
                    Id = SeedUserAId,
                    DisplayName = "Amora Alice",
                    AvatarUrl = "alice.png",
                    CreatedAt = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero)
                },
                new AppUser
                {
                    Id = SeedUserBId,
                    DisplayName = "Amora Bob",
                    AvatarUrl = "bob.png",
                    CreatedAt = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero)
                },
                new AppUser
                {
                    Id = SeedUserCId,
                    DisplayName = "Amora Carol",
                    AvatarUrl = "carol.png",
                    CreatedAt = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero)
                });
        });

        modelBuilder.Entity<VoicePost>(entity =>
        {
            entity.ToTable("VoicePosts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AudioUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.PosterId, x.Status });

            entity.HasData(
                new VoicePost
                {
                    Id = SeedPost1Id,
                    PosterId = SeedUserAId,
                    AudioUrl = "https://amora-s3.bucket.com/voices/post_1.m4a",
                    MatchCount = 0,
                    Status = VoicePostStatus.Open,
                    CreatedAt = new DateTimeOffset(2026, 5, 15, 8, 0, 0, TimeSpan.Zero)
                },
                new VoicePost
                {
                    Id = SeedPost2Id,
                    PosterId = SeedUserBId,
                    AudioUrl = "https://amora-s3.bucket.com/voices/post_2.m4a",
                    MatchCount = 1,
                    Status = VoicePostStatus.Open,
                    CreatedAt = new DateTimeOffset(2026, 5, 15, 9, 0, 0, TimeSpan.Zero)
                });
        });

        modelBuilder.Entity<VoiceComment>(entity =>
        {
            entity.ToTable("VoiceComments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AudioUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.PostId, x.CommenterId }).IsUnique();
            entity.HasIndex(x => new { x.PostId, x.Status });

            entity.HasOne(x => x.Post)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MatchConnection>(entity =>
        {
            entity.ToTable("MatchConnections");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.UserAId, x.Status });
            entity.HasIndex(x => new { x.UserBId, x.Status });
            entity.HasIndex(x => x.PostId);
        });

        base.OnModelCreating(modelBuilder);
    }
}