// Modified from VaraniumSharp.Initiator

//The MIT License (MIT)
//
//Copyright (c) 2016 Ninetail Labs
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

// this is currently unused, but there is the possibility of adding telemetry to DataTool via Azure

namespace TankLib {
    public static class Telemetry {
        private static TelemetryHandler _handler;

        public static void Init(string instrumentationKey, string userKey) {
            _handler = new TelemetryHandler(instrumentationKey, userKey);
        }

        public static void SetTelementryEnabled(bool enabled) {
            if (_handler == null) return;
            _handler.TrackTelemetry = enabled;
        }

        #region Wrappers
        /// <summary>
        /// Flushes the in-memory buffer
        /// </summary>
        public static void Flush() {
            _handler.Flush();
        }
        
        /// <summary>
        /// Logging the duration and frequency of calls to external components that your app depends on.
        /// </summary>
        /// <param name="dependencyName">Name of the external dependency</param>
        /// <param name="commandName">Dependency call command name</param>
        /// <param name="startTime">Time when dependency was called</param>
        /// <param name="duration">Time taken by dependency to handle request</param>
        /// <param name="success">Was the call handled successfully</param>
        public static void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime,
            TimeSpan duration, bool success) {
            _handler?.TrackDependency(dependencyName, commandName, startTime, duration, success);
        }

        /// <summary>
        /// User actions and other events. Used to track user behavior or to monitor performance.
        /// </summary>
        /// <param name="name">Name of the event</param>
        /// <param name="properties">Dictionary of event properties</param>
        /// <param name="metrics">Dictionary of event metrics</param>
        public static void TrackEvent(string name, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null) {
            _handler?.TrackEvent(name, properties, metrics);
        }

        /// <summary>
        /// Logging exceptions for diagnosis. Trace where they occur in relation to other events and examine stack traces.
        /// </summary>
        /// <param name="exception">Exception that occured</param>
        /// <param name="properties">Named string values that can be used to search for exception</param>
        /// <param name="metrics">Additional values associated with exception</param>
        public static void TrackException(Exception exception, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null) {
            _handler?.TrackException(exception, properties, metrics);
        }

        /// <summary>
        /// Performance measurements such as queue lengths not related to specific events.
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Metric value</param>
        public static void TrackMetric(string name, double value) {
            _handler?.TrackMetric(name, value);
        }

        /// <summary>
        /// Pages, screens, blades, or forms.
        /// </summary>
        /// <param name="name">Name of the page</param>
        public static void TrackPageView(string name) {
            _handler?.TrackPageView(name);
        }

