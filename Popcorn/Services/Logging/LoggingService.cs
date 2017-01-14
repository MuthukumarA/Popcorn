using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Popcorn.Helpers;

namespace Popcorn.Services.Logging
{
    /// <summary>
    /// The logging service
    /// </summary>
    public class LoggingService : ILoggingService
    {
        /// <summary>
        /// <see cref="TelemetryClient"/>
        /// </summary>
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initialize <see cref="LoggingService"/>
        /// </summary>
        public LoggingService()
        {
            _telemetryClient = new TelemetryClient(new TelemetryConfiguration
            {
                InstrumentationKey = Constants.ApplicationInsightsInstrumentationKey
            });
        }

        /// <summary>
        /// Track an event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        public void TrackEvent(string eventName)
        {
            _telemetryClient.TrackEvent(eventName);
        }

        /// <summary>
        /// Track an event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="properties">Event properties</param>
        public void TrackEvent(string eventName, Dictionary<string, string> properties)
        {
            _telemetryClient.TrackEvent(eventName, properties);
        }

        /// <summary>
        /// Track a metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        public void TrackMetric(string metricName, double value)
        {
            _telemetryClient.TrackMetric(metricName, value);
        }

        /// <summary>
        /// Track a metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        /// <param name="properties">Metric properties</param>
        public void TrackMetric(string metricName, double value, Dictionary<string, string> properties)
        {
            _telemetryClient.TrackMetric(metricName, value, properties);
        }

        /// <summary>
        /// Track a page view
        /// </summary>
        /// <param name="pageName">Page name</param>
        public void TrackPageView(string pageName)
        {
            _telemetryClient.TrackPageView(pageName);
        }

        /// <summary>
        /// Track a trace
        /// </summary>
        /// <param name="message">Message to trace</param>
        public void TrackTrace(string message)
        {
            _telemetryClient.TrackTrace(message);
        }

        /// <summary>
        /// Track a trace
        /// </summary>
        /// <param name="message">Message to trace</param>
        /// <param name="properties">Properties of the trace</param>
        public void TrackTrace(string message, Dictionary<string, string> properties)
        {
            _telemetryClient.TrackTrace(message, properties);
        }
    }
}