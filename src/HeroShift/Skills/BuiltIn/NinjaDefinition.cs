using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record NinjaOptions : ISkillOptions
{
    public float IdlePercentInvisibility { get; init; } = .3f;
    public float DuckPercentInvisibility { get; init; } = .3f;
    public float KnifePercentInvisibility { get; init; } = .3f;
}

public static class NinjaDefinition
{
    public static SkillDefinition<NinjaOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Ninja,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#dedede",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new NinjaOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Ninja.LoadSkill,
            EnableSkill = Ninja.EnableSkill,
            DisableSkill = Ninja.DisableSkill,
            OnTick = Ninja.OnTick,
            CheckTransmit = Ninja.CheckTransmit,
            NewRound = Ninja.NewRound,
            PlayerHurt = Ninja.PlayerHurt,
            WeaponEquip = Ninja.WeaponEquip,
            WeaponPickup = Ninja.WeaponPickup,
        },
    };
}
