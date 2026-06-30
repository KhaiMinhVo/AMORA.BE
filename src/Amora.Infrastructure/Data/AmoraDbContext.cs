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

    public DbSet<AdminNotification> AdminNotifications => Set<AdminNotification>();

    public DbSet<VoicePost> VoicePosts => Set<VoicePost>();

    public DbSet<VoiceComment> VoiceComments => Set<VoiceComment>();

    public DbSet<MatchConnection> MatchConnections => Set<MatchConnection>();

    public DbSet<PetVibeData> PetVibeDataRecords => Set<PetVibeData>();

    public DbSet<UserReport> UserReports => Set<UserReport>();

    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    public DbSet<UserBan> UserBans => Set<UserBan>();

    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<PetStateHistory> PetStateHistories => Set<PetStateHistory>();

    public DbSet<AudioPlayLog> AudioPlayLogs => Set<AudioPlayLog>();

    public DbSet<PetActivity> PetActivities => Set<PetActivity>();

    public DbSet<ShopItem> ShopItems => Set<ShopItem>();

    public DbSet<UserInventory> UserInventories => Set<UserInventory>();

    public DbSet<PetTransaction> PetTransactions => Set<PetTransaction>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<IapPurchaseRecord> IapPurchaseRecords => Set<IapPurchaseRecord>();

    public DbSet<ChatReadState> ChatReadStates => Set<ChatReadState>();

    public DbSet<MatchDailyMediaUsage> MatchDailyMediaUsages => Set<MatchDailyMediaUsage>();

    public DbSet<IapWebhookEvent> IapWebhookEvents => Set<IapWebhookEvent>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PostBoostRecord> PostBoostRecords => Set<PostBoostRecord>();

    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    public DbSet<PostReaction> PostReactions => Set<PostReaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.PasswordHash).HasMaxLength(500);
            entity.HasIndex(x => x.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            entity.Property(x => x.AvatarUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20).HasDefaultValue(Amora.Domain.Enums.Gender.PreferNotToSay);
            entity.Property(x => x.SubscriptionType).HasConversion<string>().HasMaxLength(20).HasDefaultValue(Amora.Domain.Enums.SubscriptionType.Free);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Bio).HasMaxLength(300);
            entity.Property(x => x.Interests).HasMaxLength(500);

            entity.Property(x => x.Role).HasMaxLength(50).HasDefaultValue("User");

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
                },
                new AppUser
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    DisplayName = "Amora Admin",
                    Email = "admin@gmail.com",
                    PasswordHash = "iofIl2qd9dzVVSm7ut0vWA==.RDDiARZIjoB+UTXq/fUhGdOrsjUfZkGWoiP1wGacbno=",
                    AvatarUrl = "admin.png",
                    Role = "Admin",
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
                    MaxMatchSlots = 3,
                    CreatedAt = new DateTimeOffset(2026, 5, 15, 8, 0, 0, TimeSpan.Zero)
                },
                new VoicePost
                {
                    Id = SeedPost2Id,
                    PosterId = SeedUserBId,
                    AudioUrl = "https://amora-s3.bucket.com/voices/post_2.m4a",
                    MatchCount = 1,
                    Status = VoicePostStatus.Open,
                    MaxMatchSlots = 3,
                    CreatedAt = new DateTimeOffset(2026, 5, 15, 9, 0, 0, TimeSpan.Zero)
                });
            
            entity.HasMany(x => x.Reactions).WithOne(x => x.Post).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostBoostRecord>(entity =>
        {
            entity.ToTable("PostBoostRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BoostType).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.PostId, x.ExpiresAt });
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
        });

        modelBuilder.Entity<PostReaction>(entity =>
        {
            entity.ToTable("PostReactions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
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

            // Handshake 24h: index for background sweep query
            entity.HasIndex(x => new { x.Status, x.ExpiresAt });
        });

        modelBuilder.Entity<PetVibeData>(entity =>
        {
            entity.ToTable("PetVibeData");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CleanAudioUrl).HasMaxLength(1000);
            entity.HasIndex(x => x.PostId).IsUnique();

            entity.HasOne(x => x.Post)
                .WithOne(x => x.PetVibeData)
                .HasForeignKey<PetVibeData>(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserReport>(entity =>
        {
            entity.ToTable("UserReports");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => new { x.ReporterId, x.TargetUserId }).IsUnique();
        });

        modelBuilder.Entity<UserBlock>(entity =>
        {
            entity.ToTable("UserBlocks");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.BlockerId, x.BlockedUserId }).IsUnique();
            entity.HasIndex(x => x.BlockerId); // Filter feed query
        });

        modelBuilder.Entity<UserBan>(entity =>
        {
            entity.ToTable("UserBans");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BanReason).HasMaxLength(500).IsRequired();
            entity.Property(x => x.AppealReason).HasMaxLength(1000);
            
            entity.HasOne(x => x.User)
                .WithMany(u => u.Bans)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.ToTable("Pets");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MatchId).IsUnique();
            entity.HasIndex(x => new { x.IsFrozen, x.LastInteractionAt });
            entity.Property(x => x.Stage).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(x => x.Match)
                .WithOne()
                .HasForeignKey<Pet>(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PetStateHistory>(entity =>
        {
            entity.ToTable("PetStateHistories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PetId, x.CreatedAt });
            entity.Property(x => x.EventType).HasMaxLength(50);
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb");

            entity.HasOne(x => x.Pet).WithMany().HasForeignKey(x => x.PetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShopItem>(entity =>
        {
            entity.ToTable("ShopItems");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.ItemType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.EffectJson).HasColumnType("jsonb");

            SeedShopItems(entity);
        });

        modelBuilder.Entity<UserInventory>(entity =>
        {
            entity.ToTable("UserInventories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.ShopItemId }).IsUnique();

            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ShopItem).WithMany().HasForeignKey(e => e.ShopItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PetActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Match).WithMany().HasForeignKey(e => e.MatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Pet).WithMany().HasForeignKey(e => e.PetId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PetTransaction>(entity =>
        {
            entity.ToTable("PetTransactions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.Property(x => x.TransactionType).HasMaxLength(30);

            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ShopItem).WithMany().HasForeignKey(x => x.ShopItemId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            
            // PayOS requires OrderCode to be unique integer
            entity.HasIndex(e => e.OrderCode).IsUnique();
            entity.Property(e => e.OrderCode).IsRequired();

            entity.Property(x => x.Provider).HasMaxLength(50);
            entity.Property(x => x.ProviderTransactionId).HasMaxLength(100);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IapPurchaseRecord>(entity =>
        {
            entity.ToTable("IapPurchaseRecords");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Platform, x.TransactionId }).IsUnique();
            entity.Property(x => x.Platform).HasMaxLength(20);
            entity.Property(x => x.TransactionId).HasMaxLength(200);
            entity.Property(x => x.ProductId).HasMaxLength(100);
            entity.Property(x => x.RefundReason).HasMaxLength(200);

            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatReadState>(entity =>
        {
            entity.ToTable("ChatReadStates");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.MatchId }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Match).WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MatchDailyMediaUsage>(entity =>
        {
            entity.ToTable("MatchDailyMediaUsages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.MatchId, x.UserId, x.UsageDate }).IsUnique();
        });

        modelBuilder.Entity<IapWebhookEvent>(entity =>
        {
            entity.ToTable("IapWebhookEvents");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Platform, x.EventId }).IsUnique();
            entity.Property(x => x.Platform).HasMaxLength(20);
            entity.Property(x => x.EventId).HasMaxLength(200);
            entity.Property(x => x.EventType).HasMaxLength(50);
            entity.Property(x => x.TransactionId).HasMaxLength(200);
            entity.Property(x => x.RawPayload).HasMaxLength(4000);
        });

        modelBuilder.Entity<AdminNotification>(entity =>
        {
            entity.ToTable("AdminNotifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Title).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ActionUrl).HasMaxLength(500);
            entity.HasIndex(x => x.IsRead);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.UserId, x.IsRead });
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Body).HasMaxLength(1000);
            entity.Property(x => x.DataJson).HasColumnType("jsonb");

            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AudioPlayLog>(entity =>
        {
            entity.ToTable("AudioPlayLogs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PlayedAt);
            entity.HasIndex(x => x.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }

    private static void SeedShopItems(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<ShopItem> entity)
    {
        var items = new[]
        {
            ShopSeed(ShopItem1, "pet_food", "Túi Thức Ăn Cho Pet", "Consumable", 15, """{"hp":30}"""),
            ShopSeed(ShopItem2, "gentle_bath", "Sữa Tắm Dịu Nhẹ", "Consumable", 20, """{"hp":20}"""),
            ShopSeed(ShopItem3, "growth_potion", "Lọ Thuốc Tăng Trưởng", "Buff", 30, """{"buff":"DoubleVoiceRp","hours":6}"""),
            ShopSeed(ShopItem4, "resonance_candy", "Kẹo Cộng Hưởng", "Consumable", 10, """{"rp":10}"""),
            ShopSeed(ShopItem5, "revival_flask", "Bình Hồi Sinh", "Revival", 50, """{"hp":50}"""),
            ShopSeed(Guid.Parse("f1000001-0001-4001-8001-000000000010"), "premium_7d", "Premium 7 Days", "Subscription", 70, """{"premium_days":7}"""),
            ShopSeed(Guid.Parse("f1000001-0001-4001-8001-000000000011"), "premium_30d", "Premium 1 Month", "Subscription", 138, """{"premium_days":30}"""),
            ShopSeed(Guid.Parse("f1000001-0001-4001-8001-000000000012"), "gold_7d", "Gold 7 Days", "Subscription", 98, """{"gold_days":7}"""),
            ShopSeed(Guid.Parse("f1000001-0001-4001-8001-000000000013"), "gold_30d", "Gold 1 Month", "Subscription", 198, """{"gold_days":30}""")
        };

        entity.HasData(items);
    }

    private static readonly Guid ShopItem1 = Guid.Parse("f1000001-0001-4001-8001-000000000001");
    private static readonly Guid ShopItem2 = Guid.Parse("f1000001-0001-4001-8001-000000000002");
    private static readonly Guid ShopItem3 = Guid.Parse("f1000001-0001-4001-8001-000000000003");
    private static readonly Guid ShopItem4 = Guid.Parse("f1000001-0001-4001-8001-000000000004");
    private static readonly Guid ShopItem5 = Guid.Parse("f1000001-0001-4001-8001-000000000005");
    private static readonly Guid ShopItem6 = Guid.Parse("f1000001-0001-4001-8001-000000000006");
    private static readonly Guid ShopItem7 = Guid.Parse("f1000001-0001-4001-8001-000000000007");

    private static ShopItem ShopSeed(Guid id, string code, string name, string type, int diamonds, string effect)
    {
        return new ShopItem
        {
            Id = id,
            Code = code,
            Name = name,
            Description = name,
            ItemType = Enum.Parse<ItemType>(type),
            PriceDiamonds = diamonds,
            EffectJson = effect,
            IsActive = true,
            CreatedAt = new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero)
        };
    }
}