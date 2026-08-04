using CounterStrikeSharp.API.Modules.Utils;
using src.SkillsCore;
using src.player;
using src.utils;

namespace src.utils;

/*
 * Temporary source-compatibility facade for nested SkillConfig classes and a
 * few collection-shaped call sites. It performs no file I/O, reflection or
 * string member access; effective values come from SkillRuntime.
 */
public static class SkillsInfo
{
    public static SkillsInfoModel LoadedConfig => new(SkillRuntime.All.Select(skill =>
        new DefaultSkillInfo(
            skill.LegacySkill,
            skill.Active,
            skill.Color,
            (CsTeam)skill.OnlyTeam,
            skill.DisableOnFreezeTime,
            skill.NeedsTeammates,
            skill.RequiredPermission,
            skill.HudDuration,
            skill.DescriptionHudDuration,
            skill.MaxPerServer,
            Enum.Parse<Rarity>(skill.Rarity))));

    public static SkillsInfoModel LoadSkillsInfo() => LoadedConfig;

    public class DefaultSkillInfo(
        Skills skill = Skills.None,
        bool active = true,
        string color = "#ffffff",
        CsTeam onlyTeam = CsTeam.None,
        bool disableOnFreezeTime = false,
        bool needsTeammates = false,
        string requiredPermission = "",
        float? hudDuration = null,
        float? descriptionHudDuration = null,
        int maxPerServer = -1,
        Rarity rarity = Rarity.Common)
    {
        public string Name { get; set; } = skill.ToString();
        public bool Active { get; set; } = active;
        public string Color { get; set; } = color;
        public int OnlyTeam { get; set; } = (int)onlyTeam;
        public bool DisableOnFreezeTime { get; set; } = disableOnFreezeTime;
        public bool NeedsTeammates { get; set; } = needsTeammates;
        public string RequiredPermission { get; set; } = requiredPermission;
        public float? HudDuration { get; set; } = hudDuration;
        public float? DescriptionHudDuration { get; set; } = descriptionHudDuration;
        public int MaxPerServer { get; set; } = maxPerServer;
        public string Rarity { get; set; } = rarity.ToString();
    }

    public sealed class SkillsInfoModel : List<DefaultSkillInfo>
    {
        public SkillsInfoModel() { }
        public SkillsInfoModel(IEnumerable<DefaultSkillInfo> values) : base(values) { }
        public string Name => "heroshift.json";
    }
}
