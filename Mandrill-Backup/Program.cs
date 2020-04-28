using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            return templateName.Replace('/','-')
                               .Replace('\\', '-')
                               .Replace('|', '-') 
                             + ".template.json";
        }

        private static string ToJsonPrettyPrint(dynamic obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonConverter[] { new StringEnumConverter() });
        }
        
        static void Main(string[] args)
        {
           // Debugger.Launch();

            var options = new ApplicationArguments();
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {               
                return;
            }

            var dir = Directory.CreateDirectory(options.ExportDir);
            
            switch (options.Action)
            {
                case Action.Export:
                    //setup export writer.                                    
                    ExportTemplatesToFolder(options.Key, dir, options.TemplateName, options.IgnoreDates);
                    break;
                case Action.Import:
                    ImportFromFolderToMandrill(options.Key, dir, options.TemplateName);
                    break;
                case Action.Delete:
                    DeleteTemplates(options.Key,
                            CreateBackupDir(dir),
                            options.TemplateName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static DirectoryInfo CreateBackupDir(DirectoryInfo dir)
        {
            return Directory.CreateDirectory(dir.FullName + "\\" + string.Format("backups-{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now));
        }

        private static void ImportFromFolderToMandrill(string apikey, DirectoryInfo exportDir, string templateName = null)
        {      
            var currentTemplates = GetTemplatesFromAccount(apikey, ignoreDates:true);
            
            var requestedTemplateFileName = !string.IsNullOrWhiteSpace(templateName)
                ? FormatTemplateNameToFileName(templateName)
                : null;

            //foreach file in directory (optionally filtered by requested template name)            
            foreach (var f in exportDir.GetFiles()
                    .Where(f=>string.IsNullOrWhiteSpace(templateName)
                        || f.Name.Equals(requestedTemplateFileName, StringComparison.OrdinalIgnoreCase)
                    ))
            {
               

                var content = File.ReadAllText(f.FullName);
                var templateNameFromFile = f.Name.Replace(".template.json", "");
                if (currentTemplates.ContainsKey(templateNameFromFile.ToLowerInvariant()))
                {
                    if (!content.Equals(currentTemplates[templateNameFromFile]))
                    {
                        //only update when newer
                        UpdateTemplate(apikey, content);
                    }
                    else
                    {
                        Console.WriteLine("Skipping template " + f.Name);
                    }
                }
                else
                {
                    AddTemplateToAccount(apikey, content);
                }
            }
        }

        private static IDictionary<string,string> GetTemplatesFromAccount(string apikey,
                        string templateName = null, bool ignoreDates = false)
        {
            var templatesInAccount= new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            var reader = new JsonReader();
            var httpHelper = new HttpHelper();

            var templatesResponse = httpHelper.Post(Mandrillurl + "/templates/list.json", new { key = apikey }).Result;

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
                    Console.WriteLine("Found template: " + t.name + " - " + t.slug);

                    //if templates are exported to source control and imported into other accounts 
                    //it is usefull to strip out dates; otherwise they will be seen as an update to the file 
                    //and you end it endless loops of updating accounts.
                    if (ignoreDates)
                    {
                        if (HasProperty(t, "published_at")) t.published_at = "2015-01-01 10:10:10";
                        if (HasProperty(t, "created_at")) t.created_at = "2015-01-01 10:10:10";
                        if (HasProperty(t, "updated_at")) t.updated_at = "2015-01-01 10:10:10";
                        if (HasProperty(t, "draft_updated_at")) t.draft_updated_at = "2015-01-01 10:10:10";
                    }

                    if (!templatesInAccount.ContainsKey(t.name))
                        templatesInAccount.Add(t.name, ToJsonPrettyPrint(t));
                    else
                    {
                        //else try add with slug
                        if (!templatesInAccount.ContainsKey(t.slug))
                            templatesInAccount.Add(t.slug, ToJsonPrettyPrint(t));
                        //else just forget
                    }            
                }
            }
            return templatesInAccount;
        }

        private static IDictionary<string,string> ExportTemplatesToFolder(string apikey, 
            DirectoryInfo exportDir,
            string templateName = null, bool ignoreDates = false)
        {

            var templates = GetTemplatesFromAccount(apikey, templateName, ignoreDates);

            foreach (var t in templates)
            {
                Console.WriteLine("Exporting: " + t.Key);

                File.WriteAllText(Path.Combine(exportDir.FullName, FormatTemplateNameToFileName(t.Key)),
                                  t.Value);
            }
            return templates;
        }    

        private static void DeleteTemplates(string apikey, DirectoryInfo backupDir, 
                                               string templateName = null)
        {
            var reader = new JsonReader();
            var httpHelper = new HttpHelper();

            //make backups.. always
            var templates = ExportTemplatesToFolder(apikey, backupDir, templateName);

            if (templates.Any())
            {
                
                foreach (var template in templates)
                {
                    //check if we wanted to delete a single template
                    if (!string.IsNullOrWhiteSpace(templateName)
                     && !templateName.Equals(template.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        //this seems to be the only way to get single template by name (not slug!)
                        continue;
                    }

                    dynamic t = reader.Read(template.Value);

                    //delete, take slug and use as name
                    string name = t.slug;

                    var deleteTemplate = httpHelper.Post(Mandrillurl + "/templates/delete.json", new {key = apikey, name}).Result;

                    Console.WriteLine(string.Format("Template delete result {0}: {1} - {2}", name, deleteTemplate.Code, deleteTemplate.StatusDescription));
                }
            }
        }

        private static void AddTemplateToAccount(string apikey, string templateContent)
        {
            var reader = new JsonReader();
            dynamic template = reader.Read(templateContent);
         
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
        
        private static void UpdateTemplate(string apikey, string templateContent)
        {       
            var reader = new JsonReader();
            dynamic template = reader.Read(templateContent);

            var httpHelper = new HttpHelper();
            if (!string.IsNullOrWhiteSpace(apikey))
            {
                //make sure to rewrite everything to the name
                string name = template.name;

                template.key = apikey;
                template.slug = name;

                var addResponse = httpHelper.Post(Mandrillurl + "/templates/update.json", template).Result;

                Console.WriteLine(string.Format("Template update result {0}: {1} - {2}", template.slug, addResponse.Code, addResponse.StatusDescription));
            }
        }

        internal static bool HasProperty(dynamic d, string propertyname)
        {
            return ((IDictionary<string, object>) d).ContainsKey(propertyname);
        }
    }
}
