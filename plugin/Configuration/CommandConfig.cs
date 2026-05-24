using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;

namespace revit_mcp_plugin.Configuration
{
    /// <summary>
    /// Command configuration class.
    /// </summary>
    public class CommandConfig
    {
        /// <summary>
        /// Name of the command. Corresponds to <see cref="IRevitCommand.CommandName"/>
        /// </summary>
        [JsonProperty("commandName")]
        public string CommandName { get; set; }

        /// <summary>
        /// Assembly path - DLL containing this command.
        /// </summary>
        [JsonProperty("assemblyPath")]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// Enable this command.
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Supported Revit versions.
        /// </summary>
        [JsonProperty("supportedRevitVersions")]
        public string[] SupportedRevitVersions { get; set; } = new string[0];

        /// <summary>
        /// Developer information.
        /// </summary>
        [JsonProperty("developer")]
        public DeveloperInfo Developer { get; set; } = new DeveloperInfo();

        /// <summary>
        /// Command description.
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = "";
    }
}
