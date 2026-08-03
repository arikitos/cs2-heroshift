using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record FriendlyFireOptions : ISkillOptions
{
    public float HealthDamageMultiplier { get; init; } = .3f;
}

public static class FriendlyFireDefinition
{
    public static SkillDefinition<FriendlyFireOptions> Create() => new()
    {
        Id = BuiltInSkillIds.FriendlyFire,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff0000",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: true,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new FriendlyFireOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = FriendlyFire.LoadSkill,
            OnTakeDamage = FriendlyFire.OnTakeDamage,
            NewRound = FriendlyFire.NewRound,
        },
    };
}
