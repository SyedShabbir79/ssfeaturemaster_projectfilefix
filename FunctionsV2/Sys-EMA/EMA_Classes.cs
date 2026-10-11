using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FunctionsV2.Sys_EMA.EMA_Classes;

namespace FunctionsV2.Sys_EMA
{
    internal class EMA_Classes
    {

        // EMA_Grades_ExpandGrade
        public class GradesRequest
        {
            [JsonProperty("grades")]
            public List<GradeIn> grades { get; set; }
            [JsonProperty("footer")]
            public ReqFooter footer { get; set; }
        }

        public class GradesResponse : GradesRequest
        {
            [JsonProperty("gradesOut")]
            new public List<GradeOut> grades { get; set; }
            [JsonProperty("error")]
            public string error { get; set; }
            [JsonProperty("hasError")]
            public bool hasError { get; set; }
        }

        public class ReqFooter
        {
            [JsonProperty("scheduled_dt")]
            public string scheduled_dt { get; set; }
            [JsonProperty("record_count")]
            public string record_count { get; set; }
        }

        public class GradeIn
        {
            [JsonProperty("stuCode")]
            public string stuCode { get; set; }
            [JsonProperty("courseId")]
            public string courseId { get; set; }
            [JsonProperty("columnId")]
            public string columnId { get; set; }
            [JsonProperty("pointsPossible")]
            public string pointsPossible { get; set; }
            [JsonProperty("displayGrade")]
            public string displayGrade { get; set; }
            [JsonProperty("percentageMark")]
            public string percentageMark { get; set; }
            [JsonProperty("optionalData")]
            public string optionalDataIn { get; set; }
        }

        public class GradeOut : GradeIn
        {
            [JsonProperty("optionalDataOut")]
            public OptionalData optionalDataOut { get; set; }
            [JsonProperty("optionalDataString")]
            public string optionalDataString { get; set; }
        }

        public class OptionalData
        {
            [JsonProperty("islate")]
            public int islate { get; set; }
            [JsonProperty("mks_code")]
            public string mks_code { get; set; }
            [JsonProperty("mab_type")]
            public string mab_type { get; set; }
            [JsonProperty("mab_seq")]
            public string mab_seq { get; set; }
            [JsonProperty("map_code")]
            public string map_code { get; set; }
            [JsonProperty("psl_code")]
            public string psl_code { get; set; }
            [JsonProperty("mav_occ")]
            public string mav_occ { get; set; }
            [JsonProperty("ayr_code")]
            public string ayr_code { get; set; }
            [JsonProperty("mod_code")]
            public string mod_code { get; set; }
            [JsonProperty("ic_id")]
            public string ic_id { get; set; }
            [JsonProperty("crs_id")]
            public string crs_id { get; set; }
        }

        // EMA_Grades_ParseAssessment

        public class InMabCamsList
        {
            [JsonProperty("mab.cams")]
            public List<InMabMad> mabCams { get; set; }
        }

        public class InMabCams
        {
            [JsonProperty("mab.cams")]
            public List<InMab> mabCams { get; set; }
        }

        public class InMab
        {
            [JsonProperty("map_code.mab.cams")]
            public string mab_map_code { get; set; }

            [JsonProperty("mab_seq.mab.cams")]
            public string mab_mab_seq { get; set; }

            [JsonProperty("ast_code.mab.cams")]
            public string mab_ast_code { get; set; }

            [JsonProperty("mab_perc.mab.cams")]
            public string mab_mab_perc { get; set; }

            [JsonProperty("mab_tsha.mab.cams")]
            public string mab_mab_tsha { get; set; }

            [JsonProperty("mab_name.mab.cams")]
            public string mab_mab_name { get; set; }

            [JsonProperty("mks_code.mab.cams")]
            public string mab_mks_code { get; set; }

            [JsonProperty("mab_agrp.mab.cams")]
            public string mab_mab_agrp { get; set; }

            [JsonProperty("mab_rasi.mab.cams")]
            public object mab_mab_rasi { get; set; }

            [JsonProperty("mab_udf1.mab.cams")]
            public string mab_mab_udf1 { get; set; }

            [JsonProperty("mab_udfa.mab.cams")]
            public string mab_mab_udfa { get; set; }

            [JsonProperty("mab_udfb.mab.cams")]
            public object mab_mab_udfb { get; set; }

            [JsonProperty("mab_udfc.mab.cams")]
            public object mab_mab_udfc { get; set; }

            [JsonProperty("mab_udfd.mab.cams")]
            public string mab_mab_udfd { get; set; }

            [JsonProperty("mab_udfe.mab.cams")]
            public string mab_mab_udfe { get; set; }
        }

        public class Mab
        {
            [JsonProperty("map_code")]
            public string mab_map_code { get; set; }

            [JsonProperty("mab_seq")]
            public string mab_mab_seq { get; set; }

            [JsonProperty("ast_code")]
            public string mab_ast_code { get; set; }

            [JsonProperty("mab_perc")]
            public string mab_mab_perc { get; set; }

            [JsonProperty("mab_tsha")]
            public string mab_mab_tsha { get; set; }

