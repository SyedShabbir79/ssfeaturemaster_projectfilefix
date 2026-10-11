using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using RestSharp;
using RestSharp.Authenticators;
using System.Security.Cryptography;
using FunctionsV2.Sys_LMS.Models;
using Microsoft.Extensions.Configuration;
using FunctionsV2.Sys_Staff_LMS;
using System.Diagnostics;
using System.Web;
using FunctionsV2.Helpers;
using RestSharp.Serializers.NewtonsoftJson;

namespace FunctionsV2.Sys_LMS
{
    public class Sys_Staff_UoRLearn
    {
        private readonly ICache _cache;
        public Sys_Staff_UoRLearn(ICache _cache)
        {
            this._cache = _cache;
        }


        public class lmsWorker
        {




            private class _endpoints
            {
                public String login { get { return "/v1/login"; } }
                public String organisation { get { return "/v1/organization"; } }
                public String people { get { return "/v1/people"; } }
            }


            _endpoints endpoints = new _endpoints();




            private upsertRequestUoRLearn request;
            private RestClient sabaCloud;
            private readonly ICache _cache;
            private readonly string sabaCloudBaseUrl;

            public lmsWorker(upsertRequestUoRLearn request, ICache _cache)
            {
                this.request = request;
                sabaCloud = new RestClient(request.lmsURL, configureSerialization: s => s.UseNewtonsoftJson());
                sabaCloudBaseUrl = request.lmsURL;
                //sabaCloud.AddHandler("application/json", () => RestsharpJsonSerializer.Default);
                //sabaCloud.UseNewtonsoftJson(RestsharpJsonSerializer.Default);
                this._cache = _cache;
            }








            public async Task<IActionResult> upsertEmployee(upsertRequestUoRLearn request, ExecutionContext context)
            {
                parseEmployee pe = new parseEmployee(request.person);

                Employee employee = pe.employee;
                UoR.Sys.LMS.UoRLearn.Models.Person existing = await getExistingPerson(employee, context);


                if (pe.employee.currentPositions.Count() > 0 || (existing != null && existing.status == "Active"))
                {
                    if (pe.isValid || (existing != null && existing.status == "Active"))
                    {


                        if (existing != null || (employee.staff_type == "X" || employee.staff_type == "E"))
                        {
                            DateTime _IIQUpdatesFrom;
                            if (existing != null && DateTime.TryParse(existing.customValues.custom3, out _IIQUpdatesFrom))
                            {
                                if (_IIQUpdatesFrom > DateTime.Now.Date)
                                {
                                    return new NoContentResult();
                                }
                            }

                            if (existing != null)
                            {
                                existing = await updateEmployee(existing, employee, context);
                            }
                            else if (employee.staff_type == "X" || employee.staff_type == "E")
                            {
                                existing = await createEmployee(employee, context);
                            }

                            if (existing != null && existing.status == "Active" && employee.staff_type != "L")
                            {
                                await updateTeamInfo(employee, existing, context);
                                await updateAdditionalPositions(employee, existing, context);
                            }

                            return new OkObjectResult(existing);
                        }
                        return new NoContentResult();
                    }
                    return new BadRequestResult();
                }
                else
                {
                    return new NoContentResult();
                }
            }

