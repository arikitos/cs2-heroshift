using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * PyroOptions - typed replacement for the legacy Pyro.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record PyroOptions : ISkillOptions
{
    public float RegenerationMultiplier { get; init; } = 1.5f;
    public int GrenadeLimit { get; init; } = 2;
}

/*
 * PyroDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Pyro gameplay implementation.
 */
public static class PyroDefinition
{
    public static SkillDefinition<PyroOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Pyro,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#3c47de",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PyroOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Pyro.LoadSkill,
            EnableSkill = Pyro.EnableSkill,
            DisableSkill = Pyro.DisableSkill,
            PlayerHurt = Pyro.PlayerHurt,
            WeaponEquip = Pyro.WeaponEquip,
            WeaponPickup = Pyro.WeaponPickup,
            GrenadeThrown = Pyro.GrenadeThrown,
        },
    };
}
