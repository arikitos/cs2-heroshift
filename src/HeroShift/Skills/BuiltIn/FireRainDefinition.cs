using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record FireRainOptions : ISkillOptions
{
}

public static class FireRainDefinition
{
    public static SkillDefinition<FireRainOptions> Create() => new()
    {
        Id = BuiltInSkillIds.FireRain,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#ffbf47",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Epic),
        DefaultOptions = new FireRainOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = FireRain.LoadSkill,
            EnableSkill = FireRain.EnableSkill,
            OnEntitySpawned = FireRain.OnEntitySpawned,
            NewRound = FireRain.NewRound,
            DecoyStarted = FireRain.DecoyStarted,
        },
    };
}
