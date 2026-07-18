namespace Amora.Application.Dtos.Matches;

public sealed class MatchQuotaDto
{
    public int BaseLimit { get; set; }
    public int UsedToday { get; set; }
    public int ExtraSlots { get; set; }
    public int Remaining { get; set; }
}