            private async Task updateTeamInfo(Employee employee, UoR.Sys.LMS.UoRLearn.Models.Person existing, ExecutionContext context)
            {
                RestRequest getRequest = new RestRequest($"{endpoints.people}/{existing.id}/teamInfo/{existing.id}", Method.Get);
                getRequest.AddHeader("SabaCertificate", await getLMSToken(context));

                //IRestResponse<UoR.Sys.LMS.UoRLearn.Models.TeamInfo> getTeamInfoResponse = sabaCloud.Get<UoR.Sys.LMS.UoRLearn.Models.TeamInfo>(getRequest);
                RestResponse<UoR.Sys.LMS.UoRLearn.Models.TeamInfo> getTeamInfoResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.TeamInfo>(getRequest);

                if (getTeamInfoResponse.IsSuccessful)
                {
                    UoR.Sys.LMS.UoRLearn.Models.TeamInfo teamInfo = getTeamInfoResponse.Data;

                    string existingTeamInfoHash = CreateMD5(teamInfo);

                    List<UoR.Sys.LMS.UoRLearn.Models.LMSBase> altManagers = new List<UoR.Sys.LMS.UoRLearn.Models.LMSBase>();
                    List<string> nonPrimaryManagers = employee.nonPrimaryManagers;

                    foreach (string m in nonPrimaryManagers)
                    {
                        UoR.Sys.LMS.UoRLearn.Models.LMSBase managerPtr = await getPersonPtr(new UoR.Sys.LMS.UoRLearn.Models.Conditions
                        {
                            conditions = new[] {
                        new UoR.Sys.LMS.UoRLearn.Models.Condition { name = "person_no", value = m, _operator = "==" }
                    }
                        }, context);

                        if (managerPtr != null)
                        {
                            var dr = teamInfo.directReports.SingleOrDefault(x => x.id == managerPtr.id);
                            var am = altManagers.SingleOrDefault(m => m.id == managerPtr.id);
                            if (dr == null && am == null)
                            {
                                altManagers.Add(new UoR.Sys.LMS.UoRLearn.Models.LMSBase { id = managerPtr.id });
                            }
                        }
                    }
                    teamInfo.alternateManagers = altManagers;


                    string newTeamInfoHash = CreateMD5(teamInfo);

                    if (existingTeamInfoHash != newTeamInfoHash)
                    {
                        RestRequest putRequest = new RestRequest($"{endpoints.people}/{existing.id}/teamInfo/{existing.id}", Method.Put);
                        putRequest.AddHeader("SabaCertificate", await getLMSToken(context));
                        putRequest.AddJsonBody(teamInfo);

                        //IRestResponse putTeamInfoResponse = sabaCloud.Put(putRequest);
                        RestResponse putTeamInfoResponse = await sabaCloud.ExecuteAsync(putRequest, Method.Put);
                        if (putTeamInfoResponse.IsSuccessful)
                        {
                            return;
                        }
                        throw putTeamInfoResponse.ErrorException;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    throw getTeamInfoResponse.ErrorException;
                }
            }

            private async Task updateAdditionalPositions(Employee employee, UoR.Sys.LMS.UoRLearn.Models.Person existing, ExecutionContext context)
            {
                RestRequest getAPRequest = new RestRequest($"{endpoints.people}/{existing.id}/customSection1/{existing.id}", Method.Get);
                getAPRequest.AddHeader("SabaCertificate", await getLMSToken(context));
                //getAPRequest.JsonSerializer = RestsharpJsonSerializer.Default;
                //IRestResponse<UoR.Sys.LMS.UoRLearn.Models.additionalPositions> getAdditionalPositions = sabaCloud.Get<UoR.Sys.LMS.UoRLearn.Models.additionalPositions>(getAPRequest);
                RestResponse<UoR.Sys.LMS.UoRLearn.Models.additionalPositions> getAdditionalPositions = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.additionalPositions>(getAPRequest,Method.Get);

                if (getAdditionalPositions.IsSuccessful)
                {
                    UoR.Sys.LMS.UoRLearn.Models.additionalPositions existingAP = getAdditionalPositions.Data;

                    string existingAPHash = CreateMD5(existingAP);
                    existingAP = _setAdditionalPositions(existingAP, employee);
                    string updatedAPHash = CreateMD5(existingAP);

                    if (existingAPHash != updatedAPHash)
                    {
                        RestRequest putAPRequest = new RestRequest($"{endpoints.people}/{existing.id}/customSection1/{existingAP.id}", Method.Get);
                        putAPRequest.AddHeader("SabaCertificate", await getLMSToken(context));
                        //putAPRequest.JsonSerializer = RestsharpJsonSerializer.Default;
                        putAPRequest.AddJsonBody(existingAP);

                        RestResponse<UoR.Sys.LMS.UoRLearn.Models.additionalPositions> putAPResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.additionalPositions>(putAPRequest,Method.Put);
                        if (putAPResponse.IsSuccessful)
                        {
                            return;
                        }
                        throw putAPResponse.ErrorException;
                    }
                }
                else if (getAdditionalPositions.Content.Contains("3302"))
                {
                    UoR.Sys.LMS.UoRLearn.Models.additionalPositions newAP = new UoR.Sys.LMS.UoRLearn.Models.additionalPositions();
                    newAP = _setAdditionalPositions(newAP, employee);

                    RestRequest postAPRequest = new RestRequest($"{endpoints.people}/{existing.id}/customSection1", Method.Post);
                    postAPRequest.AddHeader("SabaCertificate", await getLMSToken(context));
                    //postAPRequest.JsonSerializer = RestsharpJsonSerializer.Default;
                    postAPRequest.AddJsonBody(newAP);

                    RestResponse<UoR.Sys.LMS.UoRLearn.Models.additionalPositions> postAPResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.additionalPositions>(postAPRequest,Method.Post);
                    if (postAPResponse.IsSuccessful)
                    {
                        return;
                    }
                    throw postAPResponse.ErrorException;

                }
                else
                {
                    throw getAdditionalPositions.ErrorException;
                }
                return;
            }

            private UoR.Sys.LMS.UoRLearn.Models.additionalPositions _setAdditionalPositions(UoR.Sys.LMS.UoRLearn.Models.additionalPositions existing, Employee employee)
            {
                existing.customValues = new UoR.Sys.LMS.UoRLearn.Models.additionalPositions.CustomValues();
                foreach (Employee.EmployeePosition p in employee.currentPositions.Where(p => p.main_position > 1 && p.main_position < 7))
                {
                    switch (p.main_position)
                    {
                        case 2:
                            existing.customValues.Job2Title = p.position_title;
                            existing.customValues.Job2Category = p.pay_category;
                            existing.customValues.Job2BaseUnit = p.position_unit.unit_base;
                            existing.customValues.Job2School = p.school;
                            break;

                        case 3:
                            existing.customValues.Job3Title = p.position_title;
                            existing.customValues.Job3Category = p.pay_category;
                            existing.customValues.Job3BaseUnit = p.position_unit.unit_base;
                            existing.customValues.Job3School = p.school;
                            break;

                        case 4:
                            existing.customValues.Job4Title = p.position_title;
                            existing.customValues.Job4Category = p.pay_category;
                            existing.customValues.Job4BaseUnit = p.position_unit.unit_base;
                            existing.customValues.Job4School = p.school;
                            break;

                        case 5:
                            existing.customValues.Job5Title = p.position_title;
                            existing.customValues.Job5Category = p.pay_category;
                            existing.customValues.Job5BaseUnit = p.position_unit.unit_base;
                            existing.customValues.Job5School = p.school;
                            break;

                        case 6:
                            existing.customValues.Job6Title = p.position_title;
                            existing.customValues.Job6Category = p.pay_category;
                            existing.customValues.Job6BaseUnit = p.position_unit.unit_base;
                            existing.customValues.Job6School = p.school;
                            break;
                    }
                }
                return existing;
            }



            private async Task<UoR.Sys.LMS.UoRLearn.Models.Person> createEmployee(Employee employee, ExecutionContext context)
            {
                UoR.Sys.LMS.UoRLearn.Models.Person newEmployee = _newPerson(employee);
                newEmployee = await setEmployee(newEmployee, employee, context);
                newEmployee = await _postPerson(newEmployee, context);
                return newEmployee;
            }


            private async Task<UoR.Sys.LMS.UoRLearn.Models.Person> setEmployee(UoR.Sys.LMS.UoRLearn.Models.Person existing, Employee employee, ExecutionContext context)
            {
                existing.username = employee.upn;
                existing.gender = 2;
                existing.person_type = "Staff";
                existing.person_no = employee.personal_reference;

                existing.securityDomain = new UoR.Sys.LMS.UoRLearn.Models.Securitydomain { displayName = "Internal" };
                existing.home_domain = new UoR.Sys.LMS.UoRLearn.Models.Home_Domain { displayName = "Internal" };


                if (employee.employee_end_date.HasValue)
                {
                    existing.terminated_on = employee.employee_end_date.Value.AddDays(1).AddHours(2);

                    if (existing.terminated_on < existing.started_on)
                    {
                        existing.terminated_on = existing.started_on;
                    }


                }
                else
                {
                    existing.terminated_on = null;
                }






                existing.status = "Active";

                existing.fname = employee.prefered_name;
                existing.lname = employee.known_as_surname;


                if (string.IsNullOrWhiteSpace(existing.lname))
                {
                    existing.lname = existing.fname;
                }



                existing.email = employee.email_address.ToLower();
                existing.customValues.custom1 = employee.username;



                if (employee.staff_type != "W")
                {
                    existing.job_title = employee.mainPosition.position_title;

                    if (employee.employee_end_date.HasValue && employee.employee_end_date.Value.Date < DateTime.Now.Date)
                    {
                        existing.status = "Inactive";
                    }

                    if (employee.mainPosition.faculty == "SUMS")
                    {
                        existing.status = "Inactive";
                        existing.started_on = employee.employee_start_date;
                        existing.terminated_on = employee.employee_start_date;
                    }

                    existing.customValues.custom0 = "No";
                    if (employee.applications.lms.impsexclude)
                    {
                        existing.customValues.custom0 = "Yes";
                    }



                    existing.customValues.custom2 = string.Join(",",
                        employee.positions
                        .Select(p => p.school ?? p.faculty)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct().OrderBy(s => s)
                        );





                    if (employee.mainPosition.pay_category != "Fees & Claims")
                    {
                        existing.jobtype_id = new UoR.Sys.LMS.UoRLearn.Models.JobType() { displayName = employee.mainPosition.pay_category };
                    }


                    existing.company_id = new UoR.Sys.LMS.UoRLearn.Models.Company_Id { displayName = "Root" };
                    existing.home_company_id = new UoR.Sys.LMS.UoRLearn.Models.Home_Company_Id { displayName = "Root" };

                    if (employee.mainPosition.position_unit != null && employee.mainPosition.position_unit.leaf != null)
                    {
                        UoR.Sys.LMS.UoRLearn.Models.LMSBase baseUnitPtr = await getOrganisationPtr(employee.mainPosition.position_unit.leaf, context);
                        if (baseUnitPtr != null)
                        {
                            existing.company_id = new UoR.Sys.LMS.UoRLearn.Models.Company_Id { id = baseUnitPtr.id, displayName = baseUnitPtr.name };
                        }


                        UoR.Sys.LMS.UoRLearn.Models.LMSBase schoolUnitPtr = await getOrganisationPtr(employee.mainPosition.position_unit.baseParentLeaf, context);
                        if (schoolUnitPtr != null)
                        {
                            existing.home_company_id = new UoR.Sys.LMS.UoRLearn.Models.Home_Company_Id { id = schoolUnitPtr.id, displayName = schoolUnitPtr.name };
                        }
                    }


                    existing.timezone_id = new UoR.Sys.LMS.UoRLearn.Models.Timezone_Id() { id = employee.applications.lms.timezone };

                    existing.manager_id = null;
                    if (!String.IsNullOrEmpty(employee.mainPosition.manager_ref_no))
                    {
                        UoR.Sys.LMS.UoRLearn.Models.Person primaryManager = await getExistingPerson(new Employee() { personal_reference = employee.mainPosition.manager_ref_no }, context);
                        if (primaryManager != null && primaryManager.id != existing.id)
                        {
                            if (primaryManager.manager_id == null || primaryManager.manager_id.id != existing.id)
                            {
                                existing.manager_id = new UoR.Sys.LMS.UoRLearn.Models.Manager_Id { id = primaryManager.id, displayName = $"{primaryManager.fname} {primaryManager.lname}" };
                            }
                        }
                    }



                }
                else
                {
                    existing.status = "Inactive";
                    existing.terminated_on = DateTime.Now.Date;
                }

                existing.associations = new UoR.Sys.LMS.UoRLearn.Models.Association()
                {
                    resource = new UoR.Sys.LMS.UoRLearn.Models.Association.Resource()
                    {
                        markResource = true,
                        rate = new UoR.Sys.LMS.UoRLearn.Models.Association.Resource.RateClass[] {
                        new UoR.Sys.LMS.UoRLearn.Models.Association.Resource.RateClass {
                            amount = "0.00",
                            currency = new UoR.Sys.LMS.UoRLearn.Models.Currency_Id {
                                id = "crncy000000000000156"
                            }
                        }
                    }
                    }
                };

                return existing;
            }


            private async Task<UoR.Sys.LMS.UoRLearn.Models.Person> updateEmployee(UoR.Sys.LMS.UoRLearn.Models.Person existing, Employee employee, ExecutionContext context)
            {
                UoR.Sys.LMS.UoRLearn.Models.Person person = await setEmployee(existing, employee, context);
                return await _putPerson(person, context);
            }



            public async Task<IActionResult> upsertExternal(upsertRequestUoRLearn request, ExecutionContext context)
            {
                parseExternal pe = new parseExternal(request.person);
                if (pe.isValid)
                {
                    External external = pe.external;
                    UoR.Sys.LMS.UoRLearn.Models.Person existing = await getExistingPerson(external.ToEmployee, context);

                    if (existing != null || external.userAccountStatus == "Live")
                    {
                        DateTime _IIQUpdatesFrom;
                        if (existing != null && DateTime.TryParse(existing.customValues.custom3, out _IIQUpdatesFrom))
                        {
                            if (_IIQUpdatesFrom > DateTime.Now.Date)
                            {
                                return new NoContentResult();
                            }
                        }

                        if (existing != null)
                        {
                            existing = await setExternal(existing, external, context);
                            existing = await _putPerson(existing, context);
                        }
                        else
                        {
                            existing = await createExternal(external, context);
                        }

                        return new OkObjectResult(existing);
                    }
                    return new NoContentResult();
                }
                return new BadRequestResult();
            }



            private async Task<UoR.Sys.LMS.UoRLearn.Models.Person> createExternal(External external, ExecutionContext context)
            {
                UoR.Sys.LMS.UoRLearn.Models.Person newExternal = _newPerson(external.ToEmployee);

                newExternal.started_on = external.uniStartDate;
                newExternal.terminated_on = newExternal.started_on;

                newExternal = await setExternal(newExternal, external, context);
                newExternal = await _postPerson(newExternal, context);
                return newExternal;
            }



            private async Task<UoR.Sys.LMS.UoRLearn.Models.Person> setExternal(UoR.Sys.LMS.UoRLearn.Models.Person existing, External external, ExecutionContext context)
            {
                existing.username = external.upn;
                existing.gender = 2;
                existing.person_type = "External";
                existing.password = $"{generateRandomString(12)}_aZ0?";

                existing.fname = external.preferredName;
                existing.lname = external.knownAsSurname;
                existing.job_title = external.positionTitle;

                existing.email = external.emailAddress.ToLower();

                existing.customValues.custom0 = "No";
                existing.customValues.custom1 = external.username;
                existing.customValues.custom2 = external.school ?? external.faculty;

                existing.securityDomain = new UoR.Sys.LMS.UoRLearn.Models.Securitydomain { displayName = "External" };
                existing.home_domain = new UoR.Sys.LMS.UoRLearn.Models.Home_Domain { displayName = "External" };

                if (external.applications.lms.corpAccess.HasValue && external.applications.lms.corpAccess.Value)
                {
                    existing.person_type = "External Corporate Access";
                    DateTime calcTerminated = external.uniStartDate.AddMonths(1).Date;
                    if (external.uniLeaveDate.HasValue && calcTerminated > external.uniLeaveDate)
                    {
                        calcTerminated = external.uniLeaveDate.Value;
                    }
                    existing.terminated_on = calcTerminated;


                    if (existing.terminated_on < existing.started_on)
                    {
                        existing.terminated_on = existing.started_on;
                    }

                    existing.terminated_on = existing.terminated_on.Value.AddHours(4);


                    if (existing.terminated_on.Value.Date >= DateTime.Now.Date)
                    {
                        existing.status = "Active";
                    }
                    else
                    {
                        existing.status = "Inactive";
                    }
                }
                else
                {
                    existing.terminated_on = existing.started_on;
                    existing.status = "Inactive";
                }

                existing.company_id = new UoR.Sys.LMS.UoRLearn.Models.Company_Id { displayName = "Root" };
                existing.home_company_id = new UoR.Sys.LMS.UoRLearn.Models.Home_Company_Id { displayName = "Root" };
                UoR.Sys.LMS.UoRLearn.Models.LMSBase baseUnit = await getOrganisationPtrByName(external.department ?? external.school, context);
                if (baseUnit != null)
                {
                    existing.company_id = new UoR.Sys.LMS.UoRLearn.Models.Company_Id { displayName = baseUnit.name, id = baseUnit.id };
                    existing.home_company_id = new UoR.Sys.LMS.UoRLearn.Models.Home_Company_Id { displayName = baseUnit.name, id = baseUnit.id };
                }
                existing.timezone_id = new UoR.Sys.LMS.UoRLearn.Models.Timezone_Id { id = "tzone000000000000022" };


                if (!string.IsNullOrEmpty(external.managerRefNo))
                {
                    UoR.Sys.LMS.UoRLearn.Models.LMSBase primaryManager = await getPersonPtr(new UoR.Sys.LMS.UoRLearn.Models.Conditions
                    {
                        conditions = new[] {
                        new UoR.Sys.LMS.UoRLearn.Models.Condition { name = "person_no", value = external.managerRefNo, _operator = "==" }
                    }
                    }, context);
                    if (primaryManager != null)
                    {
                        existing.manager_id = new UoR.Sys.LMS.UoRLearn.Models.Manager_Id { id = primaryManager.id, displayName = primaryManager.name };
                    }
                }

                existing.associations = new UoR.Sys.LMS.UoRLearn.Models.Association
                {
                    resource = new UoR.Sys.LMS.UoRLearn.Models.Association.Resource
                    {
                        markResource = true,
                        rate = new[] {
                        new UoR.Sys.LMS.UoRLearn.Models.Association.Resource.RateClass {
                            amount = "0.00",
                            currency = new UoR.Sys.LMS.UoRLearn.Models.Currency_Id { id = "crncy000000000000156" }
                        }
                    }
                    }
                };
                return existing;
            }










            private string generateRandomString(int length)
            {
                using (RNGCryptoServiceProvider cryptRNG = new RNGCryptoServiceProvider())
                {
                    byte[] tokenBuffer = new byte[length];
                    cryptRNG.GetBytes(tokenBuffer);
                    return Convert.ToBase64String(tokenBuffer);
                }
            }

            public class tokenResponse
            {
                public string type { get; set; }
                public string certificate { get; set; }
            }

            private async Task<string> getLMSToken(ExecutionContext context)
            {


                string tokenKey = $"UoR_LMS_SessionToken_{sabaCloudBaseUrl.Split(".").First().Replace("https://","")}";
                //string tokenKey = $"UoR_LMS_SessionToken_Change";



                ICache.RedisRead sessionToken = await _cache.CacheRead(new ICache.RedisRead() { key = tokenKey });
                if (sessionToken.hasValue)
                {
                    return sessionToken.value.ToObject<string>();
                }

                var getRequest = new RestRequest(endpoints.login, Method.Get);
                getRequest.AddHeader("user", request.lmsUsername);
                getRequest.AddHeader("password", request.lmsPassword);

                RestResponse<tokenResponse> tokenResponse = await sabaCloud.ExecuteAsync<tokenResponse>(getRequest,Method.Get);
                if (tokenResponse.IsSuccessful)
                {
                    await _cache.CacheWrite(new ICache.RedisWrite() { key = tokenKey, value = tokenResponse.Data.certificate, expiry = 60 * 30 });
                    return tokenResponse.Data.certificate;
                }
                else
                {
                    throw new Exception(tokenResponse.Content);
                }
            }

            private async Task<UoR.Sys.LMS.UoRLearn.Models.LMSBase> getPersonPtr(UoR.Sys.LMS.UoRLearn.Models.Conditions conditions, ExecutionContext context)
            {
                RestRequest request = new RestRequest($"{endpoints.people}/searchQuery?type=internal", Method.Get);
                //request.JsonSerializer = Sys_Staff_LMS.RestsharpJsonSerializer.Default;
                request.AddHeader("SabaCertificate", await getLMSToken(context));
                request.AddJsonBody(conditions);

                RestResponse<UoR.Sys.LMS.UoRLearn.Models.PagedResponse> postPersonPtrResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.PagedResponse>(request,Method.Post);

                if (postPersonPtrResponse.IsSuccessful)
                {
                    return postPersonPtrResponse.Data.results.SingleOrDefault();
                }
                throw postPersonPtrResponse.ErrorException;
            }

            private async Task<UoR.Sys.LMS.UoRLearn.Models.Person> getExistingPerson(Employee employee, ExecutionContext context)
            {
                RestRequest request = new RestRequest($"{endpoints.people}/person_no={employee.personal_reference.ToUpper()}?type=internal", Method.Get);
                request.AddHeader("SabaCertificate", await getLMSToken(context));

                RestResponse<UoR.Sys.LMS.UoRLearn.Models.Person> getPersonResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.Person>(request, Method.Get);

                if (getPersonResponse.StatusCode == HttpStatusCode.InternalServerError && !getPersonResponse.Content.Contains("120067"))
                {
                    throw new Exception(getPersonResponse.Content);
                }

                if (getPersonResponse.IsSuccessful)
                {
                    return getPersonResponse.Data;
                }


                if (!String.IsNullOrWhiteSpace(employee.username))
                {
                    request = new RestRequest($"{endpoints.people}/custom1={employee.username.ToUpper()}?type=internal", Method.Get);
                    request.AddHeader("SabaCertificate", await getLMSToken(context));
                    getPersonResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.Person>(request,Method.Get);

                    if (getPersonResponse.IsSuccessful)
                    {
                        return getPersonResponse.Data;
                    }
                }


                if (!String.IsNullOrWhiteSpace(employee.upn))
                {
                    request = new RestRequest($"{endpoints.people}/username={employee.upn.ToUpper()}?type=internal", Method.Get);
                    request.AddHeader("SabaCertificate", await getLMSToken(context));
                    getPersonResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.Person>(request, Method.Get);

                    if (getPersonResponse.IsSuccessful)
                    {
                        return getPersonResponse.Data;
                    }
                }


                //if (!String.IsNullOrWhiteSpace(employee.email_address))
                //{
                //    request = new RestRequest($"{endpoints.people}/email={employee.email_address.ToLower()}?type=internal", DataFormat.None);
                //    request.AddHeader("SabaCertificate", await getLMSToken(context));
                //    getPersonResponse = sabaCloud.Get<UoR.Sys.LMS.UoRLearn.Models.Person>(request);

                //    if (getPersonResponse.IsSuccessful)
                //    {
                //        return getPersonResponse.Data;
                //    }
                //}





                return null;
            }




            private UoR.Sys.LMS.UoRLearn.Models.Person _newPerson(Employee employee)
            {
                return new UoR.Sys.LMS.UoRLearn.Models.Person()
                {
                    person_no = employee.personal_reference,
                    password = $"{generateRandomString(12)}_aZ0?",
                    status = "Inactive",
                    started_on = employee.employee_start_date,
                    locale_id = new UoR.Sys.LMS.UoRLearn.Models.Locale_Id { id = "local000000000000008", displayName = "English (United Kingdom)" },
                    customValues = new UoR.Sys.LMS.UoRLearn.Models.Customvalues(),
                    securityDomain = new UoR.Sys.LMS.UoRLearn.Models.Securitydomain { displayName = "Internal" },
                    home_domain = new UoR.Sys.LMS.UoRLearn.Models.Home_Domain { displayName = "Internal" }

                };
            }


            private async Task<UoR.Sys.LMS.UoRLearn.Models.Person> _putPerson(UoR.Sys.LMS.UoRLearn.Models.Person existing, ExecutionContext context)
            {
                RestRequest request = new RestRequest($"{endpoints.people}/{existing.id}", Method.Put);
                request.AddHeader("SabaCertificate", await getLMSToken(context));

                request.AddJsonBody(existing);

                RestResponse putPersonResponse = await sabaCloud.ExecuteAsync(request,Method.Put);

                if (putPersonResponse.IsSuccessful)
                {
                    return existing;
                }

                throw new Exception(putPersonResponse.Content);
            }

            private async Task<UoR.Sys.LMS.UoRLearn.Models.Person> _postPerson(UoR.Sys.LMS.UoRLearn.Models.Person existing, ExecutionContext context)
            {
                RestRequest request = new RestRequest($"{endpoints.people}?type=internal", Method.Post);
                request.AddHeader("SabaCertificate", await getLMSToken(context));

                request.AddJsonBody(existing);

                RestResponse<UoR.Sys.LMS.UoRLearn.Models.Person> postPersonResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.Person>(request,Method.Post);

                if (postPersonResponse.IsSuccessful)
                {
                    existing.id = postPersonResponse.Data.id;
                    return existing;
                }

                throw new Exception(postPersonResponse.Content);
            }

            private async Task<UoR.Sys.LMS.UoRLearn.Models.LMSBase> getOrganisationPtr(string leafId, ExecutionContext context)
            {
                string cacheKey = $"UoR_LMS_OrgPtr_{leafId}";

                ICache.RedisRead orgCache = await _cache.CacheRead(new ICache.RedisRead() { key = cacheKey });
                if (orgCache.hasValue)
                {
                    return orgCache.value.ToObject<UoR.Sys.LMS.UoRLearn.Models.LMSBase>();
                }

                RestRequest request = new RestRequest($"{endpoints.organisation}/?type=internal&count=1&q=custom0=={leafId}", Method.Get);
                request.AddHeader("SabaCertificate", await getLMSToken(context));

                RestResponse<UoR.Sys.LMS.UoRLearn.Models.PagedResponse> getOrgResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.PagedResponse>(request,Method.Get);

                if (getOrgResponse.IsSuccessful && getOrgResponse.Data.totalResults == 1)
                {
                    await _cache.CacheWrite(new ICache.RedisWrite() { key = cacheKey, value = getOrgResponse.Data.results.First(), expiry = 60 * 60 });
                    return getOrgResponse.Data.results.First();
                }
                return null;
            }

            private async Task<UoR.Sys.LMS.UoRLearn.Models.LMSBase> getOrganisationPtrByName(string name, ExecutionContext context)
            {
                name = HttpUtility.UrlEncode(name);
                string cacheKey = $"UoR_LMS_OrgPtr_{name}";

                ICache.RedisRead orgCache = await _cache.CacheRead(new ICache.RedisRead() { key = cacheKey });
                if (orgCache.hasValue)
                {
                    return orgCache.value.ToObject<UoR.Sys.LMS.UoRLearn.Models.LMSBase>();
                }

                RestRequest request = new RestRequest($"{endpoints.organisation}/?type=internal&count=1&q=name=={name}", Method.Get);
                request.AddHeader("SabaCertificate", await getLMSToken(context));

                RestResponse<UoR.Sys.LMS.UoRLearn.Models.PagedResponse> getOrgResponse = await sabaCloud.ExecuteAsync<UoR.Sys.LMS.UoRLearn.Models.PagedResponse>(request,Method.Get);

                if (getOrgResponse.IsSuccessful && getOrgResponse.Data.totalResults == 1)
                {
                    await _cache.CacheWrite(new ICache.RedisWrite() { key = cacheKey, value = getOrgResponse.Data.results.First(), expiry = 60 * 60 });
                    return getOrgResponse.Data.results.First();
                }
                return null;
            }

            private string CreateMD5<T>(T entity)
            {
                string input = Newtonsoft.Json.JsonConvert.SerializeObject(entity);
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2"));
                    }
                    return sb.ToString();
                }
            }


        }











