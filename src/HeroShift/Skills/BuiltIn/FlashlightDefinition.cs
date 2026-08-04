using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record FlashlightOptions : ISkillOptions
{
    public float Cooldown { get; init; } = 2f;
    public int ColorR { get; init; } = 255;
    public int ColorG { get; init; } = 255;
    public int ColorB { get; init; } = 255;
    public float Brightness { get; init; } = 1.5f;
    public float Range { get; init; } = 1200.0f;
    public float BlindDuration { get; init; } = 5f;
    public float BlindAngle { get; init; } = 10.0f;
    public float BlindAlpha { get; init; } = 200;
}

public static class FlashlightDefinition
{
    public static SkillDefinition<FlashlightOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Flashlight,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#a3000b",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: true,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: 2,
            Rarity: global::src.utils.Rarity.Legendary),
        DefaultOptions = new FlashlightOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Flashlight.LoadSkill,
            EnableSkill = Flashlight.EnableSkill,
            DisableSkill = Flashlight.DisableSkill,
            UseSkill = Flashlight.UseSkill,
            OnTick = Flashlight.OnTick,
            NewRound = Flashlight.NewRound,
            RoundEnd = Flashlight.RoundEnd,
            PlayerDeath = Flashlight.PlayerDeath,
        },
    };
}
