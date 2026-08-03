using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record JumpCurseOptions : ISkillOptions
{
    public float JumpVelocity { get; init; } = 301f;
}

public static class JumpCurseDefinition
{
    public static SkillDefinition<JumpCurseOptions> Create() => new()
    {
        Id = BuiltInSkillIds.JumpCurse,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#7ad1c4",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new JumpCurseOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = JumpCurse.LoadSkill,
            EnableSkill = JumpCurse.EnableSkill,
            DisableSkill = JumpCurse.DisableSkill,
            TypeSkill = JumpCurse.TypeSkill,
            OnTick = JumpCurse.OnTick,
            NewRound = JumpCurse.NewRound,
            PlayerDeath = JumpCurse.PlayerDeath,
            PlayerJump = JumpCurse.PlayerJump,
        },
    };
}
