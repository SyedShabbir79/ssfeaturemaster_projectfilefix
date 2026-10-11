using System.Collections.Generic;

namespace FunctionsV2.Helpers.Models
{
    public class AddOrUpdateTSResponse
    {
        public List<string> FieldsChanged { get; set; } = new List<string>();

        public bool NewRecord { get; set; }
    }
}
