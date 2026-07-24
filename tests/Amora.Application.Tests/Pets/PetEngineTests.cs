using Amora.Application.Pets;
using Amora.Domain.Entities;
using Amora.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Amora.Application.Tests.Pets;

public sealed class PetEngineTests
{
    [Fact]
    public void ApplyHpGain_RespectsDailyCap()
    {
        var pet = new Pet { Hp = 50, HpGainedIn24h = 25, HpGainWindowStart = DateTimeOffset.UtcNow };
        PetEngine.ApplyHpGain(pet, 20).Should().Be(5);
        pet.Hp.Should().Be(55);
    }

    [Fact]
    public void ApplyDecay_FreezesPetAfterTwentyFourHours()
    {
        var now = DateTimeOffset.UtcNow;
        var pet = new Pet { Hp = 100, LastInteractionAt = now.AddHours(-25) };
        PetEngine.ApplyDecay(pet, now).Should().Be(100);
        pet.IsFrozen.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 50, GrowthStage.ResonanceSeed)]
    [InlineData(600, 60, GrowthStage.Sprout)]
    [InlineData(1400, 65, GrowthStage.Young)]
    [InlineData(2500, 70, GrowthStage.Adult)]
    [InlineData(5000, 80, GrowthStage.Legend)]
    public void EvaluateStage_UsesCurrentRpAndMood(int rp, int mood, GrowthStage expected)
    {
        PetEngine.EvaluateStage(new Pet { Rp = rp, Mood = mood }).Should().Be(expected);
    }

    [Fact]
    public void AwardVoiceRp_StopsAtDailyCap()
    {
        var pet = new Pet
        {
            RpStatsDate = Amora.Application.Common.TimeHelper.GetVietnamToday(),
            RpFromVoiceToday = PetEngine.DailyVoiceRpCap - 1
        };
        PetEngine.AwardVoiceRp(pet, 30).Should().Be(1);
        PetEngine.AwardVoiceRp(pet, 30).Should().Be(0);
    }
}
