using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Sys_HRMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static FunctionsV2.Sys_HRMS.HRMS_LMSTimeZone;
using static FunctionsV2.Sys_HRMS.HRMS_Location;
using static FunctionsV2.Sys_HRMS.HRMS_OverseasStaff;
using static FunctionsV2.Sys_HRMS.HRMS_PayCategory;


namespace FunctionsV2.Sys_HRMS.Services
{
    public static class ProcessSapDataService
    {
        public static BaseFunctionResponse ConstructEnrichedSapPayload(MessageRoot messageRoot)
        {
            try
            {
                ResponseRoot responseRoot = new();
                SapEmployee sapEmployee = messageRoot.Person;
                MainPositionDTT mainPositionDTT = messageRoot.MainPositionDTT;

                Person person = PopulateEmployeeObject(sapEmployee);
                responseRoot = PopulatePositionObject(responseRoot, sapEmployee, person);

                if(responseRoot.HasError)
                {
                    return new BaseFunctionResponse 
                    { 
                        Response = new
                        {
                            result = responseRoot
                        },
                        Error = new BaseFunctionResponseError 
                        {
                            Code = $"{(int)ErrorCodes.BadRequest}",
                            Message = $"Validation failed when constructing position object. " +
                                $"{responseRoot.Errors.Count} error(s) have been returned: " +
                                $"{string.Join(", ", responseRoot.Errors
                                    .GroupBy(item => item)
                                    .Select(group => $"x{group.Count()} : '{group.Key}'"))}"
                        }
                    };

                }

                responseRoot.Person.Applications.dtt = new MainPositionDTT()
                {
                    FacultyDirectorateCode = mainPositionDTT.FacultyDirectorateCode,
                    ItsCode = mainPositionDTT.ItsCode,
                    RISISCode = mainPositionDTT.RISISCode,
                    PrimaryEmailDomain = mainPositionDTT.PrimaryEmailDomain,
                    SecondaryEmailDomain = mainPositionDTT.SecondaryEmailDomain,
                };

