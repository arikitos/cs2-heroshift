using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * SilentOptions - typed replacement for the legacy Silent.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record SilentOptions : ISkillOptions
{
}

/*
 * SilentDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing Silent gameplay implementation.
 */
public static class SilentDefinition
{
    public static SkillDefinition<SilentOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Silent,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#333333",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new SilentOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Silent.LoadSkill,
            PlayerMakeSound = Silent.PlayerMakeSound,
        },
    };
}
