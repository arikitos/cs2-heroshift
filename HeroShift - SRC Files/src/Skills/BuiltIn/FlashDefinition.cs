using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

public sealed record FlashOptions : ISkillOptions
{
    public float ChanceFrom { get; init; } = 1.2f;
    public float ChanceTo { get; init; } = 3.0f;
}

public static class FlashDefinition
{
    public static SkillDefinition<FlashOptions> Create() => new()
    {
        Id = BuiltInSkillIds.Flash,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#A31912",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new FlashOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = Flash.LoadSkill,
            EnableSkill = Flash.EnableSkill,
            DisableSkill = Flash.DisableSkill,
            OnTick = Flash.OnTick,
            NewRound = Flash.NewRound,
            PlayerMakeSound = Flash.PlayerMakeSound,
            PlayerJump = Flash.PlayerJump,
        },
    };
}
