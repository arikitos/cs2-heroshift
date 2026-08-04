using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record LongKnifeOptions : IMaxDistanceOptions
{
    public float MaxDistance { get; init; } = 4096f;
    public bool FriendlyFire { get; init; } = true;
}

public static class LongKnifeDefinition
{
    public static SkillDefinition<LongKnifeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.LongKnife,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#c9f8ff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new LongKnifeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = LongKnife.LoadSkill,
            EnableSkill = LongKnife.EnableSkill,
            DisableSkill = LongKnife.DisableSkill,
            NewRound = LongKnife.NewRound,
            WeaponFire = LongKnife.WeaponFire,
        },
    };
}
