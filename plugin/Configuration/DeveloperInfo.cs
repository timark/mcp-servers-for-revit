using Newtonsoft.Json;

namespace revit_mcp_plugin.Configuration
{
    /// <summary>
    /// Developer information.
    /// </summary>
    public class DeveloperInfo
    {
        /// <summary>
        /// Developer name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// Developer e-mail address.
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; } = "";

        /// <summary>
        /// Developer website.
        /// </summary>
        [JsonProperty("website")]
        public string Website { get; set; } = "";

        /// <summary>
        /// Developer Organization.
        /// </summary>
        [JsonProperty("organization")]
        public string Organization { get; set; } = "";
    }
}
