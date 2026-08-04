using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record OneShotOptions : ISkillOptions
{
}

public static class OneShotDefinition
{
    public static SkillDefinition<OneShotOptions> Create() => new()
    {
        Id = BuiltInSkillIds.OneShot,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff5CD9",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new OneShotOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = OneShot.LoadSkill,
            OnTakeDamage = OneShot.OnTakeDamage,
        },
    };
}
