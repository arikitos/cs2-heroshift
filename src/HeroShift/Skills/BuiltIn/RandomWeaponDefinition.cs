using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record RandomWeaponOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 15f;
}

public static class RandomWeaponDefinition
{
    public static SkillDefinition<RandomWeaponOptions> Create() => new()
    {
        Id = BuiltInSkillIds.RandomWeapon,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#e0873a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new RandomWeaponOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = RandomWeapon.LoadSkill,
            EnableSkill = RandomWeapon.EnableSkill,
            DisableSkill = RandomWeapon.DisableSkill,
            UseSkill = RandomWeapon.UseSkill,
            OnTick = RandomWeapon.OnTick,
            NewRound = RandomWeapon.NewRound,
        },
    };
}
