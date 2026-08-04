using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record OnlyHeadOptions : ISkillOptions
{
}

public static class OnlyHeadDefinition
{
    public static SkillDefinition<OnlyHeadOptions> Create() => new()
    {
        Id = BuiltInSkillIds.OnlyHead,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#3c47de",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new OnlyHeadOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = OnlyHead.LoadSkill,
            PlayerHurtPre = OnlyHead.PlayerHurtPre,
        },
    };
}
