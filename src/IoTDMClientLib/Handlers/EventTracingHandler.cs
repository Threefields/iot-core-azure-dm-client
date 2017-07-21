﻿/*
Copyright 2017 Microsoft
Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
and associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using DMDataContract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Devices.Management.Message;
using System;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation.Diagnostics;

namespace Microsoft.Devices.Management
{
    class EventTracingHandler : IClientPropertyHandler
    {
        const string JsonSectionName = "eventTracingCollectors";
        const string JsonReportProperties = "reportProperties";
        const string JsonApplyProperties = "applyProperties";
        const string JsonYesString = "yes";
        const string JsonNoString = "no";
        const string JsonLogFileFolder = "logFileFolder";
        const string JsonLogFileSizeLimitMB = "logFileSizeLimitMB";
        const string JsonTraceLogFileMode = "traceLogFileMode";
        const string JsonStarted = "started";
        const string JsonType = "type";
        const string JsonProvider = "provider";
        const string JsonTraceLevel = "traceLevel";
        const string JsonKeywords = "keyWords";
        const string JsonEnabled = "enabled";
        const string JsonReportLevelDetails = "detailed";
        const string JsonReportLevelMinimal = "none";
        const string JsonReportLevelNone = "none";
        const string JsonRefreshing = "refreshing";
        const string JsonQuery = "?";
        const string JsonDefaultTraceLevel = "critical";

        public EventTracingHandler(IClientHandlerCallBack callback, ISystemConfiguratorProxy systemConfiguratorProxy, JObject desiredCache)
        {
            _systemConfiguratorProxy = systemConfiguratorProxy;
            _callback = callback;
            _desiredCache = desiredCache;
        }

        // IClientPropertyHandler
        public string PropertySectionName
        {
            get
            {
                return JsonSectionName;
            }
        }

        private ProviderConfiguration TryReadProviderFromJson(JProperty cspProperty)
        {
            Logger.Log("Reading provider desired configuration...", LoggingLevel.Information);

            Guid providerGuid;
            if (!Guid.TryParse(cspProperty.Name, out providerGuid))
            {
                return null;
            }

            if (!(cspProperty.Value is JObject))
            {
                return null;
            }

            JObject jCSPProvider = (JObject)cspProperty.Value;
            if ((string)jCSPProvider.GetValue(JsonType) != JsonProvider)
            {
                return null;
            }

            Logger.Log("Provider name: " + cspProperty.Name, LoggingLevel.Information);

            ProviderConfiguration provider = new ProviderConfiguration();
            provider.Guid = cspProperty.Name;
            provider.TraceLevel = Utils.GetString(jCSPProvider, JsonTraceLevel, JsonDefaultTraceLevel);
            provider.Keywords = Utils.GetString(jCSPProvider, JsonKeywords, "");
            provider.Enabled = Utils.GetString(jCSPProvider, JsonEnabled, JsonNoString) == JsonYesString;
            return provider;
        }

        private CollectorCSPConfiguration CSPConfigurationFromJson(JObject jCSPProperties)
        {
            Logger.Log("Reading collector CSP desired configuration...", LoggingLevel.Information);

            CollectorCSPConfiguration cspConfiguration = new CollectorCSPConfiguration();
            foreach (JToken cspToken in jCSPProperties.Children())
            {
                if (!(cspToken is JProperty))
                {
                    continue;
                }

                JProperty cspProperty = (JProperty)cspToken;
                if (cspProperty.Name == JsonLogFileFolder)
                {
                    cspConfiguration.LogFileFolder = (string)cspProperty.Value;
                }
                else if (cspProperty.Name == JsonLogFileSizeLimitMB)
                {
                    cspConfiguration.LogFileSizeLimitMB = (int)cspProperty.Value;
                }
                else if (cspProperty.Name == JsonTraceLogFileMode)
                {
                    cspConfiguration.TraceLogFileMode = (string)cspProperty.Value;
                }
                else if (cspProperty.Name == JsonStarted)
                {
                    cspConfiguration.Started = (string)cspProperty.Value;
                }
                else
                {
                    ProviderConfiguration provider = TryReadProviderFromJson(cspProperty);
                    if (provider != null)
                    {
                        cspConfiguration.Providers.Add(provider);
                    }
                    // We don't throw errors here because we only read the node
                    // we know about. Extra nodes are allowed.
                }
            }
            return cspConfiguration;
        }

        private CollectorDesiredConfiguration CollectorFromJson(string name, JObject properties)
        {
            Logger.Log("Reading collector desired configuration (" + name + ")...", LoggingLevel.Information);

            CollectorDesiredConfiguration collector = new CollectorDesiredConfiguration();
            collector.Name = name;

            // Set device twin control properties...
            collector.ReportToDeviceTwin = Utils.GetString(properties, JsonReportProperties, JsonNoString);

            JToken jApplyProperties = Utils.GetJToken(properties, JsonApplyProperties);
            if (!(jApplyProperties is JObject))
            {
                collector.ApplyFromDeviceTwin = JsonNoString;
                return collector;
            }
            collector.ApplyFromDeviceTwin = JsonYesString;

            // Set csp properties...
            collector.CSPConfiguration = CSPConfigurationFromJson((JObject)jApplyProperties);
            return collector;
        }

        private async Task NullifyReported()
        {
            Logger.Log("Nullify " + PropertySectionName + "...", LoggingLevel.Information);
            await _callback.ReportPropertiesAsync(JsonSectionName, new JValue(JsonRefreshing));
        }

        private SetEventTracingConfigurationRequest SetRequestFromJson(JObject eventTracingCollectorsNode, out string generalReportLevel)
        {
            Logger.Log("Building native request from desired json...", LoggingLevel.Information);

            generalReportLevel = JsonDefaultTraceLevel;

            SetEventTracingConfigurationRequest request = new SetEventTracingConfigurationRequest();

            foreach (JToken jToken in eventTracingCollectorsNode.Children())
            {
                if (!(jToken is JProperty))
                {
                    continue;
                }
                JProperty collectorNode = (JProperty)jToken;

                // Check if it is query ("?")...
                if (collectorNode.Name == JsonQuery &&
                    collectorNode.Value is JValue && collectorNode.Value.Type == JTokenType.String)
                {
                    generalReportLevel = collectorNode.Value.ToString();
                }
                else if (collectorNode.Value is JObject)
                {
                    CollectorDesiredConfiguration collector = CollectorFromJson(collectorNode.Name, (JObject)collectorNode.Value);
                    request.Collectors.Add(collector);
                }
            }

            return request;
        }

        private async Task<string> SetEventTracingConfiguration(JObject desiredValue)
        {
            string generalReportLevel = JsonDefaultTraceLevel;
            SetEventTracingConfigurationRequest request = SetRequestFromJson((JObject)desiredValue, out generalReportLevel);

            // Send the request...
            if (request.Collectors.Count != 0)
            {
                Logger.Log("Sending request...", LoggingLevel.Information);

                var setResponse = await _systemConfiguratorProxy.SendCommandAsync(request) as Message.StringResponse;
                if (setResponse.Status != ResponseStatus.Success)
                {
                    throw new Error((int)setResponse.Status, setResponse.Response);
                }
            }
            return generalReportLevel;
        }

        private string ReportedFromResponse(GetEventTracingConfigurationResponse response, string generalReportLevel)
        {
            Logger.Log("Building diagnostic logs reported properties...", LoggingLevel.Information);

            JsonObject jEventTracingObject = new JsonObject();
            foreach (CollectorReportedConfiguration collector in response.Collectors)
            {
                if (collector.ReportToDeviceTwin == DMJSonConstants.YesString ||
                    generalReportLevel == JsonReportLevelDetails)
                {
                    JsonObject jCollectorObject = new JsonObject();
                    collector.CSPConfiguration.ToJsonObject(jCollectorObject);
                    jEventTracingObject[collector.Name] = jCollectorObject;
                }
                else if (generalReportLevel == JsonReportLevelMinimal)
                {
                    jEventTracingObject[collector.Name] = JsonValue.CreateStringValue("");
                }
            }

            return jEventTracingObject.Stringify();
        }

        private async Task<string> GetEventTracingConfiguration(string generalReportLevel)
        {
            Logger.Log("Retrieving diagnostic logs configurations...", LoggingLevel.Information);

            GetEventTracingConfigurationRequest getRequest = new GetEventTracingConfigurationRequest();
            var getResponse = await _systemConfiguratorProxy.SendCommandAsync(getRequest);
            if (getResponse.Status != ResponseStatus.Success)
            {
                var stringResponse = getResponse as StringResponse;
                throw new Error((int)stringResponse.Status, stringResponse.Response);
            }

            return ReportedFromResponse(getResponse as GetEventTracingConfigurationResponse, generalReportLevel);
        }

        private async Task OnDesiredPropertyChangeAsync(JToken desiredValue)
        {
            Logger.Log("Processing desired properties for section: " + PropertySectionName + "...", LoggingLevel.Information);

            if (!(desiredValue is JObject))
            {
                return;
            }

            _generalReportLevel = await SetEventTracingConfiguration((JObject)desiredValue);

            string responseJsonString = await GetEventTracingConfiguration(_generalReportLevel);

            await NullifyReported();

            await _callback.ReportPropertiesAsync(JsonSectionName, JObject.Parse(responseJsonString));
        }

        private void UpdateCache(JToken desiredValue)
        {
            // Removal (with or without caching/merging) requires explicitly specified state.
            JToken cachedToken = _desiredCache.SelectToken(JsonSectionName);
            if (cachedToken != null)
            {
                if (cachedToken is JObject)
                {
                    JObject cachedObject = (JObject)cachedToken;
                    cachedObject.Merge(desiredValue);
                }
            }
            else
            {
                _desiredCache[JsonSectionName] = desiredValue;
            }
        }

        // IClientPropertyHandler
        public void OnDesiredPropertyChange(JToken desiredValue)
        {
            UpdateCache(desiredValue);

            // Need to revisit all the desired nodes (not only the changed ones) 
            // so that we can re-construct the correct reported list.
            OnDesiredPropertyChangeAsync(_desiredCache[JsonSectionName]);
        }

        // IClientPropertyHandler
        public async Task<JObject> GetReportedPropertyAsync()
        {
            string responseJsonString = await GetEventTracingConfiguration(_generalReportLevel);

            return (JObject)JsonConvert.DeserializeObject(responseJsonString);
        }

        private ISystemConfiguratorProxy _systemConfiguratorProxy;
        private IClientHandlerCallBack _callback;
        private JObject _desiredCache;
        private string _generalReportLevel;
    }
}
