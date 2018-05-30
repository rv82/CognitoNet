using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CognitoNet
{
    public class Program
    {
        private string _apiKey;
        private string _apiSecret;

        /// <summary>
        /// Constructor. Reads key file and sets keys to program.
        /// </summary>
        public Program()
        {
            // Config file contains one line with comma separated api key and secret key
            string content = File.ReadAllText(Config.KeyFile);
            string[] lines = content.Split(Config.KeySeparator);
            _apiKey = lines[0];
            _apiSecret = lines[1];
        }

        /// <summary>
        /// Returns digest encoded to Base64 string.
        /// </summary>
        /// <param name="data">hashing data.</param>
        /// <returns></returns>
        private string GetDigest(string data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(data);
                byte[] bodyHashBytes = sha256.ComputeHash(bodyBytes);
                return "SHA-256=" + Convert.ToBase64String(bodyHashBytes);
            }
        }

        /// <summary>
        /// Returns hashed signature and converted to Base64 string
        /// </summary>
        /// <param name="secret">secret key.</param>
        /// <param name="data">hashing data.</param>
        private string GetSignature(string secret, string data)
        {
            Encoding encoding = Encoding.UTF8;
            byte[] signingBytes = encoding.GetBytes(data);
            byte[] signatureBytes;
            byte[] secretBytes = encoding.GetBytes(secret);
            using (HMACSHA256 hmac = new HMACSHA256(secretBytes))
            {
                signatureBytes = hmac.ComputeHash(signingBytes);
            }
            return Convert.ToBase64String(signatureBytes);
        }

        /// <summary>
        /// Constructs and returns signing string using date and SHA-256 digest converted to base64 string.
        /// </summary>
        /// <param name="utcDate"></param>
        /// <param name="digest"></param>
        /// <returns></returns>
        private string GetSigningString(string utcDate, string digest)
        {
            const string requestTarget = "post /profiles";

            string[] signingStringParts = {
                    $"(request-target): {requestTarget}",
                    $"date: {utcDate}",
                    $"digest: {digest}"
                };
            return string.Join("\n", signingStringParts);
        }

        /// <summary>
        /// Returns authorization string.
        /// </summary>
        /// <param name="apiKey">public key.</param>
        /// <param name="signature">signature.</param>
        /// <returns>authorization string.</returns>
        private string GetAuthorizationString(string apiKey, string signature)
        {
            string[] authorizationParts = {
                $"Signature keyId=\"{apiKey}\"",
                "algorithm=\"hmac-sha256\"",
                "headers=\"(request-target) date digest\"",
                $"signature=\"{signature}\""
            };
            return string.Join(",", authorizationParts);
        }

        private string GetProfileID(string json)
        {
            JObject jObject = JObject.Parse(json);
            return (string)jObject.SelectToken(JsonResponsePaths.DataID);
        }

        private async Task<string> SendRequestAndGetResponseAsync(string targetRoute, string body)
        {
            string utcDate = DateTimeOffset.Now.ToString("r");
            string digest = GetDigest(body);
            string signingString = AuthUtils.GetSigningString(targetRoute, utcDate, digest);
            string signature = GetSignature(_apiSecret, signingString);
            string authorization = GetAuthorizationString(_apiKey, signature);

            using (HttpClient httpClient = new HttpClient())
            {
                StringContent content = new StringContent(body, Encoding.UTF8);

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, Cognito.CognitoUrl + targetRoute);

                requestMessage.Headers.Add(Headers.Date, utcDate);
                requestMessage.Headers.Add(Headers.Digest, digest);
                requestMessage.Headers.Add(Headers.Authorization, authorization);
                requestMessage.Headers.Add(Headers.CognitoVersion, Cognito.Version);
                // Accept-type header
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Cognito.AcceptType));
                // Content-Type header
                content.Headers.ContentType = new MediaTypeHeaderValue(Cognito.ContentType);
                requestMessage.Content = content;

                HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);
                responseMessage.EnsureSuccessStatusCode();
                return await responseMessage.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// Async entry point.
        /// </summary>
        private async Task RunAsync()
        {
            try
            {
                string body;
                string responseBody;
                string profileID;

                // ВНИМАНИЕ! Согласно документации, эту процедуру надо проделать один раз, получить profile ID,
                // сохранить его где-то (например в БД) и в дальнейшем использовать его.
                // Authorization
                body = RequestBodies.ProfileCreatingRequestBody;
                responseBody = await SendRequestAndGetResponseAsync(CognitoUrlRoutes.Profiles, body);
                profileID = GetProfileID(responseBody);
                Console.WriteLine($"Profile ID = {profileID}");

                // this ID was get by using ProfileCreatingRequestBody
                //string progileID = "prf_dSE2ETMdf5GN5s";

                // Searching person by phone using progileID
                string phone = "+16508007985";
                body = RequestUtils.GetSearchRequestBody(phone, profileID);
                responseBody = await SendRequestAndGetResponseAsync(CognitoUrlRoutes.IdentitySearches, body);
                // Пока не стал извлекать конкретные данные.
                // Сделать это несложно при помощи Newtonsoft.Json.Linq.
                Console.WriteLine(responseBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        internal static void Main(string[] args)
        {
            try
            {
                Program program = new Program();
                Task task = program.RunAsync();
                task.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            Console.WriteLine(Messages.PressAnyKeyToStopProgram);
            Console.ReadKey();
        }
    }
}