using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        private static string FormatTemplateNameToFileName(string templateName)
        {
            return templateName + ".template.json";
        }

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
                    ExportTemplatesToFolder(options.Key, dir, options.TemplateName);
                    break;
                case Action.Import:
                    ImportFromFolderToMandrill(options.Key, dir, options.TemplateName);
                    break;
                case Action.Delete:
                    DeleteAllTemplates(options.Key,
                            Directory.CreateDirectory(dir.FullName + "\\" + string.Format("backups-{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now)),
                            options.TemplateName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ImportFromFolderToMandrill(string apikey, DirectoryInfo exportDir, string templateName = null)
        {
            var reader = new JsonReader();

            var requestedTemplateFileName = !string.IsNullOrWhiteSpace(templateName)
                ? FormatTemplateNameToFileName(templateName)
                : null;

            foreach (var f in exportDir.GetFiles()
                    .Where(f=>string.IsNullOrWhiteSpace(templateName)
                        || f.Name.Equals(requestedTemplateFileName, StringComparison.OrdinalIgnoreCase)
                    ))
            {
                dynamic template = reader.Read(File.ReadAllText(f.FullName));
                UploadToDestination(apikey, template);
            }
        }

        private static string GetSlugName(string apikey, string templateName)
        {
            var reader = new JsonReader();
            var httpHelper = new HttpHelper();

            var templatesResponse = httpHelper.Post(Mandrillurl + "/templates/list.json", new { key = apikey }).Result;

            if (templatesResponse.Code == HttpStatusCode.OK)
            {
                //todo is there a better way?
                dynamic templates = reader.Read(templatesResponse.Body);

                foreach (var t in templates)
                {
                    if (templateName.Equals(t.name, StringComparison.OrdinalIgnoreCase))
                    {
                        return t.slug;
                    }                    
                }
            }
            throw new Exception("No template found by name: " + templateName);
        }

        private static void ExportTemplatesToFolder(string apikey, DirectoryInfo exportDir, string templateName = null)
        {
            var reader = new JsonReader();
            var httpHelper = new HttpHelper();

            var templatesResponse =  httpHelper.Post(Mandrillurl + "/templates/list.json", new { key = apikey }).Result;

            if (templatesResponse.Code == HttpStatusCode.OK)
            {
                //
                dynamic templates = reader.Read(templatesResponse.Body);

                foreach (var t in templates)
                {
                    if (!string.IsNullOrWhiteSpace(templateName)
                        && !templateName.Equals(t.name, StringComparison.OrdinalIgnoreCase))
                    {
                        //this seems to be the only way to get single template by name (not slug!)
                        continue;
                    }

                    Console.WriteLine("Exporting: " + t.name + " - " + t.slug);

                    File.WriteAllText(Path.Combine(exportDir.FullName,
                            FormatTemplateNameToFileName(t.name)), 
                            ToJsonPrettyPrint(t));
                }
            }
        }

        private static void DeleteAllTemplates(string apikey, DirectoryInfo backupDir, 
                                               string templateName = null)
        {
            var reader = new JsonReader();
            var httpHelper = new HttpHelper();

            //make backups.. always
            ExportTemplatesToFolder(apikey, backupDir, templateName);

            var templatesResponse = httpHelper.Post(Mandrillurl + "/templates/list.json", new {key = apikey}).Result;
       
            if (templatesResponse.Code == HttpStatusCode.OK)
            {
                //
                dynamic templates = reader.Read(templatesResponse.Body);

                foreach (var t in templates)
                {
                    if (!string.IsNullOrWhiteSpace(templateName)
                     && !templateName.Equals(t.name, StringComparison.OrdinalIgnoreCase))
                    {
                        //this seems to be the only way to get single template by name (not slug!)
                        continue;
                    }

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
}