            [JsonProperty("mab_name")]
            public string mab_mab_name { get; set; }

            [JsonProperty("mks_code")]
            public string mab_mks_code { get; set; }

            [JsonProperty("mab_agrp")]
            public string mab_mab_agrp { get; set; }

            [JsonProperty("mab_rasi")]
            public object mab_mab_rasi { get; set; }

            [JsonProperty("mab_udf1")]
            public string mab_mab_udf1 { get; set; }

            [JsonProperty("mab_udfa")]
            public string mab_mab_udfa { get; set; }

            [JsonProperty("mab_udfb")]
            public object mab_mab_udfb { get; set; }

            [JsonProperty("mab_udfc")]
            public object mab_mab_udfc { get; set; }

            [JsonProperty("mab_udfd")]
            public string mab_mab_udfd { get; set; }

            [JsonProperty("mab_udfe")]
            public string mab_mab_udfe { get; set; }

            [JsonProperty("mab_type")]
            public string mab_mab_type { get; set; }

            [JsonProperty("mads")]
            public List<Mad> mab_mad { get; set; }
        }

        public class InMabMad : InMab
        {

            [JsonProperty("mad")]
            public InMadCams mad { get; set; }
        }

        public class InMadCams
        {
            [JsonProperty("mad.cams")]
            public List<InMad> madCams { get; set; }
        }

        public class InMad
        {
            [JsonProperty("mod_code.mad.cams")]
            public string mad_mod_code { get; set; }

            [JsonProperty("mav_occur.mad.cams")]
            public string mad_mav_occur { get; set; }

            [JsonProperty("ayr_code.mad.cams")]
            public string mad_ayr_code { get; set; }

            [JsonProperty("psl_code.mad.cams")]
            public string mad_psl_code { get; set; }

            [JsonProperty("map_code.mad.cams")]
            public string mad_map_code { get; set; }

            [JsonProperty("mab_seq.mad.cams")]
            public string mad_mab_seq { get; set; }

            [JsonProperty("mad_ddate.mad.cams")]
            public string mad_mad_ddate { get; set; }

            [JsonProperty("mad_duet.mad.cams")]
            public string mad_mad_duet { get; set; }
        }

        public class Mad
        {
            [JsonProperty("mod_code")]
            public string mad_mod_code { get; set; }

            [JsonProperty("mav_occur")]
            public string mad_mav_occur { get; set; }

            [JsonProperty("ayr_code")]
            public string mad_ayr_code { get; set; }

            [JsonProperty("psl_code")]
            public string mad_psl_code { get; set; }

            [JsonProperty("map_code")]
            public string mad_map_code { get; set; }

            [JsonProperty("mab_seq")]
            public string mad_mab_seq { get; set; }

            [JsonProperty("mad_ddate")]
            public string mad_mad_ddate { get; set; }

            [JsonProperty("mad_duet")]
            public string mad_mad_duet { get; set; }

            [JsonProperty("mab_type")]
            public string mad_mab_type { get; set; }
        }

        public class InMavCams
        {
            [JsonProperty("mav.cams")]
            public List<InRootMav> mavCams { get; set; }
        }

        public class InMavCamsNoMabs
        {
            [JsonProperty("mav.cams")]
            public List<InMav> mavCams { get; set; }
        }

        public class InMav
        {
            [JsonProperty("mod_code.mav.cams")]
            public string mav_mod_code { get; set; }

            [JsonProperty("mav_occur.mav.cams")]
            public string mav_mav_occur { get; set; }

            [JsonProperty("ayr_code.mav.cams")]
            public string mav_ayr_code { get; set; }

            [JsonProperty("psl_code.mav.cams")]
            public string mav_psl_code { get; set; }

            [JsonProperty("map_code.mav.cams")]
            public string mav_map_code { get; set; }
        }

        public class Mav
        {
            [JsonProperty("mod_code")]
            public string mav_mod_code { get; set; }

            [JsonProperty("mav_occur")]
            public string mav_mav_occur { get; set; }

            [JsonProperty("ayr_code")]
            public string mav_ayr_code { get; set; }

            [JsonProperty("psl_code")]
            public string mav_psl_code { get; set; }

            [JsonProperty("map_code")]
            public string mav_map_code { get; set; }

            [JsonProperty("mabs")]
            public List<Mab> mav_mabs { get; set; }
        }

        public class InRootMav : InMav
        {
            [JsonProperty("mab")]
            public InMabCamsList mab { get; set; }
        }

        public class InRootMab : InMabMad
        {
            [JsonProperty("mav")]
            public InMavCamsNoMabs mav { get; set; }
        }

        public class InRootMad : InMad
        {
            [JsonProperty("mab")]
            public InMabCams mab { get; set; }

            [JsonProperty("mav")]
            public InMavCamsNoMabs mav { get; set; }
        }

        public class MavRaw
        {
            public bool hasError { get; set; }
            public string error { get; set; }

            public Mav mav { get; set; }

            public object raw { get; set; }
        }

        public class InMavInner
        {
            [JsonProperty("mav")]
            public InMavCams mav { get; set; }
        }

