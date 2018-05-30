namespace CognitoNet
{
    public static class Headers
    {
        public const string Date = "Date";
        public const string Digest = "Digest";
        public const string Authorization = "Authorization";
        public const string CognitoVersion = "Cognito-Version";
    }

    public static class Config
    {
        public const string KeyFile = "key.txt";
        public const char KeySeparator = ',';
    }

    public static class Cognito
    {
        public const string CognitoUrl = "https://sandbox.cognitohq.com";
        public const string ContentType = "application/vnd.api+json";
        public const string AcceptType = "application/vnd.api+json";
        public const string Version = "2016-09-01";
    }

    public static class CognitoUrlRoutes
    {
        public const string Profiles = "/profiles";
        public const string IdentitySearches = "/identity_searches";
    }

    public static class RequestTypes
    {
        public const string Profile = "profile";
    }

    public static class RequestBodies
    {
        public const string ProfileCreatingRequestBody = "{\"data\":{\"type\":\"profile\"}}";
        public const string SearchRequestBody = "{{\"data\":{{\"type\":\"identity_search\",\"attributes\":{{\"phone\":{{\"number\":\"{0}\"}}}},\"relationships\":{{\"profile\":{{\"data\":{{\"type\":\"profile\",\"id\":\"{1}\"}}}}}}}}}}";
    }

    public static class AuthStrings
    {
        public const string SigningString = "(request-target): post {0}\ndate: {1}\ndigest: {2}";
    }

    public static class AuthUtils
    {
        public static string GetSigningString(string requestTarget, string utcDate, string digest) =>
            string.Format(AuthStrings.SigningString, requestTarget, utcDate, digest);
    }

    public static class RequestUtils
    {
        public static string GetSearchRequestBody(string phone, string profileID) =>
            string.Format(RequestBodies.SearchRequestBody, phone, profileID);
    }

    public static class JsonResponsePaths
    {
        public const string DataID = "data.id";
    }

    public static class Messages
    {
        public const string PressAnyKeyToStopProgram = "Press any key to stop program";
    }
}