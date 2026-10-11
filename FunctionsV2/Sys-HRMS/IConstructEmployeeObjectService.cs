using FunctionsV2.Helpers;
using FunctionsV2.Sys_HRMS.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using PositionError = FunctionsV2.Sys_HRMS.Models.EmployeeValidationError;
using FunctionsV2.Helpers.Models;


namespace FunctionsV2.Sys_HRMS.Services.Interfaces
{
    //Interface
    public interface IConstructEmployeeObjectService
    {
        public BaseFunctionResponse ConstructEmployeeObject(ConstrictEmployeeObjectRequest constructEmployeeObjectRequest);
    }

    //Service
    public class ConstructEmployeeObjectService : IConstructEmployeeObjectService
    {
        private readonly ITableStorageService _tableStorageService;
        private readonly IConfiguration _config;

        //Field to check / Error message when null
        private Dictionary<string, string> positionErrorCodes = new()
        {
            { "Sap_Main_Position", "Error Code 1: Main Position missing" },
            { "Sap_Occupancy_Reference", "Error Code 1: Occupancy Reference missing" },
            { "Sap_Base_Unit", "Error Code 2: Base unit missing" },
            { "Sap_Position_Title", "Error Code 3: Position title missing" },
            { "Sap_Position_Status", "Error Code 2: Position status missing" },
            { "Sap_Pos_Occ_Start", "Error Code 3: Position start date missing" }
        };

        //Field to check / Error message when null
        private Dictionary<string, string> personErrorCodes = new()
        {
            { "Staff_Type", "Error Code 1: Staff Type missing" },
            { "Personal_Reference", "Error Code 1: Personal Reference missing" }
        };

        public ConstructEmployeeObjectService(ITableStorageService tableStorageService,
            IConfiguration config)
        {
            _tableStorageService = tableStorageService;
            _config = config;
        }

