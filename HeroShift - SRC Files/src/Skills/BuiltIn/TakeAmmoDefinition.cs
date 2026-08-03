using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record TakeAmmoOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 30f;
}

public static class TakeAmmoDefinition
{
    public static SkillDefinition<TakeAmmoOptions> Create() => new()
    {
        Id = BuiltInSkillIds.TakeAmmo,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#5eb8b0",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new TakeAmmoOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = TakeAmmo.LoadSkill,
            EnableSkill = TakeAmmo.EnableSkill,
            DisableSkill = TakeAmmo.DisableSkill,
            UseSkill = TakeAmmo.UseSkill,
            OnTick = TakeAmmo.OnTick,
            NewRound = TakeAmmo.NewRound,
            WeaponEquip = TakeAmmo.WeaponEquip,
        },
    };
}
