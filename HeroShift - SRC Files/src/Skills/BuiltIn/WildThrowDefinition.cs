using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record WildThrowOptions : ISkillOptions
{
}

public static class WildThrowDefinition
{
    public static SkillDefinition<WildThrowOptions> Create() => new()
    {
        Id = BuiltInSkillIds.WildThrow,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#384728",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new WildThrowOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = WildThrow.LoadSkill,
            EnableSkill = WildThrow.EnableSkill,
            DisableSkill = WildThrow.DisableSkill,
            TypeSkill = WildThrow.TypeSkill,
            OnEntitySpawned = WildThrow.OnEntitySpawned,
            OnTick = WildThrow.OnTick,
            NewRound = WildThrow.NewRound,
            PlayerDeath = WildThrow.PlayerDeath,
        },
    };
}
