using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * GrenadierOptions - typed replacement for the legacy Grenadier.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record GrenadierOptions : ISkillOptions
{
}

/*
 * GrenadierDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Grenadier gameplay implementation.
 */
public static class GrenadierDefinition
{
    public static SkillDefinition<GrenadierOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Grenadier,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#4a6e21",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new GrenadierOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Grenadier.LoadSkill,
            EnableSkill = Grenadier.EnableSkill,
            GrenadeThrown = Grenadier.GrenadeThrown,
        },
    };
}
