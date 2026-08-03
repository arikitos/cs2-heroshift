using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * RobinHoodOptions - typed replacement for the legacy RobinHood.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record RobinHoodOptions : ISkillOptions
{
    public int MoneyMultiplier { get; init; } = 35;
}

/*
 * RobinHoodDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing RobinHood gameplay implementation.
 */
public static class RobinHoodDefinition
{
    public static SkillDefinition<RobinHoodOptions> Create() => new()
    {
        Id = BuiltInSkillIds.RobinHood,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#119125",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new RobinHoodOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = RobinHood.LoadSkill,
            PlayerHurt = RobinHood.PlayerHurt,
        },
    };
}
