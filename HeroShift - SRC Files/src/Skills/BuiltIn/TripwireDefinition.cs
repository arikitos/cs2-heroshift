using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record TripwireOptions : ISkillOptions
{
    public float RadarDuration { get; init; } = 5f;
    public float TriggerRadius { get; init; } = 24f;
    public float WireHeight { get; init; } = 30f;
    public float WireWidth { get; init; } = 0.7f;
    public float MaxWallDistance { get; init; } = 400f;
    public float Cooldown { get; init; } = 20f;
}

public static class TripwireDefinition
{
    public static SkillDefinition<TripwireOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Tripwire,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff3b3b",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Rare),
        DefaultOptions = new TripwireOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Tripwire.LoadSkill,
            EnableSkill = Tripwire.EnableSkill,
            DisableSkill = Tripwire.DisableSkill,
            UseSkill = Tripwire.UseSkill,
            OnTick = Tripwire.OnTick,
            NewRound = Tripwire.NewRound,
            PlayerDeath = Tripwire.PlayerDeath,
        },
    };
}
