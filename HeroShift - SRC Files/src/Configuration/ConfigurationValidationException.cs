namespace src.Configuration;

/*
 * ConfigurationValidationException - thrown when heroshift.json fails
 * validation. Errors carry the JSON path so operators can find the exact
 * offending field (REFACTOR.md section 14), e.g.:
 *   "skills.dash.options.cooldownSeconds: value must be greater than or equal to 0"
 */
public sealed class ConfigurationValidationException(IReadOnlyList<string> errors)
    : Exception(BuildMessage(errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;

    private static string BuildMessage(IReadOnlyList<string> errors) =>
        "Invalid HeroShift configuration:" + Environment.NewLine + string.Join(Environment.NewLine, errors);
}
