using src.player.skills;
using src.SkillsCore.Abstractions;

namespace src.SkillsCore.BuiltIn;

/*
 * RichBoyOptions - immutable typed gameplay options
 * tunables. Defaults are transcribed verbatim from the baseline snapshot.
 */
public sealed record RichBoyOptions : ISkillOptions
{
    public int MinMoney { get; init; } = 5000;
    public int MaxMoney { get; init; } = 15000;
}

/*
 * RichBoyDefinition - canonical identity, metadata, typed defaults and hooks
 * for the existing RichBoy gameplay implementation.
 */
public static class RichBoyDefinition
{
    public static SkillDefinition<RichBoyOptions> Create() => new()
    {
        Id = BuiltInSkillIds.RichBoy,
        Metadata = new SkillMetadata(
            Active: true,
            Color: "#D4AF37",
            OnlyTeam: CounterStrikeSharp.API.Modules.Utils.CsTeam.None,
            DisableOnFreezeTime: false,
            NeedsTeammates: false,
            RequiredPermission: "",
            HudDuration: null,
            DescriptionHudDuration: null,
            MaxPerServer: -1,
            Rarity: global::src.utils.Rarity.Common),
        DefaultOptions = new RichBoyOptions(),
        Hooks = new SkillHookSet
        {
            LoadSkill = RichBoy.LoadSkill,
            EnableSkill = RichBoy.EnableSkill,
            DisableSkill = RichBoy.DisableSkill,
        },
    };
}
