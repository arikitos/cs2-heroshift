using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * DraculaOptions - typed replacement for the legacy Dracula.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record DraculaOptions : ISkillOptions
{
    public float HealthRegainScale { get; init; } = .3f;
}

/*
 * DraculaDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Dracula gameplay implementation.
 */
public static class DraculaDefinition
{
    public static SkillDefinition<DraculaOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Dracula,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#FA050D",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DraculaOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Dracula.LoadSkill,
            DisableSkill = Dracula.DisableSkill,
            PlayerHurt = Dracula.PlayerHurt,
        },
    };
}