        public BaseFunctionResponse ConstructEmployeeObject(ConstrictEmployeeObjectRequest constructEmployeeObjectRequest)
        {
            try
            {
                ConstructEmployeeObjectResponse employeeObjectResponse = new();

                employeeObjectResponse = ValidateEmployeeData(constructEmployeeObjectRequest.Employee);
                if (employeeObjectResponse.ErrorList.Count > 0)
                {
                    return new BaseFunctionResponse
                    {
                        Response = new
                        {
                            Result = employeeObjectResponse
                        },
                        Error = ConstructErrorResponse(employeeObjectResponse.ErrorList, "Employee")
                    };
                }

                employeeObjectResponse = EnrichEmployeeWithPositionData(constructEmployeeObjectRequest);
                if (employeeObjectResponse.ErrorList.Count > 0)
                {
                    return new BaseFunctionResponse
                    {
                        Response = employeeObjectResponse,
                        Error = ConstructErrorResponse(employeeObjectResponse.ErrorList, "Position")
                    };
                }

                return new BaseFunctionResponse
                {
                    Response = new {
                        Result = employeeObjectResponse.Employee
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

        private BaseFunctionResponseError ConstructErrorResponse(List<PositionError> ErrorList, string dataType)
        {
            return new BaseFunctionResponseError
            {
                Code = $"{((int)ErrorCodes.BadRequest)}",
                Message = $"Validation failed when checking '{dataType}' data. " +
                                $"{ErrorList.Count} error(s) have been returned: " +
                                $"{string.Join(", ", ErrorList
                                    .GroupBy(item => item)
                                    .Select(group => $"x{group.Count()} : '{group.Key}'"))}"
            };
        }

        private ConstructEmployeeObjectResponse ValidateEmployeeData(Employee employeeObject)
        {
            List<string> missingData = new();
            ConstructEmployeeObjectResponse missingDataResponse = new();

            if (employeeObject == null)
                missingData.Add("Missing person data");

            else
            {
                missingData.AddRange(personErrorCodes
                   .Where(errorCode => string.IsNullOrWhiteSpace(employeeObject.GetType().GetProperty(errorCode.Key)?.GetValue(employeeObject)?.ToString()))
                   .Select(errorCode => errorCode.Value)
                   .ToList());

                missingDataResponse.ErrorList.Add(new PositionError(null, $"{string.Join(", ", missingData.ToArray<string>())}"));
            }
            if (missingData.Count > 0)
                return missingDataResponse;

            return new ConstructEmployeeObjectResponse
            {
                Employee = employeeObject,
            };
        }

        private ConstructEmployeeObjectResponse ValidatePositionData(Employee employeeObject)
        {
            ConstructEmployeeObjectResponse missingDataResponse = new();

            foreach (SapPosition position in employeeObject.Positions)
            {
                List<string> missingPoData = positionErrorCodes
                    .Where(errorCode => string.IsNullOrWhiteSpace(position.GetType().GetProperty(errorCode.Key)?.GetValue(position)?.ToString()))
                    .Select(errorCode => errorCode.Value)
                    .ToList();

                foreach (string missingData in missingPoData)
                {
                    missingDataResponse.ErrorList.Add(new PositionError(position.Sap_Position_Reference, missingData));
                }
            }

            if (missingDataResponse.ErrorList.Count > 0)
                return missingDataResponse;

            return new ConstructEmployeeObjectResponse
            {
                Employee = employeeObject,
            };
        }

        private ConstructEmployeeObjectResponse EnrichEmployeeWithPositionData(ConstrictEmployeeObjectRequest constructEmployeeObjectRequest)
        {
            foreach (SapPosition position in constructEmployeeObjectRequest.Employee.Positions)
            {
                bool isPosTitleVC = (position.Sap_Position_Title == _config["vcJobTitle"]) ? true : false;

                ConstructEmployeeObjectResponse validatedData = ValidatePositionData(constructEmployeeObjectRequest.Employee);
                if (validatedData.ErrorList.Count > 0)
                    return validatedData;

                OrgLevelObject level2Details = JsonConvert.DeserializeObject<OrgLevelObject>(_config["orgLevel2Roles"]);
                Dictionary<string, string> facultyNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(_config["orgLevel3Roles"]);

                switch ((position.Sap_Employee_Group, position.Sap_Employee_Subgroup, isPosTitleVC))
                {
                    //Vice Chancellor
                    case ("B", "11", true):
                        position.Sap_Faculty = facultyNames.GetValueOrDefault("vcFaculty");
                        position.Sap_Base_Unit = facultyNames.GetValueOrDefault("vcFaculty");
                        position.Sap_Position_Unit = ConstructPositionUnit(position, level2Details.UoRStandard.Id, level2Details.UoRStandard.Name).Result;

                        if (position.Sap_Position_Unit.Sap_Unit_Nm_04 != null)
                            position.Sap_School = position.Sap_Position_Unit.Sap_Unit_Nm_04;

                        if (position.Sap_Position_Unit.Sap_Unit_Nm_05 != null)
                            position.Sap_Department = position.Sap_Position_Unit.Sap_Unit_Nm_05;

                        break;

                    //Governor 
                    case ("H", "70", false):
                        if (constructEmployeeObjectRequest.Employee.Personal_Reference != constructEmployeeObjectRequest.CouncilPresident)
                        {
                            ConstructEmployeeObjectResponse response = new();

                            response.ErrorList.Add(new PositionError(constructEmployeeObjectRequest.Employee.Personal_Reference, "Error Code 4: Non council president governor"));
                            return response;
                        }
                        position.Sap_Faculty = facultyNames.GetValueOrDefault("govFaculty");
                        position.Sap_Base_Unit = facultyNames.GetValueOrDefault("govFaculty");
                        position.Sap_Position_Unit = ConstructPositionUnit(position, level2Details.UoRStandard.Id, level2Details.UoRStandard.Name).Result;
                        break;

                    //SUMS
                    case ("I", "82", false):
                        position.Sap_Position_Unit = ConstructPositionUnit(position, level2Details.SUMS.Id, level2Details.SUMS.Name).Result;
                        break;

                    default:
                        break;
                }
                position.Sap_Position_Unit_Id = SetPositionUnitId(position);
            }
            return new ConstructEmployeeObjectResponse()
            {
                Employee = constructEmployeeObjectRequest.Employee,
            };
        }

        private async Task<SapPositionUnit> ConstructPositionUnit(SapPosition position, string unitId02, string unitNm02)
        {
            SapPositionUnit positionUnit = new()
            {
                Sap_Unit_Nm_01 = "University of Reading",
                Sap_Unit_Id_01 = "1000",
                Sap_Unit_Nm_02 = unitNm02,
                Sap_Unit_Id_02 = unitId02,
                Sap_Unit_Nm_03 = position.Sap_Faculty,
                Sap_Unit_Id_03 = position.Sap_Position_Unit?.Sap_Unit_Id_03 ?? null,
                Sap_Unit_Nm_04 = position.Sap_Position_Unit?.Sap_Unit_Nm_04 ?? null,
                Sap_Unit_Id_04 = position.Sap_Position_Unit?.Sap_Unit_Id_04 ?? null,
                Sap_Unit_Nm_05 = position.Sap_Position_Unit?.Sap_Unit_Nm_05 ?? null,
                Sap_Unit_Id_05 = position.Sap_Position_Unit?.Sap_Unit_Id_05 ?? null,
                Sap_Unit_Base = position.Sap_Base_Unit,
                Sap_Unit_Status = "C",
                Sap_Unit_Start = "1900-01-01",
                Sap_Unit_End = "9999-12-31"
            };

            //Faculty
            //Double apostrophe added to Faculty to cater for TS query, to be overwritten later with TS results. 
            OrgTableStorageResponse faculty = await GetTableStorageData("Name", position.Sap_Faculty.Replace("'", "''"));
            positionUnit.Sap_Unit_Nm_03 = faculty?.Name;
            positionUnit.Sap_Unit_Id_03 = faculty?.RefId;

            if (faculty?.RefId != null && positionUnit.Sap_Unit_Nm_04 == null)
            {
                //School
                OrgTableStorageResponse school = await GetTableStorageData("ParentId", faculty.RefId);
                positionUnit.Sap_Unit_Nm_04 = school?.Name;
                positionUnit.Sap_Unit_Id_04 = school?.RefId;

                if (school?.ParentId != null && positionUnit.Sap_Unit_Nm_05 == null)
                {
                    //Department
                    OrgTableStorageResponse department = await GetTableStorageData("ParentId", school.RefId);
                    positionUnit.Sap_Unit_Nm_05 = department?.Name;
                    positionUnit.Sap_Unit_Id_05 = department?.RefId;
                }
            }
            return positionUnit;
        }

        private async Task<OrgTableStorageResponse> GetTableStorageData(string fieldName, string fieldValue)
        {
            List<OrgTableStorageResponse> orgTableStorageResponse = await _tableStorageService.QueryTableStorageByFilter<OrgTableStorageResponse>(_config["orgDataTableName"], $"{fieldName} eq '{fieldValue}'");
            return orgTableStorageResponse.FirstOrDefault();
        }

        private string SetPositionUnitId(SapPosition position)
        {
            string[] unitNames = [ position.Sap_Position_Unit.Sap_Unit_Nm_05, position.Sap_Position_Unit.Sap_Unit_Nm_04, position.Sap_Position_Unit.Sap_Unit_Nm_03,
                position.Sap_Position_Unit.Sap_Unit_Nm_02, position.Sap_Position_Unit.Sap_Unit_Nm_01 ];
            return unitNames.FirstOrDefault(unitName => unitName != null);
        }
    }
}
