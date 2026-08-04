using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record FortniteOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 2f;
    public int BarricadeHealth { get; init; } = 115;
    public string PropModel { get; init; } = "models/props/de_aztec/hr_aztec/aztec_scaffolding/aztec_scaffold_wall_support_128.vmdl";
}

public static class FortniteDefinition
{
    public static SkillDefinition<FortniteOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Fortnite,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#1b04cc",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 5,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new FortniteOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Fortnite.LoadSkill,
            EnableSkill = Fortnite.EnableSkill,
            DisableSkill = Fortnite.DisableSkill,
            UseSkill = Fortnite.UseSkill,
            OnTakeDamage = Fortnite.OnTakeDamage,
            OnTick = Fortnite.OnTick,
            NewRound = Fortnite.NewRound,
        },
    };
}
