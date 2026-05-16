using Amora.Application.Pets;
using Xunit;
using Amora.Domain.Entities;
using Amora.Domain.Enums;

namespace Amora.Application.Tests.Pets;

public sealed class PetEngineTests
{
    private static Pet CreatePet(int hp = 80, long rp = 0) => new()
    {
        Id = Guid.NewGuid(),
        MatchId = Guid.NewGuid(),
        Hp = hp,
        Rp = rp,
        Mood = PetMood.Neutral,
        LastInteractionAt = DateTimeOffset.UtcNow,
        HpGainWindowStart = DateTimeOffset.UtcNow,
        RpStatsDate = DateOnly.FromDateTime(DateTime.UtcNow)
    };

    [Fact]
    public void ApplyDecay_NoDecay_Before6Hours()
    {
        var pet = CreatePet();
        pet.LastInteractionAt = DateTimeOffset.UtcNow.AddHours(-5);

        var loss = PetEngine.ApplyDecay(pet, DateTimeOffset.UtcNow);

        Assert.Equal(0, loss);
        Assert.Equal(80, pet.Hp);
    }

    [Fact]
    public void ApplyDecay_Loses2Hp_After6Hours()
    {
        var pet = CreatePet();
        pet.LastInteractionAt = DateTimeOffset.UtcNow.AddHours(-7);

        var loss = PetEngine.ApplyDecay(pet, DateTimeOffset.UtcNow);

        Assert.Equal(2, loss);
        Assert.Equal(78, pet.Hp);
    }

    [Fact]
    public void ApplyDecay_Loses5Hp_After12Hours()
    {
        var pet = CreatePet();
        pet.LastInteractionAt = DateTimeOffset.UtcNow.AddHours(-13);

        var loss = PetEngine.ApplyDecay(pet, DateTimeOffset.UtcNow);

        Assert.Equal(5, loss);
        Assert.Equal(75, pet.Hp);
    }

    [Fact]
    public void ApplyDecay_FreezesPet_WhenHpReachesZero()
    {
        var pet = CreatePet(hp: 3);
        pet.LastInteractionAt = DateTimeOffset.UtcNow.AddHours(-13);

        PetEngine.ApplyDecay(pet, DateTimeOffset.UtcNow);

        Assert.Equal(0, pet.Hp);
        Assert.True(pet.IsFrozen);
        Assert.Equal(PetMood.Lonely, pet.Mood);
    }

    [Fact]
    public void ApplyHpGain_CapsAt30Per24h()
    {
        var pet = CreatePet();
        pet.HpGainedIn24h = 28;

        var gained = PetEngine.ApplyHpGain(pet, 10);

        Assert.Equal(2, gained);
        Assert.Equal(30, pet.HpGainedIn24h);
    }

    [Fact]
    public void ApplyHpGain_BypassCap_WithItem()
    {
        var pet = CreatePet();
        pet.HpGainedIn24h = 30;

        var gained = PetEngine.ApplyHpGain(pet, 30, bypassCap: true);

        Assert.Equal(30, gained);
        Assert.Equal(100, pet.Hp);
    }

    [Fact]
    public void AwardTextRp_RespectsDailyCap()
    {
        var pet = CreatePet();
        pet.RpFromTextToday = PetEngine.DailyTextRpCap;

        var rp = PetEngine.AwardTextRp(pet);

        Assert.Equal(0, rp);
    }

    [Fact]
    public void AwardVoiceRp_ThreeRpPer30Seconds()
    {
        var pet = CreatePet();

        var rp = PetEngine.AwardVoiceRp(pet, 65);

        Assert.Equal(6, rp);
        Assert.Equal(2, pet.RpFromVoiceToday);
    }

    [Fact]
    public void AwardOnlineRp_OncePerDay()
    {
        var pet = CreatePet();

        var first = PetEngine.AwardOnlineRp(pet);
        var second = PetEngine.AwardOnlineRp(pet);

        Assert.Equal(PetEngine.DailyOnlineRp, first);
        Assert.Equal(0, second);
        Assert.True(pet.OnlineBonusGrantedToday);
    }

    [Fact]
    public void ComputeMood_Grumpy_AfterThreeNegativeVibes()
    {
        var pet = CreatePet();
        pet.ConsecutiveNegativeVibes = 3;

        var mood = PetEngine.ComputeMood(pet, vibeScore: 5, replyDelayMinutes: 1);

        Assert.Equal(PetMood.Grumpy, mood);
    }

    [Fact]
    public void ComputeMood_Excited_FastReplyHighVibe()
    {
        var pet = CreatePet();

        var mood = PetEngine.ComputeMood(pet, vibeScore: 6, replyDelayMinutes: 3);

        Assert.Equal(PetMood.Excited, mood);
    }

    [Fact]
    public void EvaluateStage_Legend_WhenRpAndHpHigh()
    {
        var pet = CreatePet(hp: 85, rp: 3100);
        pet.HpSnapshotSum = 85;
        pet.HpSnapshotCount = 1;

        var stage = PetEngine.EvaluateStage(pet);

        Assert.Equal(GrowthStage.Legend, stage);
    }

    [Fact]
    public void RegisterVibe_ResetsOnPositive()
    {
        var pet = CreatePet();
        pet.ConsecutiveNegativeVibes = 2;

        PetEngine.RegisterVibe(pet, 3);

        Assert.Equal(0, pet.ConsecutiveNegativeVibes);
    }
}
