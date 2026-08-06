using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record GrappleOptions : IMaxDistanceOptions
{
    public float Cooldown { get; init; } = 10f;
    public float MaxDistance { get; init; } = 1500f;
    public float MinDistance { get; init; } = 150f;
    public float StopDistance { get; init; } = 90f;
    public float PullSpeed { get; init; } = 850f;
    public float MaxPullSeconds { get; init; } = 3f;
    public float RopeWidth { get; init; } = 0.8f;
}

public static class GrappleDefinition
{
    public static SkillDefinition<GrappleOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Grapple,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#38e0c4",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Rare),
        DefaultOptions = new GrappleOptions(),
        OptionsValidator = options =>
        {
            var errors = new List<string>();
            errors.AddRange(SkillOptionRules.Ordered(options.MinDistance, options.MaxDistance, nameof(options.MinDistance), nameof(options.MaxDistance)));
            if (options.Cooldown < 0) errors.Add($"{nameof(options.Cooldown)} must be non-negative");
            if (options.StopDistance < 0) errors.Add($"{nameof(options.StopDistance)} must be non-negative");
            if (options.PullSpeed <= 0) errors.Add($"{nameof(options.PullSpeed)} must be greater than zero");
            if (options.MaxPullSeconds <= 0) errors.Add($"{nameof(options.MaxPullSeconds)} must be greater than zero");
            if (options.RopeWidth <= 0) errors.Add($"{nameof(options.RopeWidth)} must be greater than zero");
            return errors;
        },
        Hooks = new SkillHookSet
        {
            LoadSkill = Grapple.LoadSkill,
            EnableSkill = Grapple.EnableSkill,
            DisableSkill = Grapple.DisableSkill,
            UseSkill = Grapple.UseSkill,
            OnTick = Grapple.OnTick,
            NewRound = Grapple.NewRound,
            RoundEnd = Grapple.RoundEnd,
            PlayerDeath = Grapple.PlayerDeath,
            PlayerDisconnect = Grapple.PlayerDisconnect,
        },
    };
}
