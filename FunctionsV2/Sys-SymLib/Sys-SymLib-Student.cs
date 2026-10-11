using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;

namespace FunctionsV2.Sys_SymLib
{
    public class Sys_SymLib_Student
    {
        [FunctionName("Sys_SymLib_FormatStudentData")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(SymLib_StudentDataRequest))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "text/plain", typeof(string))]
        public static async Task<IActionResult> FormatStudentData([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("HTTP function Sys_SymLib_FormatStudentData invoked");

            // get data from request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (String.IsNullOrWhiteSpace(requestBody))
                return new BadRequestObjectResult("Request body is missing");

            // deserialize request body
            SymLib_StudentDataRoot studentDataRoot = JsonConvert.DeserializeObject<SymLib_StudentDataRoot>(requestBody);
            if (studentDataRoot == null)
                return new BadRequestObjectResult("Failed to deserialize request body");

            // get array of student data
            SymLib_Student[] studentData = studentDataRoot.SymLib_Students;
            // get count of students in input array
            int studentCount = studentData.Length;
            // check we have some students
            if (studentData == null || studentCount == 0)
                return new BadRequestObjectResult("Request contains no student data");

            // get campus parameter
            string campus = studentDataRoot.SymLib_Campus;
            if (String.IsNullOrWhiteSpace(campus) || (! new[] {"UK", "MY"}.Contains(campus)))
                return new BadRequestObjectResult("Campus parameter missing or invalid");

            // format students using SymLib_StudentRecord constructor method
            SymLib_StudentRecord[] formattedStudents = new SymLib_StudentRecord[studentCount];
            for (int i = 0; i < studentCount; i++)
            {
                SymLib_StudentRecord studentOut = new SymLib_StudentRecord(studentData[i], campus);
                formattedStudents[i] = studentOut;
            }

            // convert each formatted student to a string and concatenate for output
            string outputString = "";
            foreach (SymLib_StudentRecord studentOut in formattedStudents)
            {
                string studentString = studentOut.StudentRecord(campus);
                if (studentString != null)
                {
                    outputString += studentString + "\n";
                }
            }

            return new OkObjectResult(outputString);

        }
    }
}

public class SymLib_StudentDataRequest 
{
    public string SymLib_Campus { get; set; }

    public object SymLib_Students { get; set; }
}

public class SymLib_StudentDataRoot : SymLib_StudentDataRequest
{ 
    public new SymLib_Student[] SymLib_Students { get; set; }
}

public class SymLib_Student
{
    public string STU_CODE { get; set; }
    public string EMOS_MAG { get; set; }
    public string Title { get; set; }
    public string Name { get; set; }
    public string Library { get; set; }
    public string Profile { get; set; }
    public string Category1 { get; set; }
    public string Category2 { get; set; }
    public string Dept_Code { get; set; }
    public string End_Date { get; set; }
    public string EMOS_PIN { get; set; }
    public string SCJ_STAC { get; set; }
    public string Dept { get; set; }
    public string Username { get; set; }
    public string Add_Line1 { get; set; }
    public string Add_Line2 { get; set; }
    public string Add_Line3 { get; set; }
    public string Add_Line4 { get; set; }
    public string Add_Line5 { get; set; }
    public string RISIS_Note { get; set; }
    public string Course_Code { get; set; }
    public string Email_Address { get; set; }
    public int EMOS_Ready { get; set; }
}

public class SymLib_StudentRecord
{
    public const string DocBoundary = "*** DOCUMENT BOUNDARY ***";
    public const string Form = "FORM=LDUSER";
    public const string Address1Begin = ".USER_ADDR1_BEGIN.";
    public const string Address1End = ".USER_ADDR1_END.";
    public const string Address2Begin = ".USER_ADDR2_BEGIN.";
    public const string Address2End = ".USER_ADDR2_END.";
    public const string XInfoBegin = ".USER_XINFO_BEGIN.";
    public const string XInfoEnd = ".USER_XINFO_END.";
    public const string UserChgHistRule = ".USER_CHG_HIST_RULE.   |aCIRCRULE";

    private string _User_Id;
    private string _User_Alt_Id;
    private string _User_Title;
    private string _User_Name;
    private string _User_Library;
    private string _User_Profile;
    private string _User_Category1;
    private string _User_Category2;
    private string _User_Department;
    private string _User_Priv_Expires;
    private string _User_Pin;
    private string _User_Status;
    private string _Department;
    private string _Email;
    private string _Line1;
    private string _Line2;
    private string _Line3;
    private string _Line4;
    private string _Line5;
    private string _Hold_Id;
    private string _Risis_Note;
    private string _User_Web_Auth;

