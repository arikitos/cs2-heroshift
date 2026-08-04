using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record RetreatOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 15f;
}

public static class RetreatDefinition
{
    public static SkillDefinition<RetreatOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Retreat,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#a86eff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new RetreatOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Retreat.LoadSkill,
            EnableSkill = Retreat.EnableSkill,
            DisableSkill = Retreat.DisableSkill,
            UseSkill = Retreat.UseSkill,
            OnTick = Retreat.OnTick,
            NewRound = Retreat.NewRound,
        },
    };
}
