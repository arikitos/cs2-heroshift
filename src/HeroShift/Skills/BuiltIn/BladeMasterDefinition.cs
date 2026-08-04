using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record BladeMasterOptions : ISkillOptions
{
    public float TorseReflectionChance { get; init; } = .95f;
    public float LegReflectionChance { get; init; } = .70f;
    public float VelocityModifier { get; init; } = .85f;
}

public static class BladeMasterDefinition
{
    public static SkillDefinition<BladeMasterOptions> Create() => new()
    {
        Id = BuiltInSkillIds.BladeMaster,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#cc7504",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new BladeMasterOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = BladeMaster.LoadSkill,
            OnTick = BladeMaster.OnTick,
            PlayerHurtPre = BladeMaster.PlayerHurtPre,
        },
    };
}
