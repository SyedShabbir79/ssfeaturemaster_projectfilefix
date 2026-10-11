using System.Linq;
using System.Xml.Linq;

namespace FunctionsV2.Sys_CampusJobs.Services
{
    public static class CommonCampusJobsService
    {
        //Get CustomField values from XML (For old SOAP APIs)
        public static string GetCustomFieldValueXml(string fieldName, XDocument doc)
        {
            XElement fieldNameElement = doc.Descendants("fields")
                           .FirstOrDefault(f => (string)f.Element("displayName") == fieldName);

            return fieldNameElement?.Descendants("value").FirstOrDefault()?.Value ?? null;
        }
    }
}
