using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record RadarHackOptions : ISkillOptions
{
}

public static class RadarHackDefinition
{
    public static SkillDefinition<RadarHackOptions> Create() => new()
    {
        Id = BuiltInSkillIds.RadarHack,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#2effcb",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new RadarHackOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = RadarHack.LoadSkill,
            OnTick = RadarHack.OnTick,
        },
    };
}
