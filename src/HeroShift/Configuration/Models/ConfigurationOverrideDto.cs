using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace src.Configuration.Models;

/*
 * ConfigurationOverrideDto - the raw shape of heroshift.json. Every field is
 * nullable/optional: a field absent from the file means "keep the code
 * default", which is why this is a plain DTO rather than HeroShiftConfiguration
 * itself (that type's properties all have non-null defaults, so it can't
 * distinguish "operator set this to the default value" from "operator didn't
 * mention this field" the way ConfigurationLoader's merge needs to).
 *
 * All nested objects mirror the typed Options records in this namespace but
 * with every property optional, matching the documented architecture's example
 * heroshift.json (override-only, no need to restate defaults).
 */
public sealed class ConfigurationOverrideDto
{
    [JsonProperty("schemaVersion")]
    public int? SchemaVersion { get; set; }

    [JsonProperty("general")]
    public JObject? General { get; set; }

    [JsonProperty("hud")]
    public JObject? Hud { get; set; }

    [JsonProperty("chat")]
    public JObject? Chat { get; set; }

    [JsonProperty("commands")]
    public JObject? Commands { get; set; }

    [JsonProperty("voting")]
    public JObject? Voting { get; set; }

    [JsonProperty("skills")]
    public JObject? Skills { get; set; }
}
