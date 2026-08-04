using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record LastGaspOptions : ISkillOptions
{
    public int DamageAfterDeath { get; init; } = 30;
    public bool CanKill { get; init; } = true;
}

public static class LastGaspDefinition
{
    public static SkillDefinition<LastGaspOptions> Create() => new()
    {
        Id = BuiltInSkillIds.LastGasp,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#88bdba",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Rare),
        DefaultOptions = new LastGaspOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = LastGasp.LoadSkill,
            PlayerDeath = LastGasp.PlayerDeath,
        },
    };
}
