using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ExpensiveAmmoOptions : ISkillOptions
{
    public int MoneyPerShot { get; init; } = 50;
}

public static class ExpensiveAmmoDefinition
{
    public static SkillDefinition<ExpensiveAmmoOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ExpensiveAmmo,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#e0c341",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ExpensiveAmmoOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ExpensiveAmmo.LoadSkill,
            EnableSkill = ExpensiveAmmo.EnableSkill,
            DisableSkill = ExpensiveAmmo.DisableSkill,
            TypeSkill = ExpensiveAmmo.TypeSkill,
            OnTick = ExpensiveAmmo.OnTick,
            NewRound = ExpensiveAmmo.NewRound,
            PlayerDeath = ExpensiveAmmo.PlayerDeath,
            WeaponFire = ExpensiveAmmo.WeaponFire,
            PlayerDisconnect = ExpensiveAmmo.PlayerDisconnect,
        },
    };
}
