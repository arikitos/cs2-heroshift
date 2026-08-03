using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * AntyFlashOptions - typed replacement for the legacy AntyFlash.SkillConfig
 * tunables (src/player/skills/AntyFlash.cs). Defaults transcribed verbatim
 * from that SkillConfig's constructor parameters.
 */
public sealed record AntyFlashOptions : ISkillOptions
{
    public float FlashDuration { get; init; } = 7f;
    public int GrenadeLimit { get; init; } = 2;
}

/*
 * AntyFlashDefinition - typed SkillDefinition for AntyFlash. Hooks reference
 * the skill's existing public static methods directly as delegates
 * (REFACTOR.md section 23) - AntyFlash.cs's hook bodies are unchanged except
 * for the SkillsInfo.GetValue calls, which now read
 * SkillConfigurationResolver's typed AntyFlashOptions snapshot instead.
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
            Rarity: utils.Rarity.Common),
        DefaultOptions = new AntyFlashOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = AntyFlash.LoadSkill,
            PlayerBlind = AntyFlash.PlayerBlind,
            GrenadeThrown = AntyFlash.GrenadeThrown,
            WeaponEquip = AntyFlash.WeaponEquip,
            WeaponPickup = AntyFlash.WeaponPickup,
            EnableSkill = AntyFlash.EnableSkill,
            DisableSkill = AntyFlash.DisableSkill,
        },
    };
}
