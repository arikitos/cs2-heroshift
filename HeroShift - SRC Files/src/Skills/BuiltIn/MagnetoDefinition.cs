using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record MagnetoOptions : ISkillOptions
{
    public float Radius { get; init; } = 100;
}

public static class MagnetoDefinition
{
    public static SkillDefinition<MagnetoOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Magneto,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#f081ec",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new MagnetoOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Magneto.LoadSkill,
            EnableSkill = Magneto.EnableSkill,
            DisableSkill = Magneto.DisableSkill,
            OnEntitySpawned = Magneto.OnEntitySpawned,
            OnTick = Magneto.OnTick,
            NewRound = Magneto.NewRound,
        },
    };
}
