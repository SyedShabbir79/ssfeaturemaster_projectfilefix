using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Helpers.Models
{
    public class BlobRequest
    {
        public string Container {  get; set; }
        // Filename should be specified without a file extension
        public string FileName { get; set; }
        public string Content {  get; set; }
    }
}