        public class upsertRequestUoRLearn
        {
            public string lmsURL { get; set; }
            public string lmsUsername { get; set; }
            public string lmsPassword { get; set; }

            public JObject person { get; set; }
        }

        [FunctionName("Sys_Staff_UoRLearn_UpsertEmployee")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(upsertRequestUoRLearn))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UoR.Sys.LMS.UoRLearn.Models.Person))]
        public async Task<IActionResult> UpsertEmployee([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            try
            {

                upsertRequestUoRLearn request = await BodyReader.Read<upsertRequestUoRLearn>(req.Body);
                var worker = new lmsWorker(request, _cache);
                return await worker.upsertEmployee(request, context);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }

        [FunctionName("Sys_Staff_UoRLearn_UpsertExternal")]
        [OpenApiOperation]
        [OpenApiRequestBody("application/json", typeof(upsertRequestUoRLearn))]
        [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(UoR.Sys.LMS.UoRLearn.Models.Person))]
        public async Task<IActionResult> UpsertExternal([HttpTrigger(AuthorizationLevel.Function, "POST", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            try
            {

                upsertRequestUoRLearn request = await BodyReader.Read<upsertRequestUoRLearn>(req.Body);
                var worker = new lmsWorker(request, _cache);
                return await worker.upsertExternal(request, context);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }
    }
}