                return new BaseFunctionResponse
                {
                    Response = new
                    {
                        result = responseRoot
                    }
                };
            }
            catch (Exception ex)
            {
                return new BaseFunctionResponse
                {
                    Error = new BaseFunctionResponseError
                    {
                        Code = $"{((int)ErrorCodes.Unknown)}",
                        Message = $"{ex.Message}"
                    }
                };
            }
        }

        private static Person PopulateEmployeeObject(SapEmployee sapEmployee)
        {
            return new Person
            {
                Personal_Reference = RemoveStaffNoPrefix(sapEmployee.Sap_Personal_Reference),
                Title = sapEmployee.Sap_Title,
                Username = CalcUsername(sapEmployee.Sap_Upn),
                Forename1 = sapEmployee.Sap_Forename1,
                Forename2 = sapEmployee.Sap_Forename2,
                Forename3 = sapEmployee.Sap_Forename3,
                Surname = sapEmployee.Sap_Surname,
                Preferred_Name = sapEmployee.Sap_Preferred_Name,
                Known_as_Surname = sapEmployee.Sap_Known_as_Surname,
                Uni_Start_Date = sapEmployee.Sap_Uni_Start_Date,
                Uni_Leave_Date = MapDate(sapEmployee.Sap_Uni_Leave_Date),
                Employee_Start_Date = sapEmployee.Sap_Uni_Start_Date,
                Employee_Leave_Date = MapDate(sapEmployee.Sap_Uni_Leave_Date),
                Extension_Date = null,
                Email_Address = sapEmployee.Sap_Email_Address,
                Bank_Account = sapEmployee.Sap_Bank_Account,
                Sort_Code = sapEmployee.Sap_Sort_Code,
                Roll_Number = sapEmployee.Sap_Roll_Number,
                Address_Line1 = sapEmployee.Sap_Address_Line1,
                Address_Line2 = sapEmployee.Sap_Address_Line2,
                City = sapEmployee.Sap_City,
                County = sapEmployee.Sap_County,
                Post_Code = sapEmployee.Sap_Post_Code,
                Country = sapEmployee.Sap_Country,
                Gender = sapEmployee.Sap_Gender,
                Staff_Type = sapEmployee.Sap_Staff_Type,
                Work_Internal_Phone = sapEmployee.Sap_Work_Internal_Phone,
                Dpa_Module_Completed = sapEmployee.Sap_Dpa_Module_Completed,
                Foi_Module_Completed = sapEmployee.Sap_Foi_Module_Completed,
                Infosec_Module_Completed = sapEmployee.Sap_Infosec_Module_Completed,
                Under_18 = sapEmployee.Sap_Under_18,
                Unique_Id = null,
                Hesa_Id = sapEmployee.Sap_Hesa_Id,
                Risis_Tutor = sapEmployee.Sap_Risis_Tutor,
                Initials = CalcInitials(sapEmployee.Sap_Forename2, sapEmployee.Sap_Forename3),
                On_Payroll = sapEmployee.Sap_On_Payroll,
                Upn = sapEmployee.Sap_Upn,
            };
        }

        private static ResponseRoot PopulatePositionObject(ResponseRoot responseRoot, SapEmployee sapEmployee, Person person)
        {
            List<Position> positions = new List<Position>();

            foreach (Sap_Position sapPosition in sapEmployee.Sap_Positions)
            {
                Position pos = ConstructPositionObject(sapEmployee, sapPosition);

                StaffLocation locationResponse = new(sapPosition.Sap_Location);
                if (locationResponse != null && locationResponse.hasError)
                {
                    responseRoot.HasError = true;
                    responseRoot.Errors.Add(new PositionError(pos.Position_Reference, locationResponse.error));
                }
                else
                {
                    pos.Location = locationResponse.location;
                }

                StaffPayCategory payCategoryResponse = new(sapPosition.Sap_Employee_Group, sapPosition.Sap_Employee_Subgroup, sapPosition.Sap_Pay_Grade);
                if (payCategoryResponse != null && payCategoryResponse.hasError)
                {
                    responseRoot.HasError = true;
                    responseRoot.Errors.Add(new PositionError(pos.Position_Reference, payCategoryResponse.error));
                }
                else
                {
                    pos.Pay_Category = payCategoryResponse.payCategory;
                }

                if (pos.Main_Position == 1)
                {
                    OverseasStaff overseasStaffResponse = new(pos.School, sapEmployee.Sap_On_Payroll, pos.Location);
                    if (overseasStaffResponse != null && overseasStaffResponse.hasError)
                    {
                        responseRoot.HasError = true;
                        responseRoot.Errors.Add(new PositionError(pos.Position_Reference, overseasStaffResponse.error));
                    }
                    else
                    {
                        person.Overseas_Staff = overseasStaffResponse.staffCode;
                    }

                    LMSTimeZone lMSTimeZoneResponse = new(person.Overseas_Staff);
                    if (lMSTimeZoneResponse != null && lMSTimeZoneResponse.hasError)
                    {
                        responseRoot.HasError = true;
                        responseRoot.Errors.Add(new PositionError(pos.Position_Reference, lMSTimeZoneResponse.error));
                        person.Applications = new Applications();
                    }
                    else
                    {
                        person.Applications = new Applications() { lms = new LMSApplication(lMSTimeZoneResponse.timeZone, lMSTimeZoneResponse.impsExclude) };
                    }
                    pos.Position_Unit.leaf = pos.Position_Unit_Id;
                }

                if (pos.Location == "Finland" || pos.Location == "Germany" || pos.Location == "South Africa")                
                    pos.Base_Unit = pos.Department;

                pos.Position_Unit.Unit_End = MapDate(sapPosition.Sap_Position_Unit.Unit_End);
                positions.Add(pos);
            }

            if (responseRoot.HasError)
                return responseRoot;

            person.Positions = positions.ToArray();
            responseRoot.Person = person;
            return responseRoot;
        }

        private static Position ConstructPositionObject(SapEmployee sapEmployee, Sap_Position sapPosition)
        {
            return new Position()
            {
                Position_Title = sapPosition.Sap_Position_Title,
                Main_Position = int.Parse(sapPosition.Sap_Main_Position),
                Faculty = sapPosition.Sap_Faculty,
                School = sapPosition.Sap_School,
                Department = sapPosition.Sap_Department,
                Base_Unit = sapPosition.Sap_Base_Unit,
                Position_Reference = sapPosition.Sap_Position_Reference,
                Occupancy_Reference = sapPosition.Sap_Occupancy_Reference,
                Occupancy_Id = CalcOccId(sapPosition.Sap_Occupancy_Reference),
                Pos_Occ_Start = sapPosition.Sap_Pos_Occ_Start,
                Pos_Occ_End = MapDate(sapPosition.Sap_Pos_Occ_End),
                Pos_Occ_Exp_End = MapDate(sapPosition.Sap_Pos_Occ_End),
                Position_Status = sapPosition.Sap_Position_Status,
                Position_Unit_Id = sapPosition.Sap_Position_Unit_Id,
                Position_Unit = sapPosition.Sap_Position_Unit,
                Manager_Ref_No = RemoveStaffNoPrefix(sapPosition.Sap_Manager_Ref_No),
                Position_Type = sapEmployee.Sap_Staff_Type
            };
        }

        private static string RemoveStaffNoPrefix(string staffNo)
        {
            if (string.IsNullOrEmpty(staffNo)) 
                return null;

            if (staffNo.Length < 8)
                return staffNo;

            return staffNo.Substring(2);
        }

        private static string CalcOccId(string occRef)
        {
            if (string.IsNullOrEmpty(occRef))
                return null;
            else 
                return occRef + "A";
        }

        private static string CalcInitials(string fn1, string fn2)
        {
            string initials = string.Empty;
            if (!string.IsNullOrEmpty(fn1)) 
                initials += fn1.Substring(0, 1); 

            if (!string.IsNullOrEmpty(fn2)) 
                initials += fn2.Substring(0, 1); 

            return initials;
        }

        private static string CalcUsername(string upn)
        {
            if (!string.IsNullOrEmpty(upn) && upn.Contains("@"))            
                return upn.Substring(0, upn.IndexOf("@"));
            
            return null;
        }

        private static string MapDate(string endDate)
        {
            if (string.IsNullOrEmpty(endDate) || (endDate == "9999-12-31")) 
                return null; 

            return endDate;
        }
    }
}
