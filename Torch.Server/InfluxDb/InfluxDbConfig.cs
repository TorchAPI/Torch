using System.Xml.Serialization;

namespace Torch.Server.InfluxDb
{
    /// <summary>
    /// Configuration for InfluxDB integration.
    /// </summary>
    public class InfluxDbConfig
    {
        /// <summary>
        /// Endpoint IP:port of the InfluxDB instance.
        /// </summary>
        [XmlElement]
        public string DbHost { get; set; }

        /// <summary>
        /// Bucket name of the InfluxDB instance.
        /// </summary>
        [XmlElement]
        public string Bucket { get; set; }

        /// <summary>
        /// Organization name of the InfluxDB instance.
        /// </summary>
        [XmlElement]
        public string Organization { get; set; }

        /// <summary>
        /// Token to authenticate into the InfluxDB instance (if any).
        /// </summary>
        [XmlElement]
        public string Token { get; set; }
        
        /// <summary>
        /// Default configuration.
        /// </summary>
        public static InfluxDbConfig Default => new InfluxDbConfig
        {
            DbHost = "http://localhost:8086",
            Bucket = "test",
            Organization = "foo",
            Token = "token",
        };
    }
}