using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record PawelJumperOptions : ISkillOptions
{
    public int ExtraJumpsMin { get; init; } = 1;
    public int ExtraJumpsMax { get; init; } = 4;
}

public static class PawelJumperDefinition
{
    public static SkillDefinition<PawelJumperOptions> Create() => new()
    {
        Id = BuiltInSkillIds.PawelJumper,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#FFA500",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new PawelJumperOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = PawelJumper.LoadSkill,
            EnableSkill = PawelJumper.EnableSkill,
            OnTick = PawelJumper.OnTick,
        },
    };
}
