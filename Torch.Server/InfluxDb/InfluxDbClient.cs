using System;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace Torch.Server.InfluxDb
{
    /// <summary>
    /// Wrap the endpoint of the InfluxDB instance.
    /// </summary>
    public class InfluxDbClient : IDisposable
    {
        readonly InfluxDbConfig _config;
        readonly InfluxDBClient _influxClient;
        readonly WriteApi _writeApi;

        /// <summary>
        /// Instantiate class.
        /// </summary>
        /// <param name="config">Config for this instance.</param>
        internal InfluxDbClient(InfluxDbConfig config)
        {
            _config = config;

            _influxClient?.Dispose();
            _writeApi?.Dispose();

            _influxClient = InfluxDBClientFactory.Create(
                config.DbHost,
                config.Token.ToCharArray());

            _writeApi = _influxClient.GetWriteApi();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _influxClient.Dispose();
            _writeApi.Dispose();
        }

        /// <summary>
        /// Instantiate a new PointData object with some properties set for convenience.
        /// </summary>
        /// <remarks>Timestamp will be set, but can be overwritten.</remarks>
        /// <param name="measurement">Measurement to write this point to.</param>
        /// <returns>New PointData object.</returns>
        public PointData MakePointIn(string measurement)
        {
            // Note: UTC time is used
            var timestamp = DateTime.UtcNow;

            return PointData
                .Measurement(measurement)
                .Timestamp(timestamp, WritePrecision.S);
        }

        /// <summary>
        /// Write given points to the InfluxDB instance.
        /// </summary>
        /// <remarks>Will NOT throw or log errors when failed. Check the database console to find any issues.</remarks>
        /// <param name="points">List of points to write to the DB instance.</param>
        public void WritePoints(params PointData[] points)
        {
            _writeApi.WritePoints(_config.Bucket, _config.Organization, points);
        }
    }
}