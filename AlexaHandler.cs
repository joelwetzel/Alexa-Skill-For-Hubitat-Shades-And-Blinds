using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Newtonsoft.Json.Linq;

using System.Net.Http;

namespace AlexaSkillForHubitatWindowShades
{
    public class AlexaHandler
    {
        // Find these on the app page for the Maker API.
        private readonly string _hubitatHubId = "XXXXX";
        private readonly string _hubitatAccessToken = "XXXXX";

        private ILambdaContext _context;


        private void log(string txt)
        {
            if (_context != null)
            {
                _context.Logger.Log(txt);
            }
        }


        public Stream Handler(Stream inputStream, ILambdaContext context)
        {
            _context = context;

            StreamReader reader = new StreamReader(inputStream);
            string request  = reader.ReadToEnd();

            log("Request:");
            log(request);

            AlexaResponse alexaResponse;
            
            JObject jRequest = JObject.Parse(request);

            string nameSpace = jRequest["directive"]["header"]["namespace"].Value<string>();
            string name = jRequest["directive"]["header"]["name"].Value<string>();

            switch (nameSpace)
            {
                case "Alexa.Authorization":
                    {
                        log("Alexa.Authorization Request");

                        alexaResponse = new AlexaResponse("Alexa.Authorization", "AcceptGrant.Response");
                        break;
                    }
                
                case "Alexa.Discovery":
                    {
                        log("Alexa.Discovery Request");

                        alexaResponse = discoverHubitatWindowShades();

                        break;
                    }

                case "Alexa.ModeController":
                    {
                        log("Alexa.ModeController Request");

                        string correlationToken = jRequest["directive"]["header"]["correlationToken"].Value<string>();
                        string endpointId = jRequest["directive"]["endpoint"]["endpointId"].Value<string>();

                        if (name == "SetMode")
                        {
                            string newMode = jRequest["directive"]["payload"]["mode"].Value<string>();

                            log($"SetMode: {newMode}");

                            string hubitatCommand = "open";
                            if (newMode == "Position.Up")
                            { hubitatCommand = "open"; }
                            else if (newMode == "Position.Down")
                            { hubitatCommand = "close"; }

                            log($"command: {hubitatCommand}, endpoint: {endpointId}");

                            sendCommandToHubitat(endpointId, hubitatCommand);

                            alexaResponse = new AlexaResponse("Alexa", "Response", endpointId, "INVALID", correlationToken);
                            alexaResponse. AddContextProperty("Alexa.ModeController", "mode", newMode, "Blinds.Position", 200);
                        }
                        else
                        {
                            alexaResponse = new AlexaResponse("Alexa", "Response", endpointId, "INVALID", correlationToken);
                        }
                        break;
                    }

                default:
                    {
                        log("INVALID Namespace");

                        alexaResponse = new AlexaResponse();
                        break;
                    }
            }
            
            string response = alexaResponse.ToString();

            log("Response:");
            log(response);

            return new MemoryStream(Encoding.UTF8.GetBytes(response));
        }


        public bool StoreDeviceStateInDynamo(String endpointId, String state, String value)
        {
            String attributeValue = state + "Value";
            
            AmazonDynamoDBClient dynamoClient = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
            Table table = Table.LoadTable(dynamoClient, "SampleSmartHome");

            Document item = new Document();
            item["ItemId"] = endpointId;
            item[attributeValue] = value;

            Task<Document> updateTask = table.UpdateItemAsync(item);
            updateTask.Wait();

            if (updateTask.Status == TaskStatus.RanToCompletion)
                return true;
            
            return false;
        }


        private AlexaResponse discoverHubitatWindowShades()
        {
            var alexaResponse = new AlexaResponse("Alexa.Discovery", "Discover.Response", "2121");

            JObject jCapabilityAlexa = alexaResponse.CreatePayloadEndpointCapability();
            JObject jCapabilityBlinds = alexaResponse.CreateBlindsCapability();

            JArray capabilities = new JArray();
            capabilities.Add(jCapabilityAlexa);
            capabilities.Add(jCapabilityBlinds);

            HttpClient httpClient = new HttpClient();

            Task<HttpResponseMessage> getTask = httpClient.GetAsync($"https://cloud.hubitat.com/api/{_hubitatHubId}/apps/833/devices/all?access_token={_hubitatAccessToken}");
            getTask.Wait();

            var response = getTask.Result;

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                log($"Could not retrieve Hubitat devices: {response.StatusCode}");
            }

            Task<string> readTask = response.Content.ReadAsStringAsync();
            readTask.Wait();

            var json = readTask.Result;
            //log(json);

            var jDevices = JArray.Parse(json);

            foreach (JObject jDevice in jDevices)
            {
                //log(jDevice.ToString());

                var deviceIsWindowShade = false;

                JArray jCapabilities = jDevice["capabilities"].Value<JArray>();

                foreach (JToken jCapability in jCapabilities)
                {
                    if (jCapability.Value<string>() == "WindowShade")
                    {
                        deviceIsWindowShade = true;
                    }
                }

                if (deviceIsWindowShade)
                {
                    var name = jDevice["name"].Value<string>();
                    var id = jDevice["id"].Value<string>();

                    alexaResponse.AddPayloadEndpoint(id, name, capabilities.ToString());
                }
            }

            return alexaResponse;
        }


        private void sendCommandToHubitat(string hubitatDeviceId, string hubitatCommand)
        {
            HttpClient httpClient = new HttpClient();

            var task = httpClient.GetAsync($"https://cloud.hubitat.com/api/{_hubitatHubId}/apps/833/devices/{hubitatDeviceId}/{hubitatCommand}?access_token={_hubitatAccessToken}");
            task.Wait();
        }
        
    }
}