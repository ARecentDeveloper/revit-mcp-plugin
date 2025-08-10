using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Configuration
{
    /// <summary>
    /// Service settings class
    /// </summary>
    public class ServiceSettings
    {
        /// <summary>
        /// Log level
        /// </summary>
        [JsonProperty("logLevel")]
        public string LogLevel { get; set; } = "Info";

        /// <summary>
        /// Socket service port
        /// </summary>
        [JsonProperty("port")]
        public int Port { get; set; } = 8080;

    }
}