        /// <summary>
        /// Logging the frequency and duration of server requests for performance analysis.
        /// </summary>
        /// <param name="name">Request name</param>
        /// <param name="startTime">Time when request was started</param>
        /// <param name="duration">Time taken by application to handle request</param>
        /// <param name="responseCode">Response status code</param>
        /// <param name="success">True if request was handled successfully</param>
        public static void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode,
            bool success) {
            _handler?.TrackRequest(name, startTime, duration, responseCode, success);
        }

        /// <summary>
        /// Diagnostic log messages. You can also capture third-party logs.
        /// </summary>
        /// <param name="message">Trace message to capture</param>
        public static void TrackTrace(string message) {
            _handler?.TrackTrace(message);
        }

        /// <summary>
        /// Diagnostic log messages. You can also capture third-party logs.
        /// </summary>
        /// <param name="message">Trace message to capture</param>
        /// <param name="severityLevel">Severity of the trace message</param>
        public static void TrackTrace(string message, SeverityLevel severityLevel) {
            _handler?.TrackTrace(message, severityLevel);
        }
        #endregion
    }
    
    public class TelemetryHandler {
        public bool TrackTelemetry;
        public bool IsInitialized;
        private readonly TelemetryClient _telemetryClient;
        private static readonly SemaphoreSlim StartupLock = new SemaphoreSlim(1);

        public TelemetryHandler(string instrumentationKey, string userKey) {
            try {
                StartupLock.Wait();
                if (IsInitialized) {
                    //LogInstance.Warning("Client can only be initialized once");
                    return;
                }
            
                _telemetryClient = new TelemetryClient {
                    InstrumentationKey = instrumentationKey
                };
            
                _telemetryClient.Context.User.Id = userKey;
                _telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
                _telemetryClient.Context.Device.OperatingSystem = GetWindowsFriendlyName();
                _telemetryClient.Context.Device.Model = GetDeviceModel();
                _telemetryClient.Context.Device.OemName = GetDeviceManufacturer();
                _telemetryClient.Context.Component.Version = GetComponentVersion();
                IsInitialized = true;
            } finally {
                StartupLock.Release();
            }
        }
        
        /// <summary>
        /// Flushes the in-memory buffer
        /// </summary>
        public void Flush() {
            _telemetryClient.Flush();
        }
        
        /// <summary>
        /// Check if we can post Telemetry data.
        /// This method checks if the client has been initialized and if posting of telemetry data is allowed
        /// </summary>
        /// <returns>True - Telemetry data can be posted</returns>
        private bool TelemetryCanBePosted() {
            return IsInitialized && TrackTelemetry;
        }

        #region Info
        /// <summary>Get the version of the Assembly</summary>
        /// <returns>Assembly version number</returns>
        public static string GetComponentVersion() {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            return ((object) entryAssembly != null ? entryAssembly.GetName().Version.ToString() : null) ?? "0.0.0.0";
        }

        /// <summary>
        ///     Get the device manufacturer from Management Information
        /// </summary>
        /// <returns>Device manufacturer if it can be found</returns>
        public static string GetDeviceManufacturer() {
            return RetrieveValueFromManagementInformation("Manufacturer", "Win32_ComputerSystem", "Unknown");
        }

        /// <summary>Get the device Model from Management Information</summary>
        /// <returns>Device manufacturer if it can be found</returns>
        public static string GetDeviceModel() {
            return RetrieveValueFromManagementInformation("Model", "Win32_ComputerSystem", "Unknown");
        }

        /// <summary>
        ///     Retrieve the Windows friendly name instead of just a version
        /// </summary>
        /// <returns></returns>
        public static string GetWindowsFriendlyName() {
            return RetrieveValueFromManagementInformation("Caption", "Win32_OperatingSystem",
                Environment.OSVersion.ToString());
        }

        /// <summary>Retrieve an entry from ManagementInformation</summary>
        /// <param name="propertyToRetrieve">The property to retrieve</param>
        /// <param name="componentFromWhichToRetrieve">The component from which the value should be retrieved</param>
        /// <param name="fallbackValue">Value to return if property cannot be found</param>
        /// <returns>Result from the ManagementInformation query</returns>
        private static string RetrieveValueFromManagementInformation(string propertyToRetrieve,
            string componentFromWhichToRetrieve, string fallbackValue) {
            object obj =
                new ManagementObjectSearcher(
                        $"SELECT {(object) propertyToRetrieve} FROM {(object) componentFromWhichToRetrieve}").Get()
                    .OfType<ManagementObject>().Select(
                        x => x.GetPropertyValue(propertyToRetrieve)).FirstOrDefault();
            return obj?.ToString() ?? fallbackValue;
        }
        #endregion

        #region Tracking Methods
        /// <summary>
        /// Logging the duration and frequency of calls to external components that your app depends on.
        /// </summary>
        /// <param name="dependencyName">Name of the external dependency</param>
        /// <param name="commandName">Dependency call command name</param>
        /// <param name="startTime">Time when dependency was called</param>
        /// <param name="duration">Time taken by dependency to handle request</param>
        /// <param name="success">Was the call handled successfully</param>
        public void TrackDependency(string dependencyName, string commandName, DateTimeOffset startTime,
            TimeSpan duration, bool success) {
            if (TelemetryCanBePosted()) {
                _telemetryClient.TrackDependency(dependencyName, commandName, startTime, duration, success);
            }
        }

        /// <summary>
        /// User actions and other events. Used to track user behavior or to monitor performance.
        /// </summary>
        /// <param name="name">Name of the event</param>
        /// <param name="properties">Dictionary of event properties</param>
        /// <param name="metrics">Dictionary of event metrics</param>
        public void TrackEvent(string name, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null) {
            if (TelemetryCanBePosted()) {
                _telemetryClient.TrackEvent(name, properties, metrics);
            }
        }

        /// <summary>
        /// Logging exceptions for diagnosis. Trace where they occur in relation to other events and examine stack traces.
        /// </summary>
        /// <param name="exception">Exception that occured</param>
        /// <param name="properties">Named string values that can be used to search for exception</param>
        /// <param name="metrics">Additional values associated with exception</param>
        public void TrackException(Exception exception, IDictionary<string, string> properties = null,
            IDictionary<string, double> metrics = null) {
            if (TelemetryCanBePosted()) {
                _telemetryClient.TrackException(exception, properties, metrics);
            }
        }

        /// <summary>
        /// Performance measurements such as queue lengths not related to specific events.
        /// </summary>
        /// <param name="name">Metric name</param>
        /// <param name="value">Metric value</param>
        public void TrackMetric(string name, double value) {
            if (TelemetryCanBePosted()) {
                _telemetryClient.TrackMetric(name, value);
            }
        }

        /// <summary>
        /// Pages, screens, blades, or forms.
        /// </summary>
        /// <param name="name">Name of the page</param>
        public void TrackPageView(string name) {
            if (TelemetryCanBePosted()) {
                _telemetryClient.TrackPageView(name);
            }
        }

        /// <summary>
        /// Logging the frequency and duration of server requests for performance analysis.
        /// </summary>
        /// <param name="name">Request name</param>
        /// <param name="startTime">Time when request was started</param>
        /// <param name="duration">Time taken by application to handle request</param>
        /// <param name="responseCode">Response status code</param>
        /// <param name="success">True if request was handled successfully</param>
        public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode,
            bool success) {
            if (TelemetryCanBePosted()) {
                _telemetryClient.TrackRequest(name, startTime, duration, responseCode, success);
            }
        }

        /// <summary>
        /// Diagnostic log messages. You can also capture third-party logs.
        /// </summary>
        /// <param name="message">Trace message to capture</param>
        public void TrackTrace(string message) {
            if (TelemetryCanBePosted()) {
                _telemetryClient.TrackTrace(message);
            }
        }

        /// <summary>
        /// Diagnostic log messages. You can also capture third-party logs.
        /// </summary>
        /// <param name="message">Trace message to capture</param>
        /// <param name="severityLevel">Severity of the trace message</param>
        public void TrackTrace(string message, SeverityLevel severityLevel) {
            if (TelemetryCanBePosted()) {
                _telemetryClient.TrackTrace(message, severityLevel);
            }
        }
        #endregion
    }
}