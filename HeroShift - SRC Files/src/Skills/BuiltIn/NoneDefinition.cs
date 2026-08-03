using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * NoneOptions - typed replacement for the legacy None.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record NoneOptions : ISkillOptions
{
}

/*
 * NoneDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing None gameplay implementation.
 */
public static class NoneDefinition
{
    public static SkillDefinition<NoneOptions> Create() => new()
    {
        Id = BuiltInSkillIds.None,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#FFFFFF",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new NoneOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = None.LoadSkill,
        },
    };
}
