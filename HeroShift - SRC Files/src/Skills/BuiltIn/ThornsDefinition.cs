using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ThornsOptions : ISkillOptions
{
    public float HealthTakenScale { get; init; } = .3f;
    public int MaxTakenDamagePerShot { get; init; } = 37;
}

public static class ThornsDefinition
{
    public static SkillDefinition<ThornsOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Thorns,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#962631",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ThornsOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Thorns.LoadSkill,
            PlayerHurt = Thorns.PlayerHurt,
        },
    };
}
