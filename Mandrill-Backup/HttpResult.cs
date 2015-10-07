using System.Net;

namespace Mandrill_Backup
{
    public class HttpResult
    {
        public HttpStatusCode Code { get; set; }
        public string StatusDescription { get; set; }
        public string Body { get; set; }
    }
}