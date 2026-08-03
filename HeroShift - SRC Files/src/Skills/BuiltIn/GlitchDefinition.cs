using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record GlitchOptions : ISkillOptions
{
}

public static class GlitchDefinition
{
    public static SkillDefinition<GlitchOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Glitch,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#f542ef",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new GlitchOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Glitch.LoadSkill,
            EnableSkill = Glitch.EnableSkill,
            DisableSkill = Glitch.DisableSkill,
            TypeSkill = Glitch.TypeSkill,
            OnTick = Glitch.OnTick,
            NewRound = Glitch.NewRound,
            PlayerDeath = Glitch.PlayerDeath,
            BotTakeover = Glitch.BotTakeover,
        },
    };
}