        public class InMavRequest
        {
            [JsonProperty("risis_ema_mav_asses")]
            public InMavInner mavHeader { get; set; }
        }
        public class InMabOuterUpdate
        {
            [JsonProperty("mab")]
            public InMabInnerUpdate mab { get; set; }
        }
        public class InMabOuterCreate
        {
            [JsonProperty("mab")]
            public InMabInnerCreate mab { get; set; }
        }
        public class InMabOuterDelete
        {
            [JsonProperty("mab")]
            public InMabInnerDelete mab { get; set; }
        }

        public class InMabInnerUpdate
        {
            [JsonProperty("update.mab.cams")]
            //public InRootMab mabCams { get; set; }
            public List<InRootMab> mabCams { get; set; }
        }
        public class InMabInnerCreate
        {
            [JsonProperty("create.mab.cams")]
            //public InRootMab mabCams { get; set; }
            public List<InRootMab> mabCams { get; set; }
        }
        public class InMabInnerDelete
        {
            [JsonProperty("delete.mab.cams")]
            //public InRootMab mabCams { get; set; }
            public List<InRootMab> mabCams { get; set; }
        }
        public class InMadOuterUpdate
        {
            [JsonProperty("mad")]
            public InMadInnerUpdate mad { get; set; }
        }
        public class InMadOuterCreate
        {
            [JsonProperty("mad")]
            public InMadInnerCreate mad { get; set; }
        }

        public class InMadOuterDelete
        {
            [JsonProperty("mad")]
            public InMadInnerDelete mad { get; set; }
        }

        public class InMadInnerUpdate
        {
            [JsonProperty("update.mad.cams")]
            //public InRootMad madCams { get; set; }
            public List<InRootMad> madCams { get; set; }
        }
        public class InMadInnerCreate
        {
            [JsonProperty("create.mad.cams")]
            //public InRootMad madCams { get; set; }
            public List<InRootMad> madCams { get; set; }
        }

        public class InMadInnerDelete
        {
            [JsonProperty("delete.mad.cams")]
            //public InRootMad madCams { get; set; }
            public List<InRootMad> madCams { get; set; }
        }

        public class InMabRequestUpdate
        {
            [JsonProperty("risis_ema_mab_asses")]
            public InMabOuterUpdate mabHeader { get; set; }
        }

        public class InMabRequestCreate
        {
            [JsonProperty("risis_ema_mab_asses")]
            public InMabOuterCreate mabHeader { get; set; }
        }

        public class InMabRequestDelete
        {
            [JsonProperty("risis_ema_mab_asses")]
            public InMabOuterDelete mabHeader { get; set; }
        }

        public class InMadRequestUpdate
        {
            [JsonProperty("risis_ema_mad_asses")]
            public InMadOuterUpdate madHeader { get; set; }
        }
        public class InMadRequestCreate
        {
            [JsonProperty("risis_ema_mad_asses")]
            public InMadOuterCreate madHeader { get; set; }
        }
        public class InMadRequestDelete
        {
            [JsonProperty("risis_ema_mad_asses")]
            public InMadOuterDelete madHeader { get; set; }
        }

        // EMA_Grades_SendAssessment
        public class AssessmentIn
        {
            [JsonProperty("courseId")]
            public string courseId { get; set; }
            [JsonProperty("itemType")]
            public string itemType { get; set; }
            [JsonProperty("name")]
            public string name { get; set; }
            [JsonProperty("optionalData")]
            public string optionalData { get; set; }
            [JsonProperty("startDate")]
            public string startDate { get; set; }
            [JsonProperty("dueDate")]
            public string dueDate { get; set; }
            [JsonProperty("endDate")]
            public string endDate { get; set; }
            [JsonProperty("group")]
            public int group { get; set; }
            [JsonProperty("points")]
            public float points { get; set; }
            [JsonProperty("available")]
            public int available { get; set; }
            [JsonProperty("extract")]
            public int extract { get; set; }
            [JsonProperty("attempts")]
            public object attempts { get; set; }//?
            [JsonProperty("gradeSchema")]
            public object gradeSchema { get; set; }//?
        }

        public class AssessmentOut
        {
            [JsonProperty("courseId")]
            public string courseId { get; set; }
            [JsonProperty("itemType")]
            public string itemType { get; set; }
            [JsonProperty("name")]
            public string name { get; set; }
            [JsonProperty("optionalData")]
            public string optionalData { get; set; }
            [JsonProperty("startDate")]
            public string startDate { get; set; }
            [JsonProperty("dueDate")]
            public string dueDate { get; set; }
            [JsonProperty("endDate")]
            public string endDate { get; set; }
            [JsonProperty("group")]
            public string group { get; set; }
            [JsonProperty("points")]
            public float points { get; set; }
            [JsonProperty("available")]
            public string available { get; set; }
            [JsonProperty("extract")]
            public string extract { get; set; }
            [JsonProperty("attempts")]
            public object attempts { get; set; }//?
            [JsonProperty("gradeSchema")]
            public object gradeSchema { get; set; }//?
        }

    }
}
