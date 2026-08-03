using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record GhostOptions : ISkillOptions
{
}

public static class GhostDefinition
{
    public static SkillDefinition<GhostOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Ghost,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#FFFFFF",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Epic),
        DefaultOptions = new GhostOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Ghost.LoadSkill,
            EnableSkill = Ghost.EnableSkill,
            DisableSkill = Ghost.DisableSkill,
            OnTick = Ghost.OnTick,
            CheckTransmit = Ghost.CheckTransmit,
            NewRound = Ghost.NewRound,
            PlayerHurt = Ghost.PlayerHurt,
            WeaponEquip = Ghost.WeaponEquip,
            WeaponPickup = Ghost.WeaponPickup,
        },
    };
}
