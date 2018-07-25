using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace ConsumerServices
{
    [ServiceContract]
    public class Consumer
    {
       
        readonly string serviceURL = string.Empty;
        static HttpClient httpClient = null;
        static WebClient webClient = null;
        static object lockObject = new object();
        public ProxyService()
        {
            serviceURL = System.Configuration.ConfigurationManager.AppSettings["urlAddress"].ToString();
			
            //set connection lease timeout to cope DNS change ; Ideally it should be in ApplicationStart
            //var sp = ServicePointManager.FindServicePoint(new Uri(serviceURL));
            //sp.ConnectionLeaseTimeout = 1*60 * 1000; // 1 minute
        }

        [OperationContract]
        [WebGet(UriTemplate = "", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetHelloContent()
        {
            byte[] resultBytes = Encoding.UTF8.GetBytes("Hello Service");
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return new MemoryStream(resultBytes);
        }

        [OperationContract]
        [WebGet(UriTemplate = "GetContentByHttpClient", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetContentByHttpClient()
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            try
            {
				//UriBuilder provide refine way to generate complete uri
                var uriBuilder = new UriBuilder(serviceURL);                
                Uri lUri = uriBuilder.Uri;
                
                var watch = System.Diagnostics.Stopwatch.StartNew();
				//call synchronously
                string result = GetHttpClient().GetStringAsync(lUri).Result;
                watch.Stop();

                System.Diagnostics.Debug.WriteLine(string.Format("Total time taken in REST call:{0}", watch.Elapsed.TotalMilliseconds));
               
                byte[] resultBytes = Encoding.UTF8.GetBytes(result );

                return new MemoryStream(resultBytes);

            }
            catch (Exception ex)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
               var result = ex.Message + "\n" + "Error:- "+ex.GetBaseException().Message+"\n"+"Source:- "+ ex.GetBaseException().Source;
              
                byte[] resultBytes = Encoding.UTF8.GetBytes(result);
                return new MemoryStream(resultBytes);
            }
        }

        [OperationContract]
        [WebGet(UriTemplate = "GetContentByWebClient", BodyStyle = WebMessageBodyStyle.Bare)]
        Stream GetContentByWebClient()
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            try
            {
               //UriBuilder provide refine way to generate complete uri
                var uriBuilder = new UriBuilder(serviceURL);                
                Uri lUri = uriBuilder.Uri;
               
                var watch = System.Diagnostics.Stopwatch.StartNew();
                string result = GetWebClient().DownloadString(lUri);
                watch.Stop();

                System.Diagnostics.Debug.WriteLine(string.Format("Total time taken in REST call:{0}", watch.Elapsed.TotalMilliseconds));
                byte[] resultBytes = Encoding.UTF8.GetBytes(result );

                return new MemoryStream(resultBytes);

            }
            catch (Exception ex)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
                var result = ex.Message + "\n" + "Error:- " + ex.GetBaseException().Message + "\n" + "Source:- " + ex.GetBaseException().Source;
               
                byte[] resultBytes = Encoding.UTF8.GetBytes(result);
                return new MemoryStream(resultBytes);
            }
        }
        private HttpClient GetHttpClient()
        {
            if (httpClient != null)
            {
                return httpClient;
            }
            lock (lockObject)
            {
                if (httpClient != null)
                {
                    return httpClient;
                }
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate,sdch");
                httpClient.DefaultRequestHeaders.Authorization =
                              new AuthenticationHeaderValue(
                                                          "Basic",
                                                          Convert.ToBase64String(
                                                                      System.Text.ASCIIEncoding.ASCII.GetBytes(
                                                                          string.Format("{0}:{1}",
                                                                                        System.Configuration.ConfigurationManager.AppSettings["username"],
                                                                                        System.Configuration.ConfigurationManager.AppSettings["password"]))));

                return httpClient;
            }
        }

        private WebClient GetWebClient()
        {
            if (webClient != null)
            {
                return webClient;
            }
            lock (lockObject)
            {
                if (webClient != null)
                {
                    return webClient;
                }
                webClient = new WebClient();
                webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.100 Safari/537.36");
                webClient.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
                webClient.Headers.Add("Authorization",string.Format("Basic {0}", Convert.ToBase64String(
                                                                                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                                                                                        string.Format("{0}:{1}",
                                                                                        System.Configuration.ConfigurationManager.AppSettings["username"],
                                                                                        System.Configuration.ConfigurationManager.AppSettings["password"])))));

                return webClient;
            }           
        }       

    }
}
