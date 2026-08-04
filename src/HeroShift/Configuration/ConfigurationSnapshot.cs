namespace src.Configuration;

/*
 * Immutable configuration holder swapped atomically on reload. Readers always
 * see either the complete previous snapshot or the complete new snapshot.
 */
public sealed class ConfigurationSnapshot(HeroShiftConfiguration configuration)
{
    public HeroShiftConfiguration Configuration { get; } = configuration;
}
