using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ShadeOptions : ISkillOptions
{
    public float TeleportDistance { get; init; } = 100f;
    public float ChanceFrom { get; init; } = .3f;
    public float ChanceTo { get; init; } = .45f;
}

public static class ShadeDefinition
{
    public static SkillDefinition<ShadeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Shade,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#4d4d4d",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ShadeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Shade.LoadSkill,
            EnableSkill = Shade.EnableSkill,
            DisableSkill = Shade.DisableSkill,
            OnTick = Shade.OnTick,
            NewRound = Shade.NewRound,
            PlayerHurt = Shade.PlayerHurt,
        },
    };
}
