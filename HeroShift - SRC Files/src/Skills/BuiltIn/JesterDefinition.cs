using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record JesterOptions : ISkillOptions
{
    public float MinTime { get; init; } = 10f;
    public float MaxTime { get; init; } = 25f;
}

public static class JesterDefinition
{
    public static SkillDefinition<JesterOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Jester,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8f108f",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new JesterOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Jester.LoadSkill,
            EnableSkill = Jester.EnableSkill,
            DisableSkill = Jester.DisableSkill,
            OnTick = Jester.OnTick,
            NewRound = Jester.NewRound,
            PlayerHurtPre = Jester.PlayerHurtPre,
            BombBeginplant = Jester.BombBeginplant,
            BombBegindefuse = Jester.BombBegindefuse,
        },
    };
}
