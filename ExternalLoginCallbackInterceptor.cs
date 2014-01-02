using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MvcAppAmazonLogin
{
    public class ExternalLoginCallbackInterceptor : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += CallbackInterceptor_BeginRequest;
        }

        void CallbackInterceptor_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;
            HttpRequest request = context.Request;

            if (!String.IsNullOrEmpty(request.QueryString["state"]))
            {
                var routeValues = new Dictionary<string, string>();
                foreach (string key in request.QueryString.AllKeys)
                    if (key.ToLower() == "state")
                    {
                        var dataInState = request.QueryString["state"].ToString().Split(new char[] { '-' });
                        foreach (var item in dataInState)
                        {
                            var qsCollection = HttpUtility.ParseQueryString(item);
                            foreach (var qs in qsCollection.AllKeys)
                            {
                                routeValues[qs] = qsCollection[qs];
                            }
                        }
                    }
                    else
                    {
                        routeValues[key] = request.QueryString[key];
                    }

                var builder = new StringBuilder();
                foreach (var item in routeValues)
                {
                    builder.Append(string.Format("{0}={1}&", item.Key, item.Value));
                }

                var urlToRedirectTo = (new UriBuilder()
                {
                    Scheme = request.Url.Scheme,
                    Host = request.Url.Host,
                    Path = request.FilePath,
                    Port = request.Url.Port,
                    Query = builder.ToString(),
                }.Uri.AbsoluteUri);

                context.Response.StatusCode = 302;
                context.Response.AppendHeader("Location", urlToRedirectTo);
                context.Response.End();
            }
        }

        public String ModuleName
        {
            get { return "ExternalLoginCallbackInterceptor"; }
        }
    }
}