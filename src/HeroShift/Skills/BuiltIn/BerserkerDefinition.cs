using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record BerserkerOptions : ISkillOptions
{
    public float MaxSpeedVelocity { get; init; } = 2f;
    public float MaxDamageVelocity { get; init; } = 2f;
}

public static class BerserkerDefinition
{
    public static SkillDefinition<BerserkerOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Berserker,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#cc2929",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new BerserkerOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Berserker.LoadSkill,
            EnableSkill = Berserker.EnableSkill,
            DisableSkill = Berserker.DisableSkill,
            OnTakeDamage = Berserker.OnTakeDamage,
            OnTick = Berserker.OnTick,
            NewRound = Berserker.NewRound,
            PlayerJump = Berserker.PlayerJump,
        },
    };
}
