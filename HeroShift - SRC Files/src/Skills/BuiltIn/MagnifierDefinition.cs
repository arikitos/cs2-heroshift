using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record MagnifierOptions : ISkillOptions
{
    public uint CustomFOV { get; init; } = 50;
}

public static class MagnifierDefinition
{
    public static SkillDefinition<MagnifierOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Magnifier,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#9ba882",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new MagnifierOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Magnifier.LoadSkill,
            EnableSkill = Magnifier.EnableSkill,
            DisableSkill = Magnifier.DisableSkill,
            TypeSkill = Magnifier.TypeSkill,
            OnTick = Magnifier.OnTick,
            NewRound = Magnifier.NewRound,
            PlayerDeath = Magnifier.PlayerDeath,
            BotTakeover = Magnifier.BotTakeover,
            PlayerDisconnect = Magnifier.PlayerDisconnect,
        },
    };
}
