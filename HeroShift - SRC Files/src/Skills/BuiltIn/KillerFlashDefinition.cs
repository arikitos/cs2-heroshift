using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record KillerFlashOptions : ISkillOptions
{
    public float FlashDuration { get; init; } = 1f;
    public bool FriendlyFire { get; init; } = true;
    public int GrenadeLimit { get; init; } = 1;
}

public static class KillerFlashDefinition
{
    public static SkillDefinition<KillerFlashOptions> Create() => new()
    {
        Id = BuiltInSkillIds.KillerFlash,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#57bcff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Epic),
        DefaultOptions = new KillerFlashOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = KillerFlash.LoadSkill,
            EnableSkill = KillerFlash.EnableSkill,
            DisableSkill = KillerFlash.DisableSkill,
            PlayerBlind = KillerFlash.PlayerBlind,
            WeaponEquip = KillerFlash.WeaponEquip,
            WeaponPickup = KillerFlash.WeaponPickup,
            GrenadeThrown = KillerFlash.GrenadeThrown,
        },
    };
}
