using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace FunctionsV2.Sys_FeesCheck {
  public class AgressoRawData {
    #nullable enable
    [OpenApiProperty(Description="The student's ID (12345678, nullable)")]
    [JsonProperty("student_id", Required = Required.AllowNull)]
    public string? student_id { get; set; }

    [OpenApiProperty(Description = "Tab field (usually 'A', nullable)")]
    [JsonProperty("tab", Required = Required.AllowNull)]
    public string? tab { get; set; }

    [OpenApiProperty(Description = "The financial year in which the entry was created (2022, 2023 (etc), nullable)")]
    [JsonProperty("fy", Required = Required.AllowNull)]
    public int? fy { get; set; }
    #nullable disable

    [OpenApiProperty(Description = "The record number (usually 0, can be 1 for 'null' object)")]
    [JsonProperty("_recno", Required = Required.Always)]
    [Required(AllowEmptyStrings = false)]
    public int _recno { get; set; }

    [OpenApiProperty(Description = "The section field (usually 'D', can be 'S' for 'null' object)")]
    [JsonProperty("_section", Required = Required.Always)]
    [Required(AllowEmptyStrings = false)]
    public string _section { get; set; }

    [OpenApiProperty(Description = "The amount paid in the transaction")]
    [JsonProperty("paid", Required = Required.Always)]
    [Required(AllowEmptyStrings = false)]
    public decimal paid { get; set; }
	}

  public class OutputDataForAgresso {
    [OpenApiProperty(Description = "The student's ID (12345678)")]
    [JsonProperty("student_id", Required = Required.Always)]
    [Required(AllowEmptyStrings = false)]
    public string student_id { get; set; }

    [OpenApiProperty(Description = "The total amount paid in all transactions")]
    [JsonProperty("amount", Required = Required.Always)]
    [Required(AllowEmptyStrings = false)]
    public decimal amount { get; set; }

    public OutputDataForAgresso(string student_id, decimal amount) {
      this.student_id = student_id;
      this.amount = amount;
    }
  }

  public class AgressoRawDataExample : OpenApiExample<AgressoRawData> {
    public override IOpenApiExample<AgressoRawData> Build(NamingStrategy namingStrategy = null) {
      this.Examples.Add(
        OpenApiExampleResolver.Resolve(
          "AgressoRawDataExample",
          new AgressoRawData() {
            student_id = "12345678",
            tab = "A",
            fy = 2023,
            _recno = 0,
            _section = "D",
            paid = (decimal)1234.56
          },
          namingStrategy
        )
      );

      return this;
    }
  }
}