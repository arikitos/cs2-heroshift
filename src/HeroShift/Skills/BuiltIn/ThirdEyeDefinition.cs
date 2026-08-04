using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ThirdEyeOptions : ISkillOptions
{
    public float Distance { get; init; } = 100f;
}

public static class ThirdEyeDefinition
{
    public static SkillDefinition<ThirdEyeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.ThirdEye,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#1b04cc",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ThirdEyeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = ThirdEye.LoadSkill,
            DisableSkill = ThirdEye.DisableSkill,
            UseSkill = ThirdEye.UseSkill,
            OnTick = ThirdEye.OnTick,
            NewRound = ThirdEye.NewRound,
        },
    };
}
