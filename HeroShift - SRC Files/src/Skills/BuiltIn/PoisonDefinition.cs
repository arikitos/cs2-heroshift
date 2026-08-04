using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record PoisonOptions : ISkillOptions
{
    public float Cooldown { get; init; } = .85f;
    public int Damage { get; init; } = 1;
    public int MinHealth { get; init; } = 30;
}

public static class PoisonDefinition
{
    public static SkillDefinition<PoisonOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Poison,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#902eff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PoisonOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Poison.LoadSkill,
            EnableSkill = Poison.EnableSkill,
            DisableSkill = Poison.DisableSkill,
            TypeSkill = Poison.TypeSkill,
            OnTick = Poison.OnTick,
            NewRound = Poison.NewRound,
            PlayerDeath = Poison.PlayerDeath,
            PlayerDisconnect = Poison.PlayerDisconnect,
        },
    };
}
