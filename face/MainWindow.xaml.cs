using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Packaging;

namespace PersonGroup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        String subscription_key = "ac5c0b4dcc1e49939de0b9313097525c";
        private static string faceId;
        private static string personId;

        public MainWindow()
        {
            InitializeComponent();
            Main();
        }
        
        public async Task Main()
        {
            
            await CreatePersonGroup(subscription_key);                      //<-但你可以從這裡開始            
            
            string personId = await CreatePerson(subscription_key);         //<-根據ppt，demo時從這裡開始做
            
            string url;
            bool detectSuccess;

            //add face in the array
            string[] image_url = new string[]{ "https://i.pinimg.com/564x/d7/b7/dd/d7b7dd8f833bba3f46dae0dc2f21595d.jpg",
                                            "https://i.pinimg.com/564x/c1/7c/2a/c17c2a3558578148f6d07a988f081e31.jpg",
                                            "https://i.pinimg.com/564x/9d/4f/27/9d4f27b12e78da4c6016e80709129aef.jpg",
                                            "https://i.pinimg.com/564x/df/23/67/df2367f9c5076711012b9520473ef55c.jpg",
                                            "https://i.pinimg.com/564x/06/3d/27/063d27b0c9d4205fd17f9708abdc5e73.jpg",
                                            "https://i.pinimg.com/564x/32/70/71/32707180120cf0a72d1013ce68defd23.jpg",
                                            "https://i.pinimg.com/564x/3a/95/d9/3a95d943e6f9741149ecdccc0c60ee57.jpg",
                                            "https://i.pinimg.com/564x/9c/b5/8f/9cb58fe4c03ad6a27db90864ff776d03.jpg",
                                            "https://i.pinimg.com/564x/68/71/73/6871733d03938d113563fd6bb178275c.jpg",
                                            "https://i.pinimg.com/564x/14/5c/72/145c7204f3a38ec55d49f7fabf9c073d.jpg"};
            
            foreach (string img in image_url)
            {
                Console.WriteLine("Now Url="+img);
                detectSuccess = await DetectFace(subscription_key, img);
                if (detectSuccess)
                {
                    await AddFace(subscription_key, personId, img);
                }
            }

            //add more face
            Console.WriteLine("Eenter 1 to add more url for DetectFace and AddFace, enter 0 to continue to the training process.");
            while (Console.ReadLine() == "1")
            {
                Console.WriteLine("Enter the url : ");
                url = Console.ReadLine();
                detectSuccess = await DetectFace(subscription_key, url);
                if (detectSuccess)
                {
                    await AddFace(subscription_key, personId, url );
                }
                
                Console.WriteLine("Eenter 1 to add url for DetectFace and AddFace, enter 0 to continue to the training process.");
            }
            
            await TrainPersonGroup(subscription_key);
            await GetTrainStatus(subscription_key);
            

            do
            {
                Console.WriteLine("Enter the url for FaceIdentify: ");
                url = Console.ReadLine();
                detectSuccess = await DetectFace2(subscription_key, url);
                if (detectSuccess)
                {
                    bool identifySuccess = await FaceIdentitfy(subscription_key);
                    if (identifySuccess)
                    {
                        await GetPerson(subscription_key);
                    }
                }
            } while (!detectSuccess);

            
            await DeletePersonGroup(subscription_key);

            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
            
        }

        static async Task GetPerson(String subscription_key)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/persongroups/hwasa/persons/" + personId + "?" + queryString;

            var response = await client.GetAsync(uri);
            string result = await response.Content.ReadAsStringAsync();

            myJsonParsing my_jsonParse = new myJsonParsing();
            string str_decode = my_jsonParse.myJsonParse(result);
            Console.WriteLine("Return Json in the GetPerson() process : \n" + str_decode);

        }

        static async Task<bool> DetectFace2(String subscription_key, string url)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            // Request parameters
            queryString["returnFaceId"] = "true";

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/detect?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"url\":\"" + url + "\"}");

            string result;
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                result = await response.Content.ReadAsStringAsync();
            }

            bool detectSuccess;
            if (result.Contains("error"))
            {
                myJsonParsing my_jsonParse = new myJsonParsing();
                string str_decode = my_jsonParse.myJsonParse(result);
                Console.WriteLine("Return Json in the DetectFace() process : \n" + str_decode);
                detectSuccess = false;

            }
            else if (result.Equals("[]")) // no face detected.
            {
                Console.WriteLine("Return Json in the DetectFace() process : \n");
                Console.WriteLine("No face detected.");
                detectSuccess = false;

            }
            else
            {
                result = result.Remove(0, 1);
                result = result.Remove(result.Length - 1, 1);
                myJsonParsing my_jsonParse = new myJsonParsing();
                string str_decode = my_jsonParse.myJsonParse(result);
                Console.WriteLine("Return Json in the DetectFace() process : \n" + str_decode);

                int index1 = str_decode.IndexOf("=");
                string temp = str_decode.Remove(0,index1+1);
                int index2 = temp.IndexOf("\n");
                faceId = temp.Remove(index2); //刪除此字串中從指定位置到最後位置的所有字元。 

                detectSuccess = true;
            }

            return detectSuccess;
        }

       
        static async Task<bool> FaceIdentitfy(String subscription_key)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/identify?" + queryString;

            HttpResponseMessage response;

            faceId = "[\"" + faceId + "\"]";

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"personGroupId\":\"hwasa\",\"faceIds\":"+ faceId +"}");

            string result;
            bool identifySuccess;
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                result = await response.Content.ReadAsStringAsync();
            }
            if (result.Contains("error"))
            {
                myJsonParsing my_jsonParse = new myJsonParsing();
                string str_decode = my_jsonParse.myJsonParse(result);
                Console.WriteLine("Return Json in the FaceIdentitfy() process : \n" + str_decode);
                identifySuccess = false;
            }
            else
            {
                result = result.Remove(0, 1);
                result = result.Remove(result.Length - 1, 1);
                myJsonParsing my_jsonParse = new myJsonParsing();
                string str_decode = my_jsonParse.myJsonParse(result);
                Console.WriteLine("Return Json in the FaceIdentitfy() process : \n" + str_decode);
                identifySuccess = true;

            }
            return identifySuccess;
            

        }
        

        static async Task GetTrainStatus(String subscription_key)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/persongroups/hwasa/training?" + queryString;

            var response = await client.GetAsync(uri);
            string result = await response.Content.ReadAsStringAsync();
            myJsonParsing my_jsonParse = new myJsonParsing();
            string str_decode = my_jsonParse.myJsonParse(result);
            Console.WriteLine("Return Json in the GetTrainStatus() process : \n" + str_decode);
        }

        static async Task TrainPersonGroup(String subscription_key)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/persongroups/hwasa/train?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("");
            string result;
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                result = await response.Content.ReadAsStringAsync();
            }
            myJsonParsing my_jsonParse = new myJsonParsing();
            string str_decode = my_jsonParse.myJsonParse(result);
            Console.WriteLine("Return Json in the TrainPersonGroup() process : \n" + str_decode);

        }

        static async Task DeletePersonGroup(String subscription_key)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/persongroups/hwasa?" + queryString;

            var response = await client.DeleteAsync(uri);
            string result = await response.Content.ReadAsStringAsync();

            myJsonParsing my_jsonParse = new myJsonParsing();
            string str_decode = my_jsonParse.myJsonParse(result);
            Console.WriteLine("Return Json in the DeletePersonGroup() process : \n" + str_decode);


        }

        static async Task AddFace(String subscription_key, String personId , string url)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            
            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/persongroups/hwasa/persons/"+ personId + "/persistedFaces?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"url\":\"" + url + "\"}");

            string result;

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                result = await response.Content.ReadAsStringAsync();
            }
            myJsonParsing my_jsonParse = new myJsonParsing();
            string str_decode = my_jsonParse.myJsonParse(result);
            Console.WriteLine("Return Json in the AddFace() process : \n" + str_decode);
        }

        static async Task<String> CreatePerson(String subscription_key)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/persongroups/hwasa/persons?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"name\":\"hwasa\"}");
            string result;
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                result = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(result);
            }

            myJsonParsing my_jsonParse = new myJsonParsing();
            string str_decode = my_jsonParse.myJsonParse(result);
            Console.WriteLine("Return Json in the CreatePerson() process : \n"+str_decode);
            string personId;
            if (!str_decode.Contains("error"))
            {
                int index = str_decode.IndexOf("=");
                string temp = str_decode.Remove(0,index+1);
                temp = temp.Trim();
                personId = temp;
            }
            else
                personId = "error";
            return personId;

        }

        static async Task CreatePersonGroup(String subscription_key)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/persongroups/hwasa?" + queryString;


            HttpResponseMessage response;

            String json_str = "{\"name\":\"Hwasa1\"}"; //目前 : recognition_01

            //Console.WriteLine(json_str);

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes(json_str);

            string result;

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PutAsync(uri, content);
                result = await response.Content.ReadAsStringAsync();
            }

            myJsonParsing my_jsonParse = new myJsonParsing();
            string str_decode = my_jsonParse.myJsonParse(result);
            Console.WriteLine("Return Json in the CreatePersonGroup() process : \n" + str_decode);

        }


        static async Task<bool> DetectFace(String subscription_key, string url)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscription_key);

            // Request parameters
            queryString["returnFaceId"] = "true";

            var uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/detect?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"url\":\"" + url + "\"}");

            string result;
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                result = await response.Content.ReadAsStringAsync();
            }

            bool detectSuccess;
            if (result.Contains("error"))
            {
                myJsonParsing my_jsonParse = new myJsonParsing();
                string str_decode = my_jsonParse.myJsonParse(result);
                Console.WriteLine("Return Json in the DetectFace() process : \n" + str_decode);
                detectSuccess = false;
                
            }
            else if (result.Equals("[]")) // no face detected.
            {
                Console.WriteLine("Return Json in the DetectFace() process : \n");
                Console.WriteLine("No face detected.");
                detectSuccess = false;

            }
            else
            {
                result = result.Remove(0, 1);
                result = result.Remove(result.Length - 1, 1);
                myJsonParsing my_jsonParse = new myJsonParsing();
                string str_decode = my_jsonParse.myJsonParse(result);
                Console.WriteLine("Return Json in the DetectFace() process : \n" + str_decode);
                detectSuccess = true;
            }
             
            return detectSuccess;
        }

        

        class myJsonParsing
        {
            public string myJsonParse(string result)
            {
                /*
                 * Suceessful Case : *
                CreatePersonGroup() : A successful call returns an empty response body.
                CreatePerson() : A successful call returns a new personId created.[personId]
                DeletePersonGroup() : A successful call returns an empty response body.
                DetectFace() : An empty response indicates no faces detected. A face entry contains [faceId] and [faceRectangle]in this project. 
                AddFace() : A successful call returns the new persistedFaceId.[persistedFaceId]
                TrainPersonGroup() : A successful call returns an empty JSON body.
                GetTrainStatus() : A successful call returns the person group's training status.[status][createdDateTime][lastActionDateTime][message(omit when succeed)]
                FaceIdentitfy() : A successful call returns the identified candidate person(s) for each query face.[faceId][candidates][personId][confidence]
                 */

                /*
                 * Error Case : *
                  {
                    "error": {
                    "code": "BadArgument",
                    "message": "Request body is invalid.",
                    "statusCode": 429
                    }
                  }
                */

                if (result != "")
                {
                    JObject jObject = JObject.Parse(result);
                    if (jObject.TryGetValue("error", out _))  //error request
                    {
                        String response="";
                        string json_str = jObject["error"].ToString();
                        response += "error=\n";
                        JObject jObject1 = JObject.Parse(json_str);
                        if (jObject1.TryGetValue("code", out _))
                        {
                            response += "code=" + jObject1["code"].ToString() + "\n";
                        }
                        if (jObject1.TryGetValue("message", out _))
                        {
                            response += "message=" + jObject1["message"].ToString() + "\n";
                        }
                        if (jObject1.TryGetValue("statusCode", out _))
                        {
                            response += "statusCode=" + jObject1["statusCode"].ToString() + "\n";
                        }
                        return response;
                    }
                    else //success request
                    {
                        String response = "";
                        if (jObject.TryGetValue("personId", out _))
                        {
                            response += "personId=" + jObject["personId"].ToString() + "\n";
                            personId = jObject["personId"].ToString();
                        }
                        if (jObject.TryGetValue("faceId", out _))
                        {
                            response += "faceId=" + jObject["faceId"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("faceRectangle", out _))
                        {
                            response += "faceRectangle=\n";
                            JObject jObject1 = JObject.Parse(jObject["faceRectangle"].ToString());

                            response += "width=" + jObject1["width"].ToString() + "\n";
                            response += "height=" + jObject1["height"].ToString() + "\n";
                            response += "left=" + jObject1["left"].ToString() + "\n";
                            response += "top=" + jObject1["top"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("persistedFaceId", out _))
                        {
                            response += "persistedFaceId=" + jObject["persistedFaceId"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("status", out _))
                        {
                            response += "status=" + jObject["status"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("createdDateTime", out _))
                        {
                            response += "createdDateTime=" + jObject["createdDateTime"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("lastActionDateTime", out _))
                        {
                            response += "lastActionDateTime=" + jObject["lastActionDateTime"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("message", out _))
                        {
                            response += "message=" + jObject["message"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("candidates", out _))
                        {
                            response += "candidates=\n";
                            string temp = jObject["candidates"].ToString();
                            int index = temp.IndexOf("[");
                            temp = temp.Remove(0,index+1);
                            index = temp.IndexOf("]");
                            temp = temp.Remove(index);

                            JObject jObject1 = JObject.Parse(temp);
                            response += "personId=" + jObject1["personId"].ToString() + "\n";
                            response += "confidence=" + jObject1["confidence"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("persistedFaceIds", out _))
                        {
                            response += "persistedFaceIds=" + jObject["persistedFaceIds"].ToString().Trim() + "\n";
                        }
                        if (jObject.TryGetValue("name", out _))
                        {
                            response += "name=" + jObject["name"].ToString() + "\n";
                        }
                        if (jObject.TryGetValue("userData", out _))
                        {
                            response += "userData=" + jObject["userData"].ToString() + "\n";
                        }

                        return response;
                    }
                }
                else   //success request or no face detected
                    return "";
                
                
       


                



            }
        }

    }
}
