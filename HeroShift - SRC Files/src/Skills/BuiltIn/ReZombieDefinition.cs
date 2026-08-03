using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ReZombieOptions : ISkillOptions
{
    public int ZombieHealth { get; init; } = 500;
    public int R { get; init; } = 255;
    public int G { get; init; } = 0;
    public int B { get; init; } = 0;
    public int A { get; init; } = 60;
}

public static class ReZombieDefinition
{
    public static SkillDefinition<ReZombieOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ReZombie,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff5C0A",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ReZombieOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ReZombie.LoadSkill,
            EnableSkill = ReZombie.EnableSkill,
            DisableSkill = ReZombie.DisableSkill,
            OnTakeDamage = ReZombie.OnTakeDamage,
            NewRound = ReZombie.NewRound,
            WeaponEquip = ReZombie.WeaponEquip,
            OnWeaponCanAcquire = ReZombie.OnWeaponCanAcquire,
        },
    };
}
