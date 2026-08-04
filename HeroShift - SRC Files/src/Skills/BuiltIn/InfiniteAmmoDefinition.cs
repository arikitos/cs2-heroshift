using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * InfiniteAmmoOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record InfiniteAmmoOptions : ISkillOptions
{
}

/*
 * InfiniteAmmoDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing InfiniteAmmo gameplay implementation.
 */
public static class InfiniteAmmoDefinition
{
    public static SkillDefinition<InfiniteAmmoOptions> Create() => new()
    {
        Id = BuiltInSkillIds.InfiniteAmmo,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#0000FF",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new InfiniteAmmoOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = InfiniteAmmo.LoadSkill,
            WeaponFire = InfiniteAmmo.WeaponFire,
            WeaponReload = InfiniteAmmo.WeaponReload,
            GrenadeThrown = InfiniteAmmo.GrenadeThrown,
        },
    };
}
