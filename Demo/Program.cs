using System;
using System.Configuration;
using System.IO;
using System.Net;
using Duo;

namespace duo_csharp
{
    class Program
    {
        private static string ikey;
        private static string skey;
        private static string akey;
        private static string host;
        private static string port;

        static void Main(string[] args)
        {
            ParseConfiguration();
            WebServer server = new WebServer(SendResponse, $"http://localhost:{port}/");
            server.Run();
            PrintStartupMessaging();
            Console.ReadKey();
            server.Stop();
        }

        private static void PrintStartupMessaging()
        {
            Console.WriteLine("Server has been started.");
            Console.WriteLine("Visit the root URL with a 'user' argument, e.g.");
            Console.WriteLine($"'http://localhost:{port}/?user=myname'.");
            Console.WriteLine("Press any key to quit.");
        }

        private static void ParseConfiguration()
        {
            ikey = ConfigurationManager.AppSettings["ikey"];
            skey = ConfigurationManager.AppSettings["skey"];
            akey = ConfigurationManager.AppSettings["akey"];
            host = ConfigurationManager.AppSettings["host"];
            port = ConfigurationManager.AppSettings["port"];
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            if (string.Compare(request.HttpMethod, "POST", true) == 0)
            {
                return doPost(request);
            }
            return doGet(request);
        }

        private static string doPost(HttpListenerRequest request)
        {
            using (Stream body = request.InputStream)
            {
                using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
                {
                    string bodyStream = reader.ReadToEnd();
                    var form = bodyStream.Split('=');
                    var sig_response_val = WebUtility.UrlDecode(form[1]);
                    string responseUser = Web.VerifyResponse(ikey, skey, akey, sig_response_val);

                    if (string.IsNullOrEmpty(responseUser))
                    {
                        return "Did not authenticate with Duo.";
                    }

                    return $"Authenticated with Duo as {responseUser}.";
                }
            }
        }

        private static string doGet(HttpListenerRequest request)
        {
            string response;

            try
            {
                response = File.ReadAllText(Path.GetFileName(request.RawUrl));
            }
            catch
            {
                string userName = request.QueryString.Get("user");
                
                if (string.IsNullOrEmpty(userName))
                {
                    return "You must include a user to authenticate with Duo";
                }

                var sig_request = Web.SignRequest(ikey, skey, akey, userName);
                response = $@"<html>
                  <head>
                    <title>Duo Authentication</title>
                    <meta name='viewport' content='width=device-width, initial-scale=1'>
                    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
                    <link rel='stylesheet' type='text/css' href='Duo-Frame.css'>
                  </head>
                  <body>
                    <h1>Duo Authentication</h1>
                    <script src='/Duo-Web-v2.js'></script>
                    <iframe id='duo_iframe'
                            title='Two-Factor Authentication'
                            frameborder='0'
                            data-host='{host}'
                            data-sig-request='{sig_request}'>
                    </iframe>
                  </body>
                </html>";
            }

            return response;
        }
    }
}
