using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * ZeusOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record ZeusOptions : ISkillOptions
{
}

/*
 * ZeusDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Zeus gameplay implementation.
 */
public static class ZeusDefinition
{
    public static SkillDefinition<ZeusOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Zeus,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#fbff00",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ZeusOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Zeus.LoadSkill,
            EnableSkill = Zeus.EnableSkill,
            WeaponFire = Zeus.WeaponFire,
        },
    };
}
