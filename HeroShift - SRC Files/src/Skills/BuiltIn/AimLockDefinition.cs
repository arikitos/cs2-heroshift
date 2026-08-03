using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record AimLockOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 20f;
    public float Duration { get; init; } = .3f;
}

public static class AimLockDefinition
{
    public static SkillDefinition<AimLockOptions> Create() => new()
    {
        Id = BuiltInSkillIds.AimLock,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#fa7b48",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new AimLockOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = AimLock.LoadSkill,
            EnableSkill = AimLock.EnableSkill,
            DisableSkill = AimLock.DisableSkill,
            UseSkill = AimLock.UseSkill,
            OnTick = AimLock.OnTick,
            NewRound = AimLock.NewRound,
        },
    };
}
