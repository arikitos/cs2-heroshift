using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * FastReloadOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record FastReloadOptions : ISkillOptions
{
}

/*
 * FastReloadDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing FastReload gameplay implementation.
 */
public static class FastReloadDefinition
{
    public static SkillDefinition<FastReloadOptions> Create() => new()
    {
        Id = BuiltInSkillIds.FastReload,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ffc061",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new FastReloadOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = FastReload.LoadSkill,
            UseSkill = FastReload.UseSkill,
        },
    };
}
