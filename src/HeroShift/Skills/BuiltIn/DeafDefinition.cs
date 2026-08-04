using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record DeafOptions : ISkillOptions
{
}

public static class DeafDefinition
{
    public static SkillDefinition<DeafOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Deaf,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#dae01f",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new DeafOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Deaf.LoadSkill,
            EnableSkill = Deaf.EnableSkill,
            DisableSkill = Deaf.DisableSkill,
            TypeSkill = Deaf.TypeSkill,
            OnTick = Deaf.OnTick,
            NewRound = Deaf.NewRound,
            PlayerMakeSound = Deaf.PlayerMakeSound,
            PlayerDeath = Deaf.PlayerDeath,
            PlayerDisconnect = Deaf.PlayerDisconnect,
        },
    };
}
