using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record NightmareOptions : ISkillOptions
{
    public string PostProcessing { get; init; } = "lighting/postprocessing/effects/death_cam_phase1_low_violence.vpost";
    public float FadeTime { get; init; } = .25f;
    public float MinExposure { get; init; } = .5f;
    public float MaxExposure { get; init; } = 2f;
}

public static class NightmareDefinition
{
    public static SkillDefinition<NightmareOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Nightmare,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#5b2c6f",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Rare),
        DefaultOptions = new NightmareOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Nightmare.LoadSkill,
            EnableSkill = Nightmare.EnableSkill,
            DisableSkill = Nightmare.DisableSkill,
            TypeSkill = Nightmare.TypeSkill,
            OnTick = Nightmare.OnTick,
            CheckTransmit = Nightmare.CheckTransmit,
            NewRound = Nightmare.NewRound,
            PlayerDeath = Nightmare.PlayerDeath,
            PlayerDisconnect = Nightmare.PlayerDisconnect,
        },
    };
}
