using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record DemonEyeOptions : ISkillOptions
{
    public float SecondCooldown { get; init; } = 1f;
    public int Damage { get; init; } = 5;
}

public static class DemonEyeDefinition
{
    public static SkillDefinition<DemonEyeOptions> Create() => new()
    {
        Id = BuiltInSkillIds.DemonEye,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#c91243",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DemonEyeOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = DemonEye.LoadSkill,
            OnTick = DemonEye.OnTick,
        },
    };
}
