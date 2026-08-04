namespace src.Configuration;

/*
 * ConfigurationSnapshot - an immutable holder swapped atomically on reload
 * (REFACTOR.md section 14). Readers always get either the fully-old or
 * fully-new configuration, never a partially-applied mix; on reload failure
 * the previous valid snapshot is kept and the failure is reported, never
 * silently swallowed.
 */
public sealed class ConfigurationSnapshot(HeroShiftConfiguration configuration)
{
    public HeroShiftConfiguration Configuration { get; } = configuration;
}
