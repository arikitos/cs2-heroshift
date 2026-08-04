using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record FalconEyeOptions : ISkillOptions
{
    public float Distance { get; init; } = 1000f;
}

public static class FalconEyeDefinition
{
    public static SkillDefinition<FalconEyeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.FalconEye,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#d1f542",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new FalconEyeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = FalconEye.LoadSkill,
            DisableSkill = FalconEye.DisableSkill,
            UseSkill = FalconEye.UseSkill,
            OnTick = FalconEye.OnTick,
            NewRound = FalconEye.NewRound,
            WeaponPickup = FalconEye.WeaponPickup,
        },
    };
}
