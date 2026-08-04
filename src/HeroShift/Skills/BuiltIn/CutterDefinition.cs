using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record CutterOptions : ISkillOptions
{
}

public static class CutterDefinition
{
    public static SkillDefinition<CutterOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Cutter,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#88a31a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new CutterOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Cutter.LoadSkill,
            OnTakeDamage = Cutter.OnTakeDamage,
        },
    };
}
