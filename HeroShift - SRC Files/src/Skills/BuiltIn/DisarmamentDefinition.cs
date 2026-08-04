using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * DisarmamentOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record DisarmamentOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = .2f;
    public float ChanceTo { get; init; } = .35f;
}

/*
 * DisarmamentDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Disarmament gameplay implementation.
 */
public static class DisarmamentDefinition
{
    public static SkillDefinition<DisarmamentOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Disarmament,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#FF4500",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DisarmamentOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Disarmament.LoadSkill,
            EnableSkill = Disarmament.EnableSkill,
            PlayerHurt = Disarmament.PlayerHurt,
        },
    };
}
