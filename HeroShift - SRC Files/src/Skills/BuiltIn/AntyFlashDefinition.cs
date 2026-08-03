using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * AntyFlashOptions - typed replacement for the legacy AntyFlash.SkillConfig
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record AntyFlashOptions : ISkillOptions
{
    public float FlashDuration { get; init; } = 7f;
    public int GrenadeLimit { get; init; } = 2;
}

/*
 * AntyFlashDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing AntyFlash gameplay implementation.
 */
public static class AntyFlashDefinition
{
    public static SkillDefinition<AntyFlashOptions> Create() => new()
    {
        Id = BuiltInSkillIds.AntyFlash,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#D6E6FF",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new AntyFlashOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = AntyFlash.LoadSkill,
            EnableSkill = AntyFlash.EnableSkill,
            DisableSkill = AntyFlash.DisableSkill,
            PlayerBlind = AntyFlash.PlayerBlind,
            WeaponEquip = AntyFlash.WeaponEquip,
            WeaponPickup = AntyFlash.WeaponPickup,
            GrenadeThrown = AntyFlash.GrenadeThrown,
        },
    };
}
