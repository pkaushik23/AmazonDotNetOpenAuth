using DotNetOpenAuth.AspNet.Clients;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MvcAppAmazonLogin
{
    public class AmazonClient : OAuth2Client
    {
        private const string AuthorizationEndpoint = "https://www.amazon.com/ap/oa";
        private const string TokenEndpoint = "https://api.amazon.com/auth/o2/token";
        private readonly string appId;
        private readonly string appSecret;

        public AmazonClient(string providerName, string appId, string appSecret)
            : base(providerName)
        {
            this.appId = appId;
            this.appSecret = appSecret;
        }

        public AmazonClient(string appId, string appSecret)
            : base("amazon")
        {
            this.appId = appId;
            this.appSecret = appSecret;
        }


        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            var requestUrl = string.Format("{0}?client_id={1}&scope={2}&response_type={3}&redirect_uri={4}&state={5}",
            AuthorizationEndpoint, this.appId, "profile", "code", returnUrl.AbsoluteUri.Substring(0, returnUrl.AbsoluteUri.IndexOf("?")),
            HttpUtility.HtmlEncode(returnUrl.Query.Replace("?", "")).Replace("&amp;", "-"));
            return new Uri(requestUrl);
        }

        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            var request =
                    WebRequest.Create(
                            "https://api.amazon.com/user/profile?access_token=" + HttpUtility.UrlEncode(accessToken));
            var profileData = string.Empty;
            var userData = new Dictionary<string, string>();
            using (var response = request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var data = reader.ReadToEnd();
                    var dummyData = new { email = "", name = "", user_id = "" };
                    var userInfo = JsonConvert.DeserializeAnonymousType(data, dummyData);

                    userData["email"] = userInfo.email;
                    userData["name"] = userInfo.name;
                    userData["id"] = userInfo.user_id;
                }
            }
            return userData;
        }

        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            var sb = new StringBuilder();
            sb.Append("grant_type=authorization_code");
            sb.Append("&code=").Append(HttpUtility.UrlEncode(authorizationCode));
            sb.Append("&client_id=").Append(HttpUtility.UrlEncode(this.appId));
            sb.Append("&client_secret=").Append(HttpUtility.UrlEncode(this.appSecret));
            sb.Append("&redirect_uri=").Append(returnUrl.AbsoluteUri.Substring(0, returnUrl.AbsoluteUri.IndexOf("?")));

            var dataToPost = sb.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(dataToPost);

            var uri = new Uri(TokenEndpoint);
            var client = WebRequest.Create(uri);
            client.Method = "POST";
            client.ContentType = "application/x-www-form-urlencoded";
            client.ContentLength = bytes.Length;
            var bodyStream = client.GetRequestStream();
            bodyStream.Write(bytes, 0, bytes.Length);
            bodyStream.Close();

            using (WebResponse response = client.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var data = reader.ReadToEnd();
                    var dummyObject = new { access_token = "", expires_in = 0d, refresh_token = "", token_type = "" };
                    var accessTokenData = JsonConvert.DeserializeAnonymousType(data, dummyObject);
                    return accessTokenData.access_token;
                }
            }
        }
    }
}