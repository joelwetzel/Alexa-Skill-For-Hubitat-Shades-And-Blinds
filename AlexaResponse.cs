using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlexaSkillForHubitatWindowShades
{
    public class AlexaResponse
    {        
        private JObject _jResponse = JObject.Parse("{}");
        
        
        public AlexaResponse() : this("Alexa", "Response")
        {
        }


        public AlexaResponse(string nameSpace, string name, string endpointId = "INVALID", string token = "INVALID", string correlationToken = null)
        {
            JObject jHeader = JObject.Parse("{}");

            jHeader.Add("namespace", CheckValue(nameSpace, "Alexa"));
            jHeader.Add("name", CheckValue(name, "Response"));

            jHeader.Add("messageId", System.Guid.NewGuid());
            jHeader.Add("payloadVersion", "3");

            if (correlationToken != null) {
                jHeader.Add("correlationToken", CheckValue(correlationToken, "INVALID"));
            }
            
            JObject jScope = JObject.Parse("{}");
            jScope.Add("type", "BearerToken");
            jScope.Add("token", CheckValue(token, "INVALID"));

            JObject jEndpoint = JObject.Parse("{}");
            jEndpoint.Add("scope", jScope);
            jEndpoint.Add("endpointId", CheckValue(endpointId, "INVALID"));

            JObject jEvent = JObject.Parse("{}");

            jEvent.Add("header", JToken.FromObject(jHeader));

            // No endpoint in an AcceptGrant or Discover request
            if (name != "AcceptGrant.Response" && name != "Discover.Response")
            {
                jEvent.Add("endpoint", JToken.FromObject(jEndpoint));
            }

            JObject jPayload = JObject.Parse("{}");
            jEvent.Add("payload", JToken.FromObject(jPayload));
            
            _jResponse.Add("event", JToken.FromObject(jEvent));
        }


        public void AddContextProperty(string namespaceValue = "Alexa.EndpointHealth", string name = "connectivity", string value = "{}", string instance = null, int uncertaintyInMilliseconds = 0)
        {
            JObject jContext;
            if (_jResponse["context"] == null)
            {
                jContext = new JObject();
            }
            else
            {
                jContext = JObject.FromObject(_jResponse["context"]);
            }

            JArray properties = new JArray();
            properties.Add(CreateContextProperty(namespaceValue, name, value, instance, uncertaintyInMilliseconds));
            jContext.Add("properties", properties);
            _jResponse.Add("context", jContext);
        }


        public JObject CreateContextProperty(string namespaceValue = "Alexa.EndpointHealth", string name = "connectivity", string value = "{}", string instance = null, int uncertaintyInMilliseconds = 0)
        {
            String valueObject;
            try
            {
                valueObject = JObject.Parse(value).ToString();
            }
            catch (JsonReaderException)
            {
                valueObject = value;
            }

            JObject jProperty = new JObject();
            jProperty.Add("namespace", namespaceValue);
            jProperty.Add("name", name);
            jProperty.Add("value", valueObject);
            jProperty.Add("timeOfSample", DateTime.UtcNow);
            jProperty.Add("uncertaintyInMilliseconds", uncertaintyInMilliseconds);

            if (instance != null)
            {
                jProperty.Add("instance", instance);
            }

            return jProperty;
        }

        
        public void AddCookie(string key, string value)
        {
            JObject jEndpoint = JObject.FromObject(_jResponse["event"]["endpoint"]);
            JToken cookie = jEndpoint["cookie"];

            if (cookie != null)
            {
                jEndpoint["cookie"][key] = value;
            }
            else
            {
                string cookieString = string.Format("{{\"{0}\": \"{1}\"}}", key, value);
                jEndpoint.Add("cookie", JToken.Parse(cookieString));                
            }
            
            _jResponse["event"]["endpoint"] = jEndpoint;
        }


        public void AddPayloadEndpoint(string endpointId, string friendlyName, string capabilities)
        {
            JObject jPayload = JObject.FromObject(_jResponse["event"]["payload"]);

            bool hasEndpoints = jPayload.TryGetValue("endpoints", out var endpointsToken);

            if (hasEndpoints)
            {
                JArray endpoints = JArray.FromObject(endpointsToken);
                endpoints.Add(CreatePayloadEndpoint(endpointId, friendlyName, capabilities));
                jPayload["endpoints"] = endpoints;
            }
            else
            {
                JArray endpoints = new JArray();
                endpoints.Add(CreatePayloadEndpoint(endpointId, friendlyName, capabilities));
                jPayload.Add("endpoints", endpoints);
            }

            _jResponse["event"]["payload"] = jPayload;
        }


        public JObject CreatePayloadEndpoint(string endpointId, string friendlyName, string capabilities, string cookie = null){
            JObject jEndpoint = new JObject();
            jEndpoint.Add("capabilities", JArray.Parse(capabilities));
            jEndpoint.Add("description", "MQTT Blinds");
            JArray displayCategories = new JArray();
            displayCategories.Add("INTERIOR_BLIND");
            jEndpoint.Add("displayCategories", displayCategories);
            jEndpoint.Add("endpointId", endpointId);
            //endpoint.Add("endpointId", "endpoint_" + new Random().Next(0, 999999).ToString("D6"));
            jEndpoint.Add("friendlyName", friendlyName);
            jEndpoint.Add("manufacturerName", "JoelWetzel");

            if (cookie != null)
                jEndpoint.Add("cookie", JObject.Parse(cookie));

            return jEndpoint;
        }


        public JObject CreatePayloadEndpointCapability(string type="AlexaInterface", string interfaceValue="Alexa", string version="3", JObject properties=null)
        {
            JObject jCapability = new JObject();
            jCapability.Add("type", type);
            jCapability.Add("interface", interfaceValue);
            jCapability.Add("version", version);

            if (properties != null)
            {
                jCapability.Add("properties", properties);
            }

            //jCapability.Add("proactivelyReported", true);
            //jCapability.Add("retrievable", true);

            return jCapability;
        }


        public JObject CreateBlindsCapability()
        {
            JObject jPropertyMode = new JObject();
            jPropertyMode.Add("name", "mode");
            JArray jSupportedArray = new JArray();
            jSupportedArray.Add(jPropertyMode);
            JObject jSupported = new JObject();
            jSupported.Add("supported", jSupportedArray);

            JObject jCapabilityAlexaBlindsController = this.CreatePayloadEndpointCapability("AlexaInterface", "Alexa.ModeController", "3", jSupported);
            jCapabilityAlexaBlindsController.Add("instance", "Blinds.Position");

            JObject jCapabilityResources = JObject.Parse(@"{
                                                                        ""friendlyNames"": [
                                                                            {
                                                                            ""@type"": ""asset"",
                                                                            ""value"": {
                                                                                ""assetId"": ""Alexa.Setting.Opening""
                                                                                }
                                                                            }
                                                                        ]
                                                                    }");
            jCapabilityAlexaBlindsController.Add("capabilityResources", jCapabilityResources);

            JObject jConfiguration = JObject.Parse(@"{
                                                                ""ordered"": false,
                                                                ""supportedModes"": [
                                                                  {
                                                                    ""value"": ""Position.Up"",
                                                                    ""modeResources"": {
                                                                      ""friendlyNames"": [
                                                                        {
                                                                          ""@type"": ""asset"",
                                                                          ""value"": {
                                                                            ""assetId"": ""Alexa.Value.Open""
                                                                          }
                                                                        }
                                                                      ]
                                                                    }
                                                                  },
                                                                  {
                                                                    ""value"": ""Position.Down"",
                                                                    ""modeResources"": {
                                                                      ""friendlyNames"": [
                                                                        {
                                                                          ""@type"": ""asset"",
                                                                          ""value"": {
                                                                            ""assetId"": ""Alexa.Value.Close""
                                                                          }
                                                                        }
                                                                      ]
                                                                    }
                                                                  }
                                                                ]
                                                              }");
            jCapabilityAlexaBlindsController.Add("configuration", jConfiguration);

            JObject jSemantics = JObject.Parse(@"{
                                                        ""actionMappings"": [
                                                          {
                                                                ""@type"": ""ActionsToDirective"",
                                                            ""actions"": [""Alexa.Actions.Close"", ""Alexa.Actions.Lower""],
                                                            ""directive"": {
                                                              ""name"": ""SetMode"",
                                                              ""payload"": {
                                                                ""mode"": ""Position.Down""
                                                              }
                                                            }
                                                          },
                                                          {
                                                            ""@type"": ""ActionsToDirective"",
                                                            ""actions"": [""Alexa.Actions.Open"", ""Alexa.Actions.Raise""],
                                                            ""directive"": {
                                                              ""name"": ""SetMode"",
                                                              ""payload"": {
                                                                ""mode"": ""Position.Up""
                                                              }
                                                            }
                                                          }
                                                        ],
                                                        ""stateMappings"": [
                                                          {
                                                            ""@type"": ""StatesToValue"",
                                                            ""states"": [""Alexa.States.Closed""],
                                                            ""value"": ""Position.Down""
                                                          },
                                                          {
                                                            ""@type"": ""StatesToValue"",
                                                            ""states"": [""Alexa.States.Open""],
                                                            ""value"": ""Position.Up""
                                                          }  
                                                        ]
                                                      }");
            jCapabilityAlexaBlindsController.Add("semantics", jSemantics);

            return jCapabilityAlexaBlindsController;
        }





        public void SetPayload(JObject payload)
        {
            _jResponse["event"]["payload"] = payload;
        }


        private string CheckValue(string value, string defaultValue)
        {
            if (String.IsNullOrEmpty(value))
                return defaultValue;

            return value;
        }


        public override string ToString()
        {
            return _jResponse.ToString();
        }
    }
}