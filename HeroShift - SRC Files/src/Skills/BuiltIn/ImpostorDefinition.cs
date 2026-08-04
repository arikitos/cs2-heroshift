using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * ImpostorOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record ImpostorOptions : ISkillOptions
{
}

/*
 * ImpostorDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Impostor gameplay implementation.
 */
public static class ImpostorDefinition
{
    public static SkillDefinition<ImpostorOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Impostor,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#99140B",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ImpostorOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Impostor.LoadSkill,
            EnableSkill = Impostor.EnableSkill,
            DisableSkill = Impostor.DisableSkill,
            NewRound = Impostor.NewRound,
        },
    };
}