    public string User_Id
    {
        get => (string)_User_Id;
        set { _User_Id = $".USER_ID.   |a{value}"; }
    }
    public string User_Alt_Id
    {
        get => (string)_User_Alt_Id;
        set { _User_Alt_Id = $".USER_ALT_ID.   |a{value}"; }
    }
    public string User_Title
    {
        get => (string)_User_Title;
        set { _User_Title = $".USER_TITLE.   |a{value}"; }
    }
    public string User_Name
    {
        get => (string)_User_Name;
        set { _User_Name = $".USER_NAME.   |a{RemoveNonLatinCharacters(value)}"; }
    }
    public string User_Library
    {
        get => (string)_User_Library;
        set { _User_Library = $".USER_LIBRARY.   |a{value}"; }
    }
    public string User_Profile
    {
        get => (string)_User_Profile;
        set { _User_Profile = $".USER_PROFILE.   |a{value}"; }
    }
    public string User_Category1
    {
        get => (string)_User_Category1;
        set { _User_Category1 = $".USER_CATEGORY1.   |a{value}"; }
    }
    public string User_Category2
    {
        get => (string)_User_Category2;
        set { if (value != null) _User_Category2 = $".USER_CATEGORY2.   |a{value}"; }
    }
    public string User_Department
    {
        get => (string)_User_Department;
        set
        {
            if (value != null && value.Length > 10)
                _User_Department = $".USER_DEPARTMENT.   |a{value[..10]}";
            else
                _User_Department = $".USER_DEPARTMENT.   |a{value}";
        }
    }
    public string User_Priv_Expires
    {
        get => (string)_User_Priv_Expires;
        set { _User_Priv_Expires = $".USER_PRIV_EXPIRES.   |a{value}"; }
    }
    public string User_Pin
    {
        get => (string)_User_Pin;
        set { _User_Pin = $".USER_PIN.   |a{value}"; }
    }
    public string User_Status
    {
        get => (string)_User_Status;
        set { if (value != null) _User_Status = $".USER_STATUS.   |a{value}"; }
    }
    public string Department
    {
        get => (string)_Department;
        set { _Department = $".DEPARTMENT.   |a{value}"; }
    }
    public string Email
    {
        get => (string)_Email;
        set { _Email = $".EMAIL.   |a{value}"; }
    }
    public string Line1
    {
        get => (string)_Line1;
        set { _Line1 = $".LINE.   |a{RemoveNonLatinCharacters(value)}"; }
    }
    public string Line2
    {
        get => (string)_Line2;
        set { _Line2 = $".LINE.   |a{RemoveNonLatinCharacters(value)}"; }
    }
    public string Line3
    {
        get => (string)_Line3;
        set { _Line3 = $".LINE.   |a{RemoveNonLatinCharacters(value)}"; }
    }
    public string Line4
    {
        get => (string)_Line4;
        set { _Line4 = $".LINE.   |a{RemoveNonLatinCharacters(value)}"; }
    }
    public string Line5
    {
        get => (string)_Line5;
        set { _Line5 = $".LINE.   |a{RemoveNonLatinCharacters(value)}"; }
    }
    public string Hold_Id
    {
        get => (string)_Hold_Id;
        set { _Hold_Id = $".HOLD_ID.   |a{value}"; }
    }
    public string Risis_Note
    {
        get => (string)_Risis_Note;
        set { if (value != null) _Risis_Note = $".RISIS_NOTE.   |a{RemoveNonLatinCharacters(value)}"; }
    }
    public string User_Web_Auth
    {
        get => (string)_User_Web_Auth;
        set { _User_Web_Auth = $".USER_WEB_AUTH.   |a{value}"; }
    }

    public string StudentRecord(string campus)
    {
        string[] record = new string[31];
        record[0] = DocBoundary;
        record[1] = Form;
        record[2] = this.User_Id;
        record[3] = this.User_Alt_Id;
        record[4] = this.User_Title;
        record[5] = this.User_Name;
        record[6] = this.User_Library;
        record[7] = this.User_Profile;
        record[8] = this.User_Category1;
        record[9] = this.User_Category2;
        record[10] = this.User_Department;
        record[11] = this.User_Priv_Expires;
        record[12] = this.User_Pin;
        if (campus == "UK")
        {
            record[13] = this.User_Status;
        }
        record[14] = Address1Begin;
        record[15] = this.Department;
        record[16] = this.Email;
        record[17] = Address1End;
        record[18] = Address2Begin;
        record[19] = this.Line1;
        record[20] = this.Line2;
        record[21] = this.Line3;
        record[22] = this.Line4;
        record[23] = this.Line5;
        record[24] = Address2End;
        record[25] = XInfoBegin;
        if (campus == "UK")
        {
            record[26] = this.Hold_Id;
            record[27] = this.Risis_Note;
        }
        record[28] = XInfoEnd;
        record[29] = this.User_Web_Auth;
        if (campus == "UK")
        {
            record[30] = UserChgHistRule;
        }
        return string.Join("\n", record.Where(s => !string.IsNullOrEmpty(s)));
    }

    public SymLib_StudentRecord(SymLib_Student studentIn, string campus)
    {
        if (studentIn.EMOS_Ready == 0)
        {
            User_Id = studentIn.STU_CODE;
        }
        else
        {
            User_Id = studentIn.EMOS_MAG;
        }
        User_Alt_Id = studentIn.STU_CODE;
        User_Title = studentIn.Title;
        User_Name = studentIn.Name;
        User_Library = studentIn.Library;
        User_Profile = studentIn.Profile;
        User_Category1 = studentIn.Category1;
        User_Category2 = studentIn.Category2;
        User_Department = studentIn.Dept_Code;
        User_Priv_Expires = studentIn.End_Date;
        User_Pin = studentIn.EMOS_PIN;
        if (campus == "UK")
        {
            User_Status = studentIn.SCJ_STAC;
        }
        Department = studentIn.Dept;
        Email = studentIn.Email_Address;
        Line1 = studentIn.Add_Line1;
        Line2 = studentIn.Add_Line2;
        Line3 = studentIn.Add_Line3;
        Line4 = studentIn.Add_Line4;
        Line5 = studentIn.Add_Line5;
        if (campus == "UK")
        {
            Hold_Id = studentIn.STU_CODE;
            Risis_Note = studentIn.RISIS_Note;
        }
        User_Web_Auth = studentIn.Username;
    }

    private static string RemoveNonLatinCharacters(string value)
    {
        string output = value;
        if (!string.IsNullOrEmpty(output))
        {
            output = Regex.Replace(value, @"[^\u0020-\u007E]", string.Empty);
        }
        return output;
    }

}



