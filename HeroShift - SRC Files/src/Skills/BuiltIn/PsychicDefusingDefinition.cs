using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record PsychicDefusingOptions : ISkillOptions
{
    public float MaxDefusingRange { get; init; } = 80f;
    public float DefusingTime { get; init; } = 10f;
}

public static class PsychicDefusingDefinition
{
    public static SkillDefinition<PsychicDefusingOptions> Create() => new()
    {
        Id = BuiltInSkillIds.PsychicDefusing,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#507529",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.CounterTerrorist,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PsychicDefusingOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = PsychicDefusing.LoadSkill,
            EnableSkill = PsychicDefusing.EnableSkill,
            DisableSkill = PsychicDefusing.DisableSkill,
            OnTick = PsychicDefusing.OnTick,
            NewRound = PsychicDefusing.NewRound,
            PlayerDeath = PsychicDefusing.PlayerDeath,
            BombPlanted = PsychicDefusing.BombPlanted,
        },
    };
}
