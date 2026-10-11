

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using FunctionsV2.LongRunningFramework;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UoR.Sys.Staff.OrgData.Models;

namespace UoR.AIS.HelperFunctionsV2.Activity
{
    public static class ProcessOrgDataStructure
    {
        [FunctionName("A_ProcessOrgDataStructureService")]
        public static async Task<ActivityResult> ProcessOrgDataStructureService([ActivityTrigger] IDurableActivityContext executionContext, ILogger log)
        {

            log.LogInformation("Activity function A_ProcessOrgDataStructureService execution started");

            string tableName = "uksaisorgdata";
            string connectionString = System.Environment.GetEnvironmentVariable("OrgDataCS");

            var taskInput = executionContext.GetInput<string>();

            if (string.IsNullOrEmpty(connectionString))
            {
                log.LogCritical("OrgDataCS app config is empty");
                throw new System.Exception("OrgDataCS app config is empty");
            }

            // Retrieve the storage account from the connection string.
            var tableClient = new TableClient(connectionString, tableName);
            OrgDataRequest request = null;

            // 1. Deserialiaze the request
            if (!string.IsNullOrEmpty(taskInput))
            {
                request = JsonConvert.DeserializeObject<OrgDataRequest>(taskInput);
            }


            // 2. Retrieve the before values for the unit and log them
            UnitDetails beforeValues = request.OrgData.Before;
            if (beforeValues != null)
            {
                log.LogInformation("End date: " + beforeValues.EndDate.ToString());
                log.LogInformation("Parent Id: " + beforeValues.ParentId.ToString());
                log.LogInformation("Start date: " + beforeValues.StartDate.ToString());
                log.LogInformation("Unit name: " + beforeValues.UnitName.ToString());
                log.LogInformation("Unit status: " + beforeValues.UnitStatus.ToString());
            }

            UnitDetails afterValues = request.OrgData.After;
            log.LogInformation("End date: " + afterValues.EndDate.ToString());
            log.LogInformation("Parent Id: " + afterValues.ParentId.ToString());
            log.LogInformation("Start date: " + afterValues.StartDate.ToString());
            log.LogInformation("Unit name: " + afterValues.UnitName.ToString());
            log.LogInformation("Unit status: " + afterValues.UnitStatus.ToString());



            OrgDataResponse response = new OrgDataResponse();

            ActivityResult activityResult = new ActivityResult();

            // 3. Get the list of business units, divisions and departments
            var queryDivisions = tableClient.
                QueryAsync<OrgDataDump>(ent => ent.PartitionKey == "Division");

            var queryDepartments = tableClient.
                QueryAsync<OrgDataDump>(ent => ent.PartitionKey == "Department");

            OrgDataResult beforeOrgdataResult = null;
            if (beforeValues.ParentId != string.Empty)
            {
                beforeOrgdataResult = await ContructOrgStructure(request, queryDivisions, tableClient, beforeValues, log);
            }
            OrgDataResult afterOrgdataResult = await ContructOrgStructure(request, queryDivisions, tableClient, afterValues, log);

            /*if (afterOrgdataResult != null)
            {
                afterOrgdataResult.Approver = GetApprover(request.OrgData.UnitId, request.Approvers);
            }*/

            response.Before = beforeOrgdataResult;
            response.After = afterOrgdataResult;

            activityResult.Status = "Success";
            activityResult.Result = response;

            if (afterValues.UnitStatus == "A")
            {
                afterOrgdataResult.Change = true;
            }
            else
            {
                // In-active status received. This doesn't needs to get processed. 
                log.LogInformation("In active status received. This doesn't needs to get processed");

                afterOrgdataResult.Change = false;
                activityResult.Status = "Success";
                activityResult.Result = response;

            }


            return activityResult;

        }

