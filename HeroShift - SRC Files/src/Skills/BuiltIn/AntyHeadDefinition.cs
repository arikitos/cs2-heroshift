using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record AntyHeadOptions : ISkillOptions
{
}

public static class AntyHeadDefinition
{
    public static SkillDefinition<AntyHeadOptions> Create() => new()
    {
        Id = BuiltInSkillIds.AntyHead,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#8B4513",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new AntyHeadOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = AntyHead.LoadSkill,
            PlayerHurtPre = AntyHead.PlayerHurtPre,
        },
    };
}
