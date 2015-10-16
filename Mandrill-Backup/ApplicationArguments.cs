using CommandLine;
using CommandLine.Text;

namespace Mandrill_Backup
{

    enum Action
    {
        Export = 1,
        Import = 2,
        Delete = 3
    }

    class ApplicationArguments
    {
        [Option('e', "Export Directory", Required = true, HelpText = "Directory for templates")]
        public string ExportDir { get; set; }
        [Option('k', "Key, Mandrill ApiKey", Required = true, HelpText = "Mandrill ApiKey")]
        public string Key { get; set; }
        [Option('a', "Action; Export, Import", Required = true, HelpText = "Export or import templates")]
        public Action Action { get; set; }
        [Option('t', "Optional template name", Required = false, HelpText = "Import/Export single template")]
        public string TemplateName { get; set; }
        [Option('d', "Ignore dates", Required = false, HelpText = "Ignore date fields in meta data")]
        public bool IgnoreDates { get; set; }
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}