/*
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Microsoft.Devices.Management
{
    class EventTracingHandler : IClientPropertyHandler
    {
        const string JsonSectionName = "eventTracingCollectors";

        public EventTracingHandler(IClientHandlerCallBack callback, ISystemConfiguratorProxy systemConfiguratorProxy, JObject desiredCache)
        {
            this._systemConfiguratorProxy = systemConfiguratorProxy;
            this._callback = callback;
            this._desiredCache = desiredCache;
        }

        // IClientPropertyHandler
        public string PropertySectionName
        {
            get
            {
                return JsonSectionName; // todo: constant in data contract?
            }
        }

        private void PopulateCollector(CollectorDesiredConfiguration collector, JObject properties)
        {
            // Read device twin control properties...
            collector.ReportToDeviceTwin = (string)properties.GetValue("reportProperties");

            JToken jApplyProperties = properties.GetValue("applyProperties");
            if (jApplyProperties is JObject)
            {
                collector.ApplyFromDeviceTwin = "yes";
                JObject jCSPProperties = (JObject)jApplyProperties;
                collector.CSPConfiguration = new CollectorCSPConfiguration();
                foreach (JToken cspToken in jCSPProperties.Children())
                {
                    if (!(cspToken is JProperty))
                    {
                        continue;
                    }

                    JProperty cspProperty = (JProperty)cspToken;
                    if (cspProperty.Name == "logFileFolder")
                    {
                        collector.CSPConfiguration.LogFileFolder = (string)cspProperty.Value;
                    }
                    else if (cspProperty.Name == "logFileSizeLimitMB")
                    {
                        collector.CSPConfiguration.LogFileSizeLimitMB = (int)cspProperty.Value;
                    }
                    else if (cspProperty.Name == "traceLogFileMode")
                    {
                        collector.CSPConfiguration.TraceLogFileMode = (string)cspProperty.Value;
                    }
                    else if (cspProperty.Name == "started")
                    {
                        collector.CSPConfiguration.Started = (string)cspProperty.Value;
                    }
                    else
                    {
                        // must be a provider's guid...
                        Guid providerGuid;
                        if (!Guid.TryParse(cspProperty.Name, out providerGuid))
                        {
                            throw new Error(-1, "Provider's identity must be a guid!");
                        }

                        if (!(cspProperty.Value is JObject))
                        {
                            throw new Error(-2, "Provider's definition must be a json object!");
                        }

                        ProviderConfiguration provider = new ProviderConfiguration();
                        provider.Guid = cspProperty.Name;

                        JObject jCSPProvider = (JObject)cspProperty.Value;
                        if ((string)jCSPProvider.GetValue("type") != "provider")
                        {
                            throw new Error(-3, "Expected provider type!");
                        }

                        provider.TraceLevel = (string)jCSPProvider.GetValue("traceLevel");
                        provider.Keywords = (string)jCSPProvider.GetValue("keyWords");
                        provider.Enabled = (string)jCSPProvider.GetValue("enabled") == "yes";

                        collector.CSPConfiguration.Providers.Add(provider);
                    }
                }
            }
            else
            {
                collector.ApplyFromDeviceTwin = "no";
            }
        }

        private async Task NullifyReported()
        {
            Debug.WriteLine("NullifyReported\n");
            await this._callback.ReportPropertiesAsync(JsonSectionName, new JValue("refreshing"));
        }

        private async Task OnDesiredPropertyChangeAsync(JToken desiredValue)
        {
            if (!(desiredValue is JObject))
            {
                return;
            }
            string generalReportLevel = "none";

            // A list of collectors
            JObject eventTracingCollectorsNode = (JObject)desiredValue;

            SetEventTracingConfigurationRequest request = new SetEventTracingConfigurationRequest();

            // Convert jason to request...
            foreach (JToken jToken in eventTracingCollectorsNode.Children())
            {
                if (!(jToken is JProperty))
                {
                    continue;
                }
                JProperty collectorNode = (JProperty)jToken;

                if (collectorNode.Name == "?" &&
                    collectorNode.Value is JValue && collectorNode.Value.Type == JTokenType.String)
                {
                    generalReportLevel = collectorNode.Value.ToString();
                }
                else if (collectorNode.Value is JObject)
                {
                    CollectorDesiredConfiguration collector = new CollectorDesiredConfiguration();
                    collector.Name = collectorNode.Name;
                    request.Collectors.Add(collector);
                    PopulateCollector(collector, (JObject)collectorNode.Value);
                }
            }

            // Send the request...
            if (request.Collectors.Count != 0)
            {
                var setResponse = await this._systemConfiguratorProxy.SendCommandAsync(request) as Message.StringResponse;
                if (setResponse.Status != ResponseStatus.Success)
                {
                    throw new Error((int)setResponse.Status, setResponse.Response);
                }
            }

            // Get all collectors from the system...
            Message.GetEventTracingConfigurationRequest getRequest = new Message.GetEventTracingConfigurationRequest();
            var getResponse = await this._systemConfiguratorProxy.SendCommandAsync(getRequest);
            if (getResponse.Status != ResponseStatus.Success)
            {
                var stringResponse = getResponse as Message.StringResponse;
                throw new Error((int)stringResponse.Status, stringResponse.Response);
            }

            // Build the reported list...
            JsonObject jEventTracingObject = new JsonObject();
            var eventTracingResponse = getResponse as Message.GetEventTracingConfigurationResponse;
            foreach (CollectorReportedConfiguration collector in eventTracingResponse.Collectors)
            {
                if (collector.ReportToDeviceTwin == DMJSonConstants.YesString ||
                    generalReportLevel == "detailed")
                {
                    JsonObject jCollectorObject = new JsonObject();
                    collector.CSPConfiguration.ToJsonObject(jCollectorObject);
                    jEventTracingObject[collector.Name] = jCollectorObject;
                }
                else if (generalReportLevel == "minimal")
                {
                    jEventTracingObject[collector.Name] = JsonValue.CreateStringValue("");
                }
            }
            string responseJsonString = jEventTracingObject.Stringify();
            Debug.WriteLine(responseJsonString);

            // Report...
            await NullifyReported();

            await _callback.ReportPropertiesAsync(JsonSectionName, JObject.Parse(responseJsonString));
        }


        private void UpdateCache(JToken desiredValue)
        {
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
            // ToDo: we need to use the cached status to know what to report back.
            return (JObject)JsonConvert.DeserializeObject("{ \"state\" : \"not implemented\" }");
        }

        private ISystemConfiguratorProxy _systemConfiguratorProxy;
        private IClientHandlerCallBack _callback;
        private JObject _desiredCache;
    }
}
