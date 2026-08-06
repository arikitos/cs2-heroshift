using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record ChameleonOptions : ISkillOptions
{
}

public static class ChameleonDefinition
{
    public static SkillDefinition<ChameleonOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Chameleon,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#5fd98a",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Rare),
        DefaultOptions = new ChameleonOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Chameleon.LoadSkill,
            EnableSkill = Chameleon.EnableSkill,
            DisableSkill = Chameleon.DisableSkill,
            NewRound = Chameleon.NewRound,
            PlayerDeath = Chameleon.PlayerDeath,
            PlayerDisconnect = Chameleon.PlayerDisconnect,
        },
    };
}
