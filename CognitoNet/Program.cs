using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CognitoNet
{
    internal class Program
    {
        private string _apiKey;
        private string _apiSecret;

        #region Constants

        private const string KeyFile = "key.txt";
        private const char KeySeparator = ',';
        private const string CognitoUrl = "https://sandbox.cognitohq.com/profiles";
        private const string ContentType = "application/vnd.api+json";
        private const string AcceptType = "application/vnd.api+json";
        private const string Version = "2016-09-01";

        #endregion Constants

        /// <summary>
        /// Returns digest encoded to Base64 string
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

        private async Task Run()
        {
            string utcDate = DateTimeOffset.Now.ToString("r");

            string body = JsonConvert.SerializeObject(new { data = new { type = "profile" } });
            string digest = GetDigest(body);
            string signingString = GetSigningString(utcDate, digest);
            string signature = GetSignature(_apiSecret, signingString);
            string authorization = GetAuthorizationString(_apiKey, signature);

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    StringContent content = new StringContent(body, Encoding.UTF8);

                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, CognitoUrl);

                    requestMessage.Headers.Add("Date", utcDate);
                    requestMessage.Headers.Add("Digest", digest);
                    requestMessage.Headers.Add("Authorization", authorization);
                    requestMessage.Headers.Add("Cognito-Version", Version);
                    // Accept-type header
                    requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(AcceptType));
                    // Content-Type header
                    content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
                    requestMessage.Content = content;

                    HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);
                    responseMessage.EnsureSuccessStatusCode();
                    string responseBody = await responseMessage.Content.ReadAsStringAsync();

                    Console.WriteLine(responseBody);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public Program()
        {
            // Config file contains one line with comma separated api key and secret key
            string content = File.ReadAllText(KeyFile);
            string[] lines = content.Split(KeySeparator);
            _apiKey = lines[0];
            _apiSecret = lines[1];
        }

        private static void Main(string[] args)
        {
            try
            {
                Program program = new Program();
                Task task = program.Run();
                task.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            Console.ReadKey();
        }
    }
}