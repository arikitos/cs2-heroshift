using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record RicochetOptions : ISkillOptions
{
    public int Bounces { get; init; } = 3;
    public float SegmentDistance { get; init; } = 1200f;
    public float DamageMultiplier { get; init; } = 0.5f;
    public float FallbackDamage { get; init; } = 25f;
    public bool RespectArmor { get; init; } = true;
    public float DamageFalloff { get; init; } = 0.75f;
    public int MaxImpactsPerTick { get; init; } = 2;
    public bool ShowTracer { get; init; } = true;
    public float TracerWidth { get; init; } = 0.5f;
    public float TracerSpeed { get; init; } = 2600f;
    public float TracerLength { get; init; } = 220f;
    public int MaxActiveTracers { get; init; } = 12;
    public float NormalProbeOffset { get; init; } = 6f;
}

public static class RicochetDefinition
{
    public static SkillDefinition<RicochetOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Ricochet,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ffd75e",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Rare),
        DefaultOptions = new RicochetOptions(),
        OptionsValidator = options =>
        {
            var errors = new List<string>();
            if (options.Bounces < 1) errors.Add($"{nameof(options.Bounces)} must be at least one");
            if (options.SegmentDistance <= 0) errors.Add($"{nameof(options.SegmentDistance)} must be greater than zero");
            if (options.DamageMultiplier < 0) errors.Add($"{nameof(options.DamageMultiplier)} must be non-negative");
            if (options.FallbackDamage < 0) errors.Add($"{nameof(options.FallbackDamage)} must be non-negative");
            if (options.DamageFalloff < 0) errors.Add($"{nameof(options.DamageFalloff)} must be non-negative");
            if (options.MaxImpactsPerTick < 1) errors.Add($"{nameof(options.MaxImpactsPerTick)} must be at least one");
            if (options.TracerWidth <= 0) errors.Add($"{nameof(options.TracerWidth)} must be greater than zero");
            if (options.TracerSpeed <= 0) errors.Add($"{nameof(options.TracerSpeed)} must be greater than zero");
            if (options.TracerLength <= 0) errors.Add($"{nameof(options.TracerLength)} must be greater than zero");
            if (options.MaxActiveTracers < 1) errors.Add($"{nameof(options.MaxActiveTracers)} must be at least one");
            if (options.NormalProbeOffset <= 0) errors.Add($"{nameof(options.NormalProbeOffset)} must be greater than zero");
            return errors;
        },
        Hooks = new SkillHookSet
        {
            LoadSkill = Ricochet.LoadSkill,
            DisableSkill = Ricochet.DisableSkill,
            OnTick = Ricochet.OnTick,
            NewRound = Ricochet.NewRound,
            RoundEnd = Ricochet.RoundEnd,
            PlayerDisconnect = Ricochet.PlayerDisconnect,
            BulletImpact = Ricochet.BulletImpact,
        },
    };
}
