using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record DarknessOptions : ISkillOptions
{
    public int R { get; init; } = 0;
    public int G { get; init; } = 0;
    public int B { get; init; } = 0;
    public int A { get; init; } = 230;
}

public static class DarknessDefinition
{
    public static SkillDefinition<DarknessOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Darkness,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#383838",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Rare),
        DefaultOptions = new DarknessOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Darkness.LoadSkill,
            EnableSkill = Darkness.EnableSkill,
            DisableSkill = Darkness.DisableSkill,
            TypeSkill = Darkness.TypeSkill,
            OnTick = Darkness.OnTick,
            NewRound = Darkness.NewRound,
            PlayerDeath = Darkness.PlayerDeath,
            BotTakeover = Darkness.BotTakeover,
        },
    };
}
