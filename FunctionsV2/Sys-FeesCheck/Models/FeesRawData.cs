using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FunctionsV2.Sys_FeesCheck.Models {
  [OpenApiExample(typeof(FeesRawDataExample))]
  public class FeesRawData {
    [OpenApiProperty(Description = "The raw data from Agresso (serialized as a JSON array in the request body)")]
    [JsonProperty("agressoRawData", Required = Required.Always)]
    [Required(AllowEmptyStrings = false)]
    public List<AgressoRawData> agressoRawData { get; set; }

    [OpenApiProperty(Description = "The raw data from SITS (serialized as a JSON array in the request body)")]
    [JsonProperty("sitsRawData", Required = Required.Always)]
    [Required(AllowEmptyStrings = false)]
    public List<SITSRawData> sitsRawData { get; set; }
  }
  public class FeesRawDataExample : OpenApiExample<FeesRawData> {
    public override IOpenApiExample<FeesRawData> Build(NamingStrategy namingStrategy = null) {
      this.Examples.Add(
        OpenApiExampleResolver.Resolve(
          "FeesRawDataExample",
          new FeesRawData() {
            agressoRawData = new List<AgressoRawData>(),
            sitsRawData = new List<SITSRawData>()
          },
          namingStrategy
        )
      );

      return this;
    }
  }
}