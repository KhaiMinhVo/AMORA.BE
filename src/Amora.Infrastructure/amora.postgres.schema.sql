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

