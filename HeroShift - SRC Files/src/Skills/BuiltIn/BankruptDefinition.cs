using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record BankruptOptions : ISkillOptions
{
}

public static class BankruptDefinition
{
    public static SkillDefinition<BankruptOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Bankrupt,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#abab33",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new BankruptOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Bankrupt.LoadSkill,
            EnableSkill = Bankrupt.EnableSkill,
            DisableSkill = Bankrupt.DisableSkill,
            TypeSkill = Bankrupt.TypeSkill,
            OnTick = Bankrupt.OnTick,
            NewRound = Bankrupt.NewRound,
        },
    };
}
