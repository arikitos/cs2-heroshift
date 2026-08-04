using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record PilotOptions : ISkillOptions
{
    public float MaximumFuel { get; init; } = 150f;
    public float FuelConsumption { get; init; } = .64f;
    public float Refuelling { get; init; } = .1f;
}

public static class PilotDefinition
{
    public static SkillDefinition<PilotOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Pilot,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#1466F5",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PilotOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Pilot.LoadSkill,
            EnableSkill = Pilot.EnableSkill,
            DisableSkill = Pilot.DisableSkill,
            OnTick = Pilot.OnTick,
            NewRound = Pilot.NewRound,
        },
    };
}
