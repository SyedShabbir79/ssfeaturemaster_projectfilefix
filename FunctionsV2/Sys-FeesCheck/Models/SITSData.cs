using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FunctionsV2.Sys_FeesCheck.Models {
	public class SITSCleanData {
		[OpenApiProperty(Description = "The student's course join code (12345678/1)")]
		[JsonProperty("scjc", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string scjc { get; set; }

		[OpenApiProperty(Description = "The student's sequence number (01, 02 etc)")]
		[JsonProperty("seq2", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string seq2 { get; set; }

		[OpenApiProperty(Description = "The student's student number (12345678)")]
		[JsonProperty("stuc", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string stuc { get; set; }

		[OpenApiProperty(Description = "The amount of fees the student has unpaid (123.45)")]
		[JsonProperty("unpaidFees", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public decimal unpaidFees { get; set; }

		private string sitsAmountRegex { get; } = @"(.*£ )(\d+(\.\d+)?)";

		public SITSCleanData(SITSRawData rawData) {
			this.scjc = rawData.sce_scjc;
			this.seq2 = rawData.sce_seq2;
			this.stuc = rawData.sce_stuc;
			string unpaidFees = Regex.Replace(rawData.sce_udf4, sitsAmountRegex, "$2");
			this.unpaidFees = decimal.Parse(unpaidFees);
		}
	}

	public class SITSRawData {
		[OpenApiProperty(Description = "The student's course join code (12345678/1)")]
		[JsonProperty("sce_scjc", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string sce_scjc { get; set; }

		[OpenApiProperty(Description = "The student's sequence number (01, 02 etc)")]
		[JsonProperty("sce_seq2", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string sce_seq2 { get; set; }

		[OpenApiProperty(Description = "The student's student number (12345678)")]
		[JsonProperty("sce_stuc", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string sce_stuc { get; set; }

		[OpenApiProperty(Description = "The amount of fees the student has unpaid, stored in the 4th user-defined field (udf) in SITS (123.45)")]
		[JsonProperty("sce_udf4", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string sce_udf4 { get; set; }
	}

	public class OutputDataForSITS {
		[OpenApiProperty(Description = "The student's student number (12345678)")]
		[JsonProperty("stuc", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string stuc { get; set; }

		[OpenApiProperty(Description = "The student's course join code (12345678/1)")]
		[JsonProperty("scjc", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string scjc { get; set; }

		[OpenApiProperty(Description = "The student's sequence number (01, 02 etc)")]
		[JsonProperty("seq2", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public string seq2 { get; set; }

		[OpenApiProperty(Description = "The balance the student has left over (123.45)")]
		[JsonProperty("balance", Required = Required.Always)]
		[Required(AllowEmptyStrings = false)]
		public decimal balance { get; set; }

		public OutputDataForSITS(string stuc, string scjc, string seq2, decimal balance) {
			this.stuc = stuc;
			this.scjc = scjc;
			this.seq2 = seq2;
			this.balance = balance;
		}
	}

	public class SITSRawDataExample : OpenApiExample<SITSRawData> {
		public override IOpenApiExample<SITSRawData> Build(NamingStrategy namingStrategy = null) {
			this.Examples.Add(
				OpenApiExampleResolver.Resolve(
					"SITSRawDataExample",
					new SITSRawData() {
						sce_scjc = "12345678/1",
						sce_seq2 = "02",
						sce_stuc = "12345678",
						sce_udf4 = "ONE=£ 1234.56"
					},
					namingStrategy
				)
			);

			return this;
		}
	}
}
