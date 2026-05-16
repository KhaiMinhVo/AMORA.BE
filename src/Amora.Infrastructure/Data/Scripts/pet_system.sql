-- Amora Pet System — PostgreSQL schema (tham chiếu; ưu tiên EF migration)
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS "Pets" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MatchId" UUID NOT NULL UNIQUE REFERENCES "MatchConnections"("Id") ON DELETE CASCADE,
    "Hp" INT NOT NULL DEFAULT 80 CHECK ("Hp" BETWEEN 0 AND 100),
    "Mood" VARCHAR(20) NOT NULL DEFAULT 'Neutral',
    "Rp" BIGINT NOT NULL DEFAULT 0,
    "Stage" VARCHAR(20) NOT NULL DEFAULT 'ResonanceSeed',
    "IsFrozen" BOOLEAN NOT NULL DEFAULT FALSE,
    "LastInteractionAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ConsecutiveNegativeVibes" INT NOT NULL DEFAULT 0,
    "LastPartnerMessageAt" TIMESTAMPTZ NULL,
    "HpGainedIn24h" INT NOT NULL DEFAULT 0,
    "HpGainWindowStart" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "RpStatsDate" DATE NOT NULL DEFAULT CURRENT_DATE,
    "RpFromTextToday" INT NOT NULL DEFAULT 0,
    "RpFromVoiceToday" INT NOT NULL DEFAULT 0,
    "OnlineBonusGrantedToday" BOOLEAN NOT NULL DEFAULT FALSE,
    "ConsecutiveHighHpDays" INT NOT NULL DEFAULT 0,
    "LastHpSnapshotDate" DATE NULL,
    "HpSnapshotSum" DOUBLE PRECISION NOT NULL DEFAULT 0,
    "HpSnapshotCount" INT NOT NULL DEFAULT 0,
    "ActiveBuffsJson" JSONB NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS "IX_Pets_IsFrozen_LastInteractionAt" ON "Pets" ("IsFrozen", "LastInteractionAt");

ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PetCoins" INT NOT NULL DEFAULT 100;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AmoraGems" INT NOT NULL DEFAULT 0;
