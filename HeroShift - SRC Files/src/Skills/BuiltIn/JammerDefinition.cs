using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record JammerOptions : ISkillOptions
{
}

public static class JammerDefinition
{
    public static SkillDefinition<JammerOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Jammer,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#42f5a7",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new JammerOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Jammer.LoadSkill,
            EnableSkill = Jammer.EnableSkill,
            DisableSkill = Jammer.DisableSkill,
            TypeSkill = Jammer.TypeSkill,
            OnTick = Jammer.OnTick,
            NewRound = Jammer.NewRound,
            PlayerDeath = Jammer.PlayerDeath,
            BotTakeover = Jammer.BotTakeover,
            PlayerDisconnect = Jammer.PlayerDisconnect,
        },
    };
}