        private static async Task<OrgDataResult> ContructOrgStructure(OrgDataRequest request, Azure.AsyncPageable<OrgDataDump> queryDivisions,
            TableClient tableClient, UnitDetails unitDetails, ILogger log)
        {
            OrgDataResult result = new OrgDataResult();

            if (request.OrgData.UnitType == "business unit")
            {
                result.Change = false;
            }
            else
            {
                result.Change = true;
                if (request.OrgData.UnitType == "division")
                {
                    await foreach (OrgDataDump dump in queryDivisions)
                    {
                        if (dump.RefId == request.OrgData.UnitId)
                        {
                            result.Faculty = unitDetails.UnitName;
                            result.FacultyId = request.OrgData.UnitId;

                            OrgDataDump division = await GetDepartmentOrDivision(request.OrgData.UnitId, tableClient);
                            result.PartitionKey = division.PartitionKey;
                            result.RowKey = division.RowKey;

                            break;
                        }
                    }

                    if(result.Faculty == null)
                    {
                        result.PartitionKey = "Division";
                        result.RowKey = System.Guid.NewGuid().ToString();
                        result.ChangedUnitId = request.OrgData.UnitId;
                        result.ParentId = unitDetails.ParentId;
                        result.ChangedUnitName = request.OrgData.After.UnitName;
                        result.Faculty = unitDetails.UnitName;
                        result.FacultyId = request.OrgData.UnitId;
                    }
                }
                else
                {
                    int level = 0;
                    string parentId = unitDetails.ParentId;
                    List<OrgDataDump> departments = new List<OrgDataDump>();
                    OrgDataDump dept = await GetDepartmentOrDivision(request.OrgData.UnitId, tableClient);

                    if (dept == null)
                    {
                        dept = new OrgDataDump()
                        {
                            Name = unitDetails.UnitName,
                            ParentId = unitDetails.ParentId,
                            RefId = request.OrgData.UnitId
                        };
                        result.PartitionKey = "Department";
                        result.RowKey = System.Guid.NewGuid().ToString();
                        result.ChangedUnitId = request.OrgData.UnitId;
                        result.ParentId = unitDetails.ParentId;
                        result.ChangedUnitName = request.OrgData.After.UnitName;

                    }
                    else
                    {
                        log.LogInformation("Department name: " + dept.Name);
                        log.LogInformation("Department id: " + dept.RefId);
                        dept.Name = unitDetails.UnitName;

                        result.PartitionKey = dept.PartitionKey;
                        result.RowKey = dept.RowKey;
                        result.ChangedUnitId = request.OrgData.UnitId;
                        result.ParentId = unitDetails.ParentId;
                        result.ChangedUnitName = request.OrgData.After.UnitName;
                    }

                    departments.Add(dept);
                    level++;
                    while (parentId != null)
                    {
                        OrgDataDump obj = await GetDepartmentOrDivision(parentId, tableClient);
                        

                        if (obj == null)
                            break;

                        if (obj.PartitionKey == "BusinessUnit")
                            break;
                        log.LogInformation("Department name: " + obj.Name);
                        log.LogInformation("Department id: " + obj.RefId);

                        departments.Add(obj);
                        level++;
                        parentId = obj.ParentId;
                    }


                    if (departments.Count == 1)
                    {
                        result.Faculty = departments[0].Name;
                        result.FacultyId = departments[0].RefId;
                    }
                    else if (departments.Count == 2)
                    {
                        result.School = departments[0].Name;
                        result.SchoolId = departments[0].RefId;
                        result.Faculty = departments[1].Name;
                        result.FacultyId = departments[1].RefId;
                    }
                    else if (departments.Count == 3)
                    {
                        result.Dept = departments[0].Name;
                        result.DeptId = departments[0].RefId;
                        result.School = departments[1].Name;
                        result.SchoolId = departments[1].RefId;
                        result.Faculty = departments[2].Name;
                        result.FacultyId = departments[2].RefId;
                    }
                    else if (departments.Count == 4)
                    {
                        result.Unit = departments[0].Name;
                        result.UnitId = departments[0].RefId;
                        result.Dept = departments[1].Name;
                        result.DeptId = departments[1].RefId;
                        result.School = departments[2].Name;
                        result.SchoolId = departments[2].RefId;
                        result.Faculty = departments[3].Name;
                        result.FacultyId = departments[3].RefId;
                    }
                    else if (departments.Count == 5)
                    {
                        result.SubUnit = departments[0].Name;
                        result.SubUnitId = departments[0].RefId;
                        result.Unit = departments[1].Name;
                        result.UnitId = departments[1].RefId;
                        result.Dept = departments[2].Name;
                        result.DeptId = departments[2].RefId;
                        result.School = departments[3].Name;
                        result.SchoolId = departments[3].RefId;
                        result.Faculty = departments[4].Name;
                        result.FacultyId = departments[4].RefId;
                    }
                    else if (departments.Count > 5)
                    {
                        int actualCount = departments.Count - 5;

                        result.SubUnit = departments[actualCount].Name;
                        result.SubUnitId = departments[actualCount++].RefId;

                        result.Unit = departments[actualCount].Name;
                        result.UnitId = departments[actualCount++].RefId;

                        result.Dept = departments[actualCount].Name;
                        result.DeptId = departments[actualCount++].RefId;

                        result.School = departments[actualCount].Name;
                        result.SchoolId = departments[actualCount++].RefId;

                        result.Faculty = departments[actualCount].Name;
                        result.FacultyId = departments[actualCount].RefId;
                    }
                    else
                    {
                        throw new System.Exception("Invalid count received");
                    }

                    if (result.FacultyId == request.OrgData.UnitId || result.SchoolId == request.OrgData.UnitId || result.DeptId == request.OrgData.UnitId
                    || result.UnitId == request.OrgData.UnitId || result.SubUnitId == request.OrgData.UnitId)
                    {
                        // The changed unit is within the first five levels. 
                    }
                    else
                    {
                        result = null;
                    }
                }
            }
            return result;
        }
        private static async Task<OrgDataDump> GetDepartmentOrDivision(string refId, TableClient tableClient)
        {
            var queryDepartments = tableClient.
                QueryAsync<OrgDataDump>(ent => ent.RefId == refId);

            OrgDataDump obj = null;
            await foreach (OrgDataDump dept in queryDepartments)
            {
                obj = dept;
            }
            return obj;
        }
        /*private static Approvers GetApprover(string unit, List<Approver> approversList)
        {
            Approvers approvers = new Approvers();

            var facultyObj = approversList.Where(x => x.UnitId == unit && x.Level == 1);
            if (facultyObj.Count() > 1)
            {
                approvers.FacultyStaffNo = facultyObj.First().StaffId;
                approvers.FacultyUserName = facultyObj.First().UserName;
            }

            var schoolObj = approversList.Where(x => x.UnitId == unit && x.Level == 2);
            if (schoolObj.Count() > 1)
            {
                approvers.SchoolStaffNo = schoolObj.First().StaffId;
                approvers.SchoolUserName = schoolObj.First().UserName;
            }

            var deptObj = approversList.Where(x => x.UnitId == unit && x.Level == 3);
            if (deptObj.Count() > 1)
            {
                approvers.DeptStaffNo = deptObj.First().StaffId;
                approvers.DeptUserName = deptObj.First().UserName;
            }
            return approvers;
        }*/
    }
}