namespace src.Configuration;

public sealed record RuntimeConfigurationSnapshot(
    HeroShiftConfiguration Configuration,
    EffectiveSkillConfigurationCollection Skills);
