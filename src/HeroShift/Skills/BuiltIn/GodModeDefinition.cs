using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record GodModeOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 30f;
    public float Duration { get; init; } = 2f;
}

public static class GodModeDefinition
{
    public static SkillDefinition<GodModeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.GodMode,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#e0d83a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new GodModeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = GodMode.LoadSkill,
            EnableSkill = GodMode.EnableSkill,
            DisableSkill = GodMode.DisableSkill,
            UseSkill = GodMode.UseSkill,
            OnTick = GodMode.OnTick,
            NewRound = GodMode.NewRound,
        },
    };
}
