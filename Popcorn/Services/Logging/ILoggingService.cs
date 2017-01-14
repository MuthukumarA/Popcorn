using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Popcorn.Services.Logging
{
    /// <summary>
    /// The logging service
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Track an event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        void TrackEvent(string eventName);

        /// <summary>
        /// Track an event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="properties">Event properties</param>
        void TrackEvent(string eventName, Dictionary<string, string> properties);

        /// <summary>
        /// Track a metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        void TrackMetric(string metricName, double value);

        /// <summary>
        /// Track a metric
        /// </summary>
        /// <param name="metricName">Name of the metric</param>
        /// <param name="value">Value of the metric</param>
        /// <param name="properties">Metric properties</param>
        void TrackMetric(string metricName, double value, Dictionary<string, string> properties);

        /// <summary>
        /// Track a page view
        /// </summary>
        /// <param name="pageName">Page name</param>
        void TrackPageView(string pageName);

        /// <summary>
        /// Track a trace
        /// </summary>
        /// <param name="message">Message to trace</param>
        void TrackTrace(string message);

        /// <summary>
        /// Track a trace
        /// </summary>
        /// <param name="message">Message to trace</param>
        /// <param name="properties">Properties of the trace</param>
        void TrackTrace(string message, Dictionary<string, string> properties);
    }
}
