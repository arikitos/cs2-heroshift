using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record InheritanceOptions : ISkillOptions
{
}

public static class InheritanceDefinition
{
    public static SkillDefinition<InheritanceOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Inheritance,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#c9a227",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: true,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Rare),
        DefaultOptions = new InheritanceOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Inheritance.LoadSkill,
            EnableSkill = Inheritance.EnableSkill,
            DisableSkill = Inheritance.DisableSkill,
            TypeSkill = Inheritance.TypeSkill,
            NewRound = Inheritance.NewRound,
            PlayerDeath = Inheritance.PlayerDeath,
            PlayerDisconnect = Inheritance.PlayerDisconnect,
        },
    };
}
