using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record QuickShotOptions : ISkillOptions
{
}

public static class QuickShotDefinition
{
    public static SkillDefinition<QuickShotOptions> Create() => new()
    {
        Id = BuiltInSkillIds.QuickShot,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8a42f5",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new QuickShotOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = QuickShot.LoadSkill,
            OnTick = QuickShot.OnTick,
        },
    };
}
