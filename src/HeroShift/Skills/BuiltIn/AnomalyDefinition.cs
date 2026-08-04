using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record AnomalyOptions : ISkillOptions
{
    public int SecondsInBack { get; init; } = 5;
    public float Cooldown { get; init; } = 15;
}

public static class AnomalyDefinition
{
    public static SkillDefinition<AnomalyOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Anomaly,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#a86eff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new AnomalyOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Anomaly.LoadSkill,
            EnableSkill = Anomaly.EnableSkill,
            DisableSkill = Anomaly.DisableSkill,
            UseSkill = Anomaly.UseSkill,
            OnTick = Anomaly.OnTick,
            NewRound = Anomaly.NewRound,
        },
    };
}
