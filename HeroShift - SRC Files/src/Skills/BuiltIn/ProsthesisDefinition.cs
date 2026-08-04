using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ProsthesisOptions : ISkillOptions
{
}

public static class ProsthesisDefinition
{
    public static SkillDefinition<ProsthesisOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Prosthesis,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#9c9c9c",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new ProsthesisOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Prosthesis.LoadSkill,
            PlayerHurtPre = Prosthesis.PlayerHurtPre,
        },
    };
}
