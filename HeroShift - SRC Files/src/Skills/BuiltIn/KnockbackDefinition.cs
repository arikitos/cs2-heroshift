using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * KnockbackOptions - typed replacement for the legacy Knockback.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record KnockbackOptions : ISkillOptions
{
    public float KnockbackUnits { get; init; } = 120f;
    public float MaxSpeed { get; init; } = 1200f;
}

/*
 * KnockbackDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Knockback gameplay implementation.
 */
public static class KnockbackDefinition
{
    public static SkillDefinition<KnockbackOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Knockback,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff8c42",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new KnockbackOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Knockback.LoadSkill,
            WeaponFire = Knockback.WeaponFire,
        },
    };
}
