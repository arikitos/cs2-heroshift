using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record GlueOptions : ISkillOptions
{
}

public static class GlueDefinition
{
    public static SkillDefinition<GlueOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Glue,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#fff52e",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new GlueOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Glue.LoadSkill,
            OnEntitySpawned = Glue.OnEntitySpawned,
        },
    };
}
