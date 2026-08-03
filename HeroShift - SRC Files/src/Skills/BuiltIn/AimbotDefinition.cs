using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record AimbotOptions : ISkillOptions
{
}

public static class AimbotDefinition
{
    public static SkillDefinition<AimbotOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Aimbot,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ff0000",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new AimbotOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Aimbot.LoadSkill,
            OnTakeDamage = Aimbot.OnTakeDamage,
            OnTakeDamagePost = Aimbot.OnTakeDamagePost,
        },
    };
}
