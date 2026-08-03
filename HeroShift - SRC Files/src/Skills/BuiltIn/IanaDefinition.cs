using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record IanaOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 30;
    public float Duration { get; init; } = 10;
}

public static class IanaDefinition
{
    public static SkillDefinition<IanaOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Iana,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#d0d930",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new IanaOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Iana.LoadSkill,
            EnableSkill = Iana.EnableSkill,
            DisableSkill = Iana.DisableSkill,
            UseSkill = Iana.UseSkill,
            OnTakeDamage = Iana.OnTakeDamage,
            OnTick = Iana.OnTick,
            NewRound = Iana.NewRound,
            PlayerHurt = Iana.PlayerHurt,
            WeaponEquip = Iana.WeaponEquip,
            WeaponPickup = Iana.WeaponPickup,
            WeaponDrop = Iana.WeaponDrop,
            OnWeaponCanAcquire = Iana.OnWeaponCanAcquire,
        },
    };
}
