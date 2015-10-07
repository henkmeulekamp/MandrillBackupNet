using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using JsonReader = JsonFx.Json.JsonReader;

namespace Mandrill_Backup
{

    class Program
    {
        public const string Mandrillurl = "https://mandrillapp.com/api/1.0/";
        //export optional name
        //import optional name

        private static string ToJsonPrettyPrint(dynamic obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonConverter[] { new StringEnumConverter() });
        }
        
        static void Main(string[] args)
        {
            var options = new ApplicationArguments();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {               
                return;
            }

            var dir = Directory.CreateDirectory(options.ExportDir);
            
            switch (options.Action)
            {
                case Action.Export:
                    ExportallTemplatesToFolder(options.Key, dir);
                    break;
                case Action.Import:
                    ImportFromFolderToMandrill(options.Key, dir);
                    break;
                case Action.Delete:
                    DeleteAllTemplates(options.Key,
                            Directory.CreateDirectory(dir.FullName + "\\" + string.Format("backups-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ImportFromFolderToMandrill(string apikey, DirectoryInfo exportDir)
        {
            var reader = new JsonReader();

            foreach (var f in exportDir.GetFiles())
            {
                dynamic template = reader.Read(File.ReadAllText(f.FullName));
                UploadToDestination(apikey, template);
            }
        }

        private static void ExportallTemplatesToFolder(string apikey, DirectoryInfo exportDir)
        {
            var reader = new JsonReader();
            var httpHelper = new HttpHelper();

            var templatesResponse = httpHelper.Post(Mandrillurl + "/templates/list.json", new {key = apikey}).Result;
            if (templatesResponse.Code == HttpStatusCode.OK)
            {
                //
                dynamic templates = reader.Read(templatesResponse.Body);

                foreach (var t in templates)
                {
                    Console.WriteLine("Exporting: " + t.name + " - " + t.slug);

                    File.WriteAllText(Path.Combine(exportDir.FullName, t.name + ".template.json"), ToJsonPrettyPrint(t));
                }
            }
        }

        private static void DeleteAllTemplates(string apikey, DirectoryInfo backupDir)
        {
            var reader = new JsonReader();
            var httpHelper = new HttpHelper();

            //make backups.. always
            ExportallTemplatesToFolder(apikey, backupDir);

            var templatesResponse = httpHelper.Post(Mandrillurl + "/templates/list.json", new {key = apikey}).Result;
            if (templatesResponse.Code == HttpStatusCode.OK)
            {
                //
                dynamic templates = reader.Read(templatesResponse.Body);

                foreach (var t in templates)
                {
                    //delete, take slug and use as name
                    string name = t.slug;

                    var deleteTemplate = httpHelper.Post(Mandrillurl + "/templates/delete.json", new {key = apikey, name}).Result;

                    Console.WriteLine(string.Format("Template delete result {0}: {1} - {2}", name, deleteTemplate.Code, deleteTemplate.StatusDescription));
                }
            }
        }

        private static void UploadToDestination(string apikey, dynamic template)
        {
            var httpHelper = new HttpHelper();
            if (!string.IsNullOrWhiteSpace(apikey))
            {
                //make sure to rewrite everything to the name
                string name = template.name;

                template.key = apikey;
                template.slug = name;

                var addResponse = httpHelper.Post(Mandrillurl + "/templates/add.json", template).Result;

                Console.WriteLine(string.Format("Template add result {0}: {1} - {2}", template.slug, addResponse.Code, addResponse.StatusDescription));
            }
        }

        internal static bool HasProperty(dynamic d, string propertyname)
        {
            return ((IDictionary<string, object>) d).ContainsKey(propertyname);
        }
    }

    internal class HttpHelper
    {
        //some helper methods
        internal async Task<HttpResult> Post<T>(string url, T request)
        {
            //set authorization header to basic clientId:secret
            var client = CreateHttpClient();

            //format message to post
            string requestData = JsonConvert.SerializeObject(request);

            //set posted data to form-urlencoded content
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestData, Encoding.UTF8, "application/json")
            };

            return await client.SendAsync(httpRequest).ContinueWith(responseTask =>
            {
                var response = responseTask.Result;

                return new HttpResult
                {
                    Body = response.Content.ReadAsStringAsync().Result, Code = response.StatusCode, StatusDescription = response.ReasonPhrase
                };
            }).ConfigureAwait(false);
        }

        internal HttpClient CreateHttpClient()
        {
            var client = new HttpClient();

            //request json back
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}
