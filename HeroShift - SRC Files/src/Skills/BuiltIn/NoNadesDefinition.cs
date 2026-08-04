using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record NoNadesOptions : ISkillOptions
{
}

public static class NoNadesDefinition
{
    public static SkillDefinition<NoNadesOptions> Create() => new()
    {
        Id = BuiltInSkillIds.NoNades,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#a38c1a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new NoNadesOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = NoNades.LoadSkill,
            PlayerHurtPre = NoNades.PlayerHurtPre,
        },
    };
}
