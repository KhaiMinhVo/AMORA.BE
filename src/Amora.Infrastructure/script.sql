CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE TABLE "MatchConnections" (
        "Id" uuid NOT NULL,
        "PostId" uuid NOT NULL,
        "UserAId" uuid NOT NULL,
        "UserBId" uuid NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MatchConnections" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "DisplayName" character varying(200) NOT NULL,
        "AvatarUrl" character varying(1000) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE TABLE "VoicePosts" (
        "Id" uuid NOT NULL,
        "PosterId" uuid NOT NULL,
        "AudioUrl" character varying(1000) NOT NULL,
        "MatchCount" integer NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VoicePosts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE TABLE "VoiceComments" (
        "Id" uuid NOT NULL,
        "PostId" uuid NOT NULL,
        "CommenterId" uuid NOT NULL,
        "AudioUrl" character varying(1000) NOT NULL,
        "Duration" integer NOT NULL,
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VoiceComments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VoiceComments_VoicePosts_PostId" FOREIGN KEY ("PostId") REFERENCES "VoicePosts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    INSERT INTO "Users" ("Id", "AvatarUrl", "CreatedAt", "DisplayName")
    VALUES ('11111111-1111-1111-1111-111111111111', 'alice.png', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Amora Alice');
    INSERT INTO "Users" ("Id", "AvatarUrl", "CreatedAt", "DisplayName")
    VALUES ('22222222-2222-2222-2222-222222222222', 'bob.png', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Amora Bob');
    INSERT INTO "Users" ("Id", "AvatarUrl", "CreatedAt", "DisplayName")
    VALUES ('33333333-3333-3333-3333-333333333333', 'carol.png', TIMESTAMPTZ '2026-05-15T00:00:00+00:00', 'Amora Carol');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    INSERT INTO "VoicePosts" ("Id", "AudioUrl", "CreatedAt", "MatchCount", "PosterId", "Status")
    VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'https://amora-s3.bucket.com/voices/post_1.m4a', TIMESTAMPTZ '2026-05-15T08:00:00+00:00', 0, '11111111-1111-1111-1111-111111111111', 'Open');
    INSERT INTO "VoicePosts" ("Id", "AudioUrl", "CreatedAt", "MatchCount", "PosterId", "Status")
    VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'https://amora-s3.bucket.com/voices/post_2.m4a', TIMESTAMPTZ '2026-05-15T09:00:00+00:00', 1, '22222222-2222-2222-2222-222222222222', 'Open');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE INDEX "IX_MatchConnections_PostId" ON "MatchConnections" ("PostId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE INDEX "IX_MatchConnections_UserAId_Status" ON "MatchConnections" ("UserAId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE INDEX "IX_MatchConnections_UserBId_Status" ON "MatchConnections" ("UserBId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_VoiceComments_PostId_CommenterId" ON "VoiceComments" ("PostId", "CommenterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE INDEX "IX_VoiceComments_PostId_Status" ON "VoiceComments" ("PostId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    CREATE INDEX "IX_VoicePosts_PosterId_Status" ON "VoicePosts" ("PosterId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515064241_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260515064241_InitialCreate', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515193531_AddPetVibeDataTable') THEN
    CREATE TABLE "PetVibeData" (
        "Id" uuid NOT NULL,
        "PostId" uuid NOT NULL,
        "Energy" double precision NOT NULL,
        "Pitch" double precision NOT NULL,
        "PitchVariance" double precision NOT NULL,
        "IsMonotone" boolean NOT NULL,
        "DurationSec" double precision NOT NULL,
        "CleanAudioUrl" character varying(1000) NOT NULL,
        "ProcessedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PetVibeData" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PetVibeData_VoicePosts_PostId" FOREIGN KEY ("PostId") REFERENCES "VoicePosts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515193531_AddPetVibeDataTable') THEN
    CREATE UNIQUE INDEX "IX_PetVibeData_PostId" ON "PetVibeData" ("PostId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515193531_AddPetVibeDataTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260515193531_AddPetVibeDataTable', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515201624_AddReportAndBlockTables') THEN
    CREATE TABLE "UserBlocks" (
        "Id" uuid NOT NULL,
        "BlockerId" uuid NOT NULL,
        "BlockedUserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserBlocks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515201624_AddReportAndBlockTables') THEN
    CREATE TABLE "UserReports" (
        "Id" uuid NOT NULL,
        "ReporterId" uuid NOT NULL,
        "TargetUserId" uuid NOT NULL,
        "Reason" character varying(30) NOT NULL,
        "Description" character varying(500),
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserReports" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515201624_AddReportAndBlockTables') THEN
    CREATE INDEX "IX_UserBlocks_BlockerId" ON "UserBlocks" ("BlockerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515201624_AddReportAndBlockTables') THEN
    CREATE UNIQUE INDEX "IX_UserBlocks_BlockerId_BlockedUserId" ON "UserBlocks" ("BlockerId", "BlockedUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515201624_AddReportAndBlockTables') THEN
    CREATE UNIQUE INDEX "IX_UserReports_ReporterId_TargetUserId" ON "UserReports" ("ReporterId", "TargetUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515201624_AddReportAndBlockTables') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260515201624_AddReportAndBlockTables', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    ALTER TABLE "Users" ADD "Bio" character varying(300);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    ALTER TABLE "Users" ADD "City" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    ALTER TABLE "Users" ADD "DateOfBirth" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    ALTER TABLE "Users" ADD "Gender" character varying(20) NOT NULL DEFAULT 'PreferNotToSay';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    ALTER TABLE "Users" ADD "Interests" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    ALTER TABLE "Users" ADD "IsProfileComplete" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    UPDATE "Users" SET "Bio" = NULL, "City" = NULL, "DateOfBirth" = NULL, "Gender" = 'PreferNotToSay', "Interests" = NULL, "IsProfileComplete" = FALSE
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    UPDATE "Users" SET "Bio" = NULL, "City" = NULL, "DateOfBirth" = NULL, "Gender" = 'PreferNotToSay', "Interests" = NULL, "IsProfileComplete" = FALSE
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    UPDATE "Users" SET "Bio" = NULL, "City" = NULL, "DateOfBirth" = NULL, "Gender" = 'PreferNotToSay', "Interests" = NULL, "IsProfileComplete" = FALSE
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260515202540_ExpandUserProfile') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260515202540_ExpandUserProfile', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516071728_AddHandshake24h') THEN
    ALTER TABLE "MatchConnections" ADD "ExpiresAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516071728_AddHandshake24h') THEN
    UPDATE "MatchConnections"
    SET "ExpiresAt" = "CreatedAt" + INTERVAL '24 hours';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516071728_AddHandshake24h') THEN
    CREATE INDEX "IX_MatchConnections_Status_ExpiresAt" ON "MatchConnections" ("Status", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516071728_AddHandshake24h') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260516071728_AddHandshake24h', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    ALTER TABLE "Users" ADD "AmoraGems" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    ALTER TABLE "Users" ADD "PetCoins" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE TABLE "Pets" (
        "Id" uuid NOT NULL,
        "MatchId" uuid NOT NULL,
        "Hp" integer NOT NULL,
        "Mood" character varying(20) NOT NULL,
        "Rp" bigint NOT NULL,
        "Stage" character varying(20) NOT NULL,
        "IsFrozen" boolean NOT NULL,
        "LastInteractionAt" timestamp with time zone NOT NULL,
        "ConsecutiveNegativeVibes" integer NOT NULL,
        "LastPartnerMessageAt" timestamp with time zone,
        "HpGainedIn24h" integer NOT NULL,
        "HpGainWindowStart" timestamp with time zone NOT NULL,
        "RpStatsDate" date NOT NULL,
        "RpFromTextToday" integer NOT NULL,
        "RpFromVoiceToday" integer NOT NULL,
        "OnlineBonusGrantedToday" boolean NOT NULL,
        "ConsecutiveHighHpDays" integer NOT NULL,
        "LastHpSnapshotDate" date,
        "HpSnapshotSum" double precision NOT NULL,
        "HpSnapshotCount" integer NOT NULL,
        "ActiveBuffsJson" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Pets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Pets_MatchConnections_MatchId" FOREIGN KEY ("MatchId") REFERENCES "MatchConnections" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE TABLE "ShopItems" (
        "Id" uuid NOT NULL,
        "Code" character varying(50) NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" text NOT NULL,
        "ItemType" character varying(20) NOT NULL,
        "PricePetCoins" integer NOT NULL,
        "PriceAmoraGems" integer NOT NULL,
        "EffectJson" jsonb NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ShopItems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE TABLE "PetStateHistories" (
        "Id" uuid NOT NULL,
        "PetId" uuid NOT NULL,
        "EventType" character varying(50) NOT NULL,
        "PayloadJson" jsonb NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PetStateHistories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PetStateHistories_Pets_PetId" FOREIGN KEY ("PetId") REFERENCES "Pets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE TABLE "PetTransactions" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "ShopItemId" uuid,
        "TransactionType" character varying(30) NOT NULL,
        "PetCoinsDelta" integer NOT NULL,
        "AmoraGemsDelta" integer NOT NULL,
        "MetadataJson" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PetTransactions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PetTransactions_ShopItems_ShopItemId" FOREIGN KEY ("ShopItemId") REFERENCES "ShopItems" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_PetTransactions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE TABLE "UserInventories" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "ShopItemId" uuid NOT NULL,
        "Quantity" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserInventories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserInventories_ShopItems_ShopItemId" FOREIGN KEY ("ShopItemId") REFERENCES "ShopItems" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_UserInventories_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000001', 'energy_cookie', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Bánh Quy Năng Lượng', '{"hp":30}', TRUE, 'Consumable', 'Bánh Quy Năng Lượng', 0, 50, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000002', 'gentle_bath', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Sữa Tắm Dịu Nhẹ', '{"buff":"AffectionateMood","hours":2}', TRUE, 'Buff', 'Sữa Tắm Dịu Nhẹ', 5, 80, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000003', 'growth_potion', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Lọ Thuốc Tăng Trưởng', '{"buff":"DoubleVoiceRp","hours":6}', TRUE, 'Buff', 'Lọ Thuốc Tăng Trưởng', 10, 120, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000004', 'resonance_candy', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Kẹo Cộng Hưởng', '{"rp":10}', TRUE, 'Consumable', 'Kẹo Cộng Hưởng', 0, 40, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000005', 'revival_flask', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Bình Hồi Sinh', '{"hp":50}', TRUE, 'Revival', 'Bình Hồi Sinh', 20, 200, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000006', 'fire_fox_skin', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Da Cáo Lửa', '{}', TRUE, 'Cosmetic', 'Da Cáo Lửa', 15, 150, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceAmoraGems", "PricePetCoins", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000007', 'memory_collar', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Vòng Cổ Kỷ Niệm', '{}', TRUE, 'Cosmetic', 'Vòng Cổ Kỷ Niệm', 30, 300, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    UPDATE "Users" SET "AmoraGems" = 0, "PetCoins" = 0
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    UPDATE "Users" SET "AmoraGems" = 0, "PetCoins" = 0
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    UPDATE "Users" SET "AmoraGems" = 0, "PetCoins" = 0
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE INDEX "IX_Pets_IsFrozen_LastInteractionAt" ON "Pets" ("IsFrozen", "LastInteractionAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE UNIQUE INDEX "IX_Pets_MatchId" ON "Pets" ("MatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE INDEX "IX_PetStateHistories_PetId_CreatedAt" ON "PetStateHistories" ("PetId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE INDEX "IX_PetTransactions_ShopItemId" ON "PetTransactions" ("ShopItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE INDEX "IX_PetTransactions_UserId_CreatedAt" ON "PetTransactions" ("UserId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE UNIQUE INDEX "IX_ShopItems_Code" ON "ShopItems" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE INDEX "IX_UserInventories_ShopItemId" ON "UserInventories" ("ShopItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    CREATE UNIQUE INDEX "IX_UserInventories_UserId_ShopItemId" ON "UserInventories" ("UserId", "ShopItemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516092907_AddPetSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260516092907_AddPetSystem', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516094252_AddIapAndPresence') THEN
    CREATE TABLE "IapPurchaseRecords" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Platform" character varying(20) NOT NULL,
        "TransactionId" character varying(200) NOT NULL,
        "ProductId" character varying(100) NOT NULL,
        "GemsGranted" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_IapPurchaseRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_IapPurchaseRecords_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516094252_AddIapAndPresence') THEN
    CREATE UNIQUE INDEX "IX_IapPurchaseRecords_Platform_TransactionId" ON "IapPurchaseRecords" ("Platform", "TransactionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516094252_AddIapAndPresence') THEN
    CREATE INDEX "IX_IapPurchaseRecords_UserId" ON "IapPurchaseRecords" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516094252_AddIapAndPresence') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260516094252_AddIapAndPresence', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    ALTER TABLE "Users" ADD "Email" character varying(256);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    ALTER TABLE "Users" ADD "LastCoPresenceCoinDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    ALTER TABLE "Users" ADD "LastPetCoinRewardDate" date;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    ALTER TABLE "Users" ADD "PasswordHash" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    CREATE TABLE "ChatReadStates" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "MatchId" uuid NOT NULL,
        "LastReadAt" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ChatReadStates" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ChatReadStates_MatchConnections_MatchId" FOREIGN KEY ("MatchId") REFERENCES "MatchConnections" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ChatReadStates_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    CREATE TABLE "MatchDailyMediaUsages" (
        "Id" uuid NOT NULL,
        "MatchId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "UsageDate" date NOT NULL,
        "ImagesSent" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_MatchDailyMediaUsages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    UPDATE "Users" SET "Email" = NULL, "LastCoPresenceCoinDate" = NULL, "LastPetCoinRewardDate" = NULL, "PasswordHash" = NULL
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    UPDATE "Users" SET "Email" = NULL, "LastCoPresenceCoinDate" = NULL, "LastPetCoinRewardDate" = NULL, "PasswordHash" = NULL
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    UPDATE "Users" SET "Email" = NULL, "LastCoPresenceCoinDate" = NULL, "LastPetCoinRewardDate" = NULL, "PasswordHash" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email") WHERE "Email" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    CREATE INDEX "IX_ChatReadStates_MatchId" ON "ChatReadStates" ("MatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    CREATE UNIQUE INDEX "IX_ChatReadStates_UserId_MatchId" ON "ChatReadStates" ("UserId", "MatchId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    CREATE UNIQUE INDEX "IX_MatchDailyMediaUsages_MatchId_UserId_UsageDate" ON "MatchDailyMediaUsages" ("MatchId", "UserId", "UsageDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260516095737_AddHighPriorityMvp') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260516095737_AddHighPriorityMvp', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518061948_AddIapRefundFields') THEN
    ALTER TABLE "IapPurchaseRecords" ADD "RefundReason" character varying(200);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518061948_AddIapRefundFields') THEN
    ALTER TABLE "IapPurchaseRecords" ADD "RefundedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260518061948_AddIapRefundFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260518061948_AddIapRefundFields', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522134329_AddIapWebhookEvents') THEN
    CREATE TABLE "IapWebhookEvents" (
        "Id" uuid NOT NULL,
        "Platform" character varying(20) NOT NULL,
        "EventId" character varying(200) NOT NULL,
        "EventType" character varying(50) NOT NULL,
        "TransactionId" character varying(200),
        "Processed" boolean NOT NULL,
        "RawPayload" character varying(4000),
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_IapWebhookEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522134329_AddIapWebhookEvents') THEN
    CREATE UNIQUE INDEX "IX_IapWebhookEvents_Platform_EventId" ON "IapWebhookEvents" ("Platform", "EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522134329_AddIapWebhookEvents') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260522134329_AddIapWebhookEvents', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    ALTER TABLE "Users" ADD "BanReason" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    ALTER TABLE "Users" ADD "BannedUntil" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    ALTER TABLE "Users" ADD "IsBanned" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    ALTER TABLE "Users" ADD "Role" character varying(50) NOT NULL DEFAULT 'User';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    UPDATE "Users" SET "BanReason" = NULL, "BannedUntil" = NULL, "IsBanned" = FALSE, "Role" = 'User'
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    UPDATE "Users" SET "BanReason" = NULL, "BannedUntil" = NULL, "IsBanned" = FALSE, "Role" = 'User'
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    UPDATE "Users" SET "BanReason" = NULL, "BannedUntil" = NULL, "IsBanned" = FALSE, "Role" = 'User'
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    INSERT INTO "Users" ("Id", "AmoraGems", "AvatarUrl", "BanReason", "BannedUntil", "Bio", "City", "CreatedAt", "DateOfBirth", "DisplayName", "Email", "Gender", "Interests", "IsBanned", "IsProfileComplete", "LastCoPresenceCoinDate", "LastPetCoinRewardDate", "PasswordHash", "PetCoins", "Role")
    VALUES ('99999999-9999-9999-9999-999999999999', 0, 'admin.png', NULL, NULL, NULL, NULL, TIMESTAMPTZ '2026-05-15T00:00:00+00:00', NULL, 'Amora Admin', 'admin@amora.app', 'PreferNotToSay', NULL, FALSE, FALSE, NULL, NULL, NULL, 0, 'Admin');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522153322_AddAdminModerationUserFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260522153322_AddAdminModerationUserFields', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523092833_AddGoogleAndPhoneAuth') THEN
    ALTER TABLE "Users" ADD "GoogleId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523092833_AddGoogleAndPhoneAuth') THEN
    ALTER TABLE "Users" ADD "PhoneNumber" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523092833_AddGoogleAndPhoneAuth') THEN
    ALTER TABLE "Users" ADD "RequiresPasswordUpdate" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523092833_AddGoogleAndPhoneAuth') THEN
    UPDATE "Users" SET "GoogleId" = NULL, "PhoneNumber" = NULL, "RequiresPasswordUpdate" = FALSE
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523092833_AddGoogleAndPhoneAuth') THEN
    UPDATE "Users" SET "GoogleId" = NULL, "PhoneNumber" = NULL, "RequiresPasswordUpdate" = FALSE
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523092833_AddGoogleAndPhoneAuth') THEN
    UPDATE "Users" SET "GoogleId" = NULL, "PhoneNumber" = NULL, "RequiresPasswordUpdate" = FALSE
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523092833_AddGoogleAndPhoneAuth') THEN
    UPDATE "Users" SET "GoogleId" = NULL, "PhoneNumber" = NULL, "RequiresPasswordUpdate" = FALSE
    WHERE "Id" = '99999999-9999-9999-9999-999999999999';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523092833_AddGoogleAndPhoneAuth') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260523092833_AddGoogleAndPhoneAuth', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523103127_AddUserPhotos') THEN
    ALTER TABLE "Users" ADD "Photos" text[] NOT NULL DEFAULT ARRAY[]::text[];
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523103127_AddUserPhotos') THEN
    UPDATE "Users" SET "Photos" = ARRAY[]::text[]
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523103127_AddUserPhotos') THEN
    UPDATE "Users" SET "Photos" = ARRAY[]::text[]
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523103127_AddUserPhotos') THEN
    UPDATE "Users" SET "Photos" = ARRAY[]::text[]
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523103127_AddUserPhotos') THEN
    UPDATE "Users" SET "Photos" = ARRAY[]::text[]
    WHERE "Id" = '99999999-9999-9999-9999-999999999999';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523103127_AddUserPhotos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260523103127_AddUserPhotos', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525102248_RemovePetMood') THEN
    ALTER TABLE "Pets" DROP COLUMN "ConsecutiveNegativeVibes";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525102248_RemovePetMood') THEN
    ALTER TABLE "Pets" DROP COLUMN "Mood";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525102248_RemovePetMood') THEN
    UPDATE "ShopItems" SET "EffectJson" = '{"hp":20}', "ItemType" = 'Consumable'
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525102248_RemovePetMood') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260525102248_RemovePetMood', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    DELETE FROM "ShopItems"
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000006';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    DELETE FROM "ShopItems"
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000007';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "Users" DROP COLUMN "AmoraGems";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "ShopItems" DROP COLUMN "PriceAmoraGems";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "PetTransactions" DROP COLUMN "AmoraGemsDelta";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "Users" RENAME COLUMN "PetCoins" TO "Diamonds";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "Users" RENAME COLUMN "LastPetCoinRewardDate" TO "LastDiamondRewardDate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "ShopItems" RENAME COLUMN "PricePetCoins" TO "PriceDiamonds";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "PetTransactions" RENAME COLUMN "PetCoinsDelta" TO "DiamondsDelta";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "Users" ADD "GoldUntil" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "Users" ADD "IsGold" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "Users" ADD "IsPremium" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    ALTER TABLE "Users" ADD "PremiumUntil" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    CREATE TABLE "PaymentTransactions" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "AmountVnd" integer NOT NULL,
        "DiamondsReceived" integer NOT NULL,
        "Provider" character varying(50) NOT NULL,
        "ProviderTransactionId" character varying(100),
        "Status" character varying(20) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PaymentTransactions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PaymentTransactions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "ShopItems" SET "Code" = 'pet_food', "Description" = 'Túi Thức Ăn Cho Pet', "Name" = 'Túi Thức Ăn Cho Pet', "PriceDiamonds" = 15
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "ShopItems" SET "PriceDiamonds" = 20
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "ShopItems" SET "PriceDiamonds" = 30
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "ShopItems" SET "PriceDiamonds" = 10
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "ShopItems" SET "PriceDiamonds" = 50
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceDiamonds", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000010', 'premium_7d', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Premium 7 Days', '{"premium_days":7}', TRUE, 'Subscription', 'Premium 7 Days', 70, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceDiamonds", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000011', 'premium_30d', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Premium 1 Month', '{"premium_days":30}', TRUE, 'Subscription', 'Premium 1 Month', 138, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceDiamonds", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000012', 'gold_7d', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Gold 7 Days', '{"gold_days":7}', TRUE, 'Subscription', 'Gold 7 Days', 98, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    INSERT INTO "ShopItems" ("Id", "Code", "CreatedAt", "Description", "EffectJson", "IsActive", "ItemType", "Name", "PriceDiamonds", "UpdatedAt")
    VALUES ('f1000001-0001-4001-8001-000000000013', 'gold_30d', TIMESTAMPTZ '2026-05-16T00:00:00+00:00', 'Gold 1 Month', '{"gold_days":30}', TRUE, 'Subscription', 'Gold 1 Month', 198, TIMESTAMPTZ '2026-05-16T00:00:00+00:00');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "Users" SET "GoldUntil" = NULL, "IsGold" = FALSE, "IsPremium" = FALSE, "PremiumUntil" = NULL
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "Users" SET "GoldUntil" = NULL, "IsGold" = FALSE, "IsPremium" = FALSE, "PremiumUntil" = NULL
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "Users" SET "GoldUntil" = NULL, "IsGold" = FALSE, "IsPremium" = FALSE, "PremiumUntil" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    UPDATE "Users" SET "GoldUntil" = NULL, "IsGold" = FALSE, "IsPremium" = FALSE, "PremiumUntil" = NULL
    WHERE "Id" = '99999999-9999-9999-9999-999999999999';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    CREATE INDEX "IX_PaymentTransactions_UserId_CreatedAt" ON "PaymentTransactions" ("UserId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525113613_AddUnifiedCurrencyAndVnPay') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260525113613_AddUnifiedCurrencyAndVnPay', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "ShopItems" ADD "DailyPurchaseLimit" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "ShopItems" ADD "ImageUrl" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "ShopItems" ADD "MinStage" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "Pets" ADD "DeathTime" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "Pets" ADD "EquippedCosmeticsJson" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "Pets" ADD "IsDead" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "Pets" ADD "LastWaterClaimAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "Pets" ADD "Name" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "Pets" ADD "WaterClaimDate" date NOT NULL DEFAULT DATE '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    ALTER TABLE "Pets" ADD "WaterClaimsToday" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000001';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000002';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000003';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000004';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000005';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000010';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000011';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000012';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    UPDATE "ShopItems" SET "DailyPurchaseLimit" = 0, "ImageUrl" = NULL, "MinStage" = NULL
    WHERE "Id" = 'f1000001-0001-4001-8001-000000000013';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531121118_AddPetGamification') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260531121118_AddPetGamification', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531134051_AddPayOsOrderCode') THEN
    ALTER TABLE "PaymentTransactions" ADD "OrderCode" bigint NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531134051_AddPayOsOrderCode') THEN
    CREATE UNIQUE INDEX "IX_PaymentTransactions_OrderCode" ON "PaymentTransactions" ("OrderCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531134051_AddPayOsOrderCode') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260531134051_AddPayOsOrderCode', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531165151_AddPetType') THEN
    ALTER TABLE "Pets" ADD "Type" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531165151_AddPetType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260531165151_AddPetType', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260601100642_AddAdminSeedPassword') THEN
    UPDATE "Users" SET "Email" = 'admin@gmail.com', "PasswordHash" = 'iofIl2qd9dzVVSm7ut0vWA==.RDDiARZIjoB+UTXq/fUhGdOrsjUfZkGWoiP1wGacbno='
    WHERE "Id" = '99999999-9999-9999-9999-999999999999';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260601100642_AddAdminSeedPassword') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260601100642_AddAdminSeedPassword', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602084832_AddAppealsAndPostReports') THEN
    ALTER TABLE "Users" ADD "AppealReason" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602084832_AddAppealsAndPostReports') THEN
    ALTER TABLE "Users" ADD "HasPendingAppeal" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602084832_AddAppealsAndPostReports') THEN
    ALTER TABLE "UserReports" ADD "TargetPostId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602084832_AddAppealsAndPostReports') THEN
    UPDATE "Users" SET "AppealReason" = NULL, "HasPendingAppeal" = FALSE
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602084832_AddAppealsAndPostReports') THEN
    UPDATE "Users" SET "AppealReason" = NULL, "HasPendingAppeal" = FALSE
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602084832_AddAppealsAndPostReports') THEN
    UPDATE "Users" SET "AppealReason" = NULL, "HasPendingAppeal" = FALSE
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602084832_AddAppealsAndPostReports') THEN
    UPDATE "Users" SET "AppealReason" = NULL, "HasPendingAppeal" = FALSE
    WHERE "Id" = '99999999-9999-9999-9999-999999999999';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602084832_AddAppealsAndPostReports') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260602084832_AddAppealsAndPostReports', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602090401_AddReportCommentId') THEN
    ALTER TABLE "UserReports" ADD "TargetCommentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602090401_AddReportCommentId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260602090401_AddReportCommentId', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604155914_AddNotificationSystem') THEN
    CREATE TABLE "Notifications" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Type" character varying(30) NOT NULL,
        "Title" character varying(200) NOT NULL,
        "Body" character varying(1000) NOT NULL,
        "IsRead" boolean NOT NULL,
        "DataJson" jsonb,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604155914_AddNotificationSystem') THEN
    CREATE INDEX "IX_Notifications_UserId_CreatedAt" ON "Notifications" ("UserId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604155914_AddNotificationSystem') THEN
    CREATE INDEX "IX_Notifications_UserId_IsRead" ON "Notifications" ("UserId", "IsRead");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604155914_AddNotificationSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260604155914_AddNotificationSystem', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604165748_AddVoiceIntroUrl') THEN
    ALTER TABLE "Users" ADD "VoiceIntroUrl" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604165748_AddVoiceIntroUrl') THEN
    UPDATE "Users" SET "VoiceIntroUrl" = NULL
    WHERE "Id" = '11111111-1111-1111-1111-111111111111';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604165748_AddVoiceIntroUrl') THEN
    UPDATE "Users" SET "VoiceIntroUrl" = NULL
    WHERE "Id" = '22222222-2222-2222-2222-222222222222';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604165748_AddVoiceIntroUrl') THEN
    UPDATE "Users" SET "VoiceIntroUrl" = NULL
    WHERE "Id" = '33333333-3333-3333-3333-333333333333';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604165748_AddVoiceIntroUrl') THEN
    UPDATE "Users" SET "VoiceIntroUrl" = NULL
    WHERE "Id" = '99999999-9999-9999-9999-999999999999';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604165748_AddVoiceIntroUrl') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260604165748_AddVoiceIntroUrl', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    ALTER TABLE "Users" DROP COLUMN "GoldUntil";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    ALTER TABLE "Users" DROP COLUMN "IsGold";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    ALTER TABLE "Users" DROP COLUMN "IsPremium";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    ALTER TABLE "Users" RENAME COLUMN "PremiumUntil" TO "SubscriptionEndDate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    ALTER TABLE "VoicePosts" ADD "MaxMatchSlots" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    ALTER TABLE "Users" ADD "SubscriptionType" character varying(20) NOT NULL DEFAULT 'Free';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    CREATE TABLE "PostBoostRecords" (
        "Id" uuid NOT NULL,
        "PostId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "BoostType" character varying(30) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PostBoostRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    UPDATE "VoicePosts" SET "MaxMatchSlots" = 3
    WHERE "Id" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    UPDATE "VoicePosts" SET "MaxMatchSlots" = 3
    WHERE "Id" = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    CREATE INDEX "IX_PostBoostRecords_PostId_ExpiresAt" ON "PostBoostRecords" ("PostId", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    CREATE INDEX "IX_PostBoostRecords_UserId_CreatedAt" ON "PostBoostRecords" ("UserId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605152208_AddSubscriptionsAndBoosts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260605152208_AddSubscriptionsAndBoosts', '8.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260608180056_AddUserBansTable') THEN
    ALTER TABLE "Users" DROP COLUMN "AppealReason";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260608180056_AddUserBansTable') THEN
    ALTER TABLE "Users" DROP COLUMN "BanReason";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260608180056_AddUserBansTable') THEN
    ALTER TABLE "Users" DROP COLUMN "BannedUntil";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260608180056_AddUserBansTable') THEN
    ALTER TABLE "Users" DROP COLUMN "HasPendingAppeal";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260608180056_AddUserBansTable') THEN
    CREATE TABLE "UserBans" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "BanReason" character varying(500) NOT NULL,
        "BannedUntil" timestamp with time zone,
        "AppealReason" character varying(1000),
        "AppealStatus" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "IsActive" boolean NOT NULL,
        CONSTRAINT "PK_UserBans" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserBans_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260608180056_AddUserBansTable') THEN
    CREATE INDEX "IX_UserBans_UserId" ON "UserBans" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260608180056_AddUserBansTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260608180056_AddUserBansTable', '8.0.8');
    END IF;
END $EF$;
COMMIT;

