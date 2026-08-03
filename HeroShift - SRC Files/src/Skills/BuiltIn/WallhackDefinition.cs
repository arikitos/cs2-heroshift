using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record WallhackOptions : ISkillOptions
{
}

public static class WallhackDefinition
{
    public static SkillDefinition<WallhackOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Wallhack,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#5d00ff",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 1,
            Rarity: global::src.utils.Rarity.Epic),
        DefaultOptions = new WallhackOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Wallhack.LoadSkill,
            EnableSkill = Wallhack.EnableSkill,
            DisableSkill = Wallhack.DisableSkill,
            CheckTransmit = Wallhack.CheckTransmit,
            NewRound = Wallhack.NewRound,
            PlayerDeath = Wallhack.PlayerDeath,
        },
    };
}
