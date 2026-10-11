

using FunctionsV2.Sys_CampusJobs.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Helpers;
using System.Net.Http.Headers;
using System.IO;
using System.Linq;

namespace FunctionsV2.Sys_CampusJobs.Services.Interfaces
{
    //Interface
    public interface ISapRequestService
    {
        public BaseFunctionResponse ConstructSapPayloadResponse(UpdateSapRequest updateSapRequest);
    }

    //Service
    public class SapRequestService : ISapRequestService
    {
        private readonly IConfiguration _config;

        public SapRequestService(IConfiguration config)
        {
            _config = config;
        }

        public BaseFunctionResponse ConstructSapPayloadResponse(UpdateSapRequest updateSapRequest)
        {
            try
            {
                //Construct request to push to SAP and call Campus Jobs API
                SapRequest sapRequest = ConstructSapRequest(updateSapRequest);
                HttpResponseMessage sapResponse = CallSapCampusJobsAPI(sapRequest, updateSapRequest.SapToken).Result;

                //If it is a failure, return the pre-defined errors, which match [dbo].[CJ_Ref_IntegrationStatus] 
                if (!sapResponse.IsSuccessStatusCode)               
                     return ConstructErrorResponse(sapResponse);

                //Campus Jobs API will return 200 even if there is a data error during processing.
                SapResponse response = JsonConvert.DeserializeObject<SapResponse>(sapResponse.Content.ReadAsStringAsync().Result);
                SapErrorResponse sapErrors = IdentifyErrorsInSapResponse(response);

                //Check for error messages in each step, return the first error.
                if (sapErrors != null)
                {                    
                    BaseFunctionResponse baseFunctionResponse = new()
                    {
                        Error = new BaseFunctionResponseError()
                        {
                            Code = $"{((int)ErrorCodes.BadRequest)}",
                            Message = $"Error while pushing user to SAP, failed on step number {sapErrors.Step.step.ToString()}, on position {sapErrors.PositionId}. " +
                                $"Message from SAP - {sapErrors.Step.message}"
                        }
                    };

                    if (sapErrors.Step.step > 2)
                    { 
                        TargetConnectCustomerUserPatch testFuncResponse = new()
                        {
                            CustomFields =
                            [
                                new CustomFields()
                                {
                                    VALUE41 = new VALUE41()
                                    {
                                        name = "Person Ref",
                                        value = new VALUE41.Value()
                                        {
                                            text = response.staffId
                                        }
                                    }
                                }
                            ]
                        };
                        baseFunctionResponse.Response = testFuncResponse;
                    }
                    return baseFunctionResponse;
                }

                //Construct the payload which will be used in the logic app to update Target Connect with the new information provided from SAP. 
                TargetConnectCustomerUserPatch targetConnectRequestPayload = ConstructTargetConnectCustomUserUpdatePayload(response, updateSapRequest);

                return new BaseFunctionResponse()
                { 
                    Response = new
                    {
                        result = targetConnectRequestPayload
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

        private SapErrorResponse IdentifyErrorsInSapResponse(SapResponse sapResponse)
        {
            List<string> errorMessages = new();

            foreach(SapCJPosition position in sapResponse.positions)
            {
                //Each position will contain an array called steps, giving details on the progress of each step in upserting the inbound data to existing SAP object. 
                foreach(Step step in position.steps)
                {
                    if(step.status.ToUpper() == "ERROR")
                    {
                        return new SapErrorResponse
                        {
                            Step = step,
                            PositionId = position.position,
                        };
                    }
                }
            }
            return null;
        }

        private BaseFunctionResponse ConstructErrorResponse(HttpResponseMessage httpResponseMessage)
        {
            string errorMessage = string.Empty;

            //Status' match the values in [uks-uorcache-d-001-sqldb].[dbo].[CJ_Ref_IntegrationStatus]
            switch (httpResponseMessage.StatusCode)
            {
                case HttpStatusCode.InternalServerError:
                    errorMessage = "Error-Unknown";
                    break;
                case HttpStatusCode.BadRequest:
                    errorMessage = "Error-Integration";
                    break;
            }

            return new BaseFunctionResponse()
            {
                Error = new BaseFunctionResponseError()
                {
                    Code = $"{httpResponseMessage.StatusCode}",
                    Message = errorMessage
                }
            };            
        }

        private SapRequest ConstructSapRequest(UpdateSapRequest updateSapRequest)
        {
            //Construct the SAP request using data provided by TargetConnect
            Employee sapEmployeeRequest = new()
            {
                personal = new Personal()
                {
                    firstname = updateSapRequest.CustomUser.firstName,
                    lastName = updateSapRequest.CustomUser.lastName,
                    title = updateSapRequest.CustomUser.customFields.REF365?.value.text,
                    staffId = updateSapRequest.CustomUser.customFields.VALUE41?.value.text,
                    upn = updateSapRequest.CustomUser.username,
                    studentId = updateSapRequest.CustomUser.customFields.UNIVERSITY_NUMBER?.value.text,
                    businessFirstName = updateSapRequest.CustomUser.firstName,
                    businessLastName = updateSapRequest.CustomUser.lastName,
                    formalName = $"{updateSapRequest.CustomUser.firstName} {updateSapRequest.CustomUser.lastName}",
                    legalSex = updateSapRequest.CustomUser.customFields.GENDER?.value.text,
                    nationality = updateSapRequest.CustomUser.customFields.REF3590?.value.text,
                    dateOfBirth = updateSapRequest.CustomUser.customFields.DATE_OF_BIRTH?.value.text,
                    email = updateSapRequest.CustomUser.email
                },
                address = new Address()
                {
                    address1 = updateSapRequest.CustomUser.customFields.ADDRESS1.value.text,
                    city = updateSapRequest.CustomUser.customFields.ADDRESS1.value.text, //TEMP
                    county = "",
                    postCode = updateSapRequest.CustomUser.customFields.ADDRESS4.value.text,
                    country = updateSapRequest.CustomUser.customFields.COUNTRY_OF_DOMICILE.value.text
                },
                employment = new Employment()
                {
                    primary = new PrimaryPosition(),
                    secondary = new List<SecondaryPosition>()
                }
            };

            //Order the list in ascending order of application id to ensure that the smallest number always comes first, which will be the primary record
            List<GetPlacementRecord> orderedPlacements = updateSapRequest.Placements.OrderBy(x => x.ApplicationId).ToList();
            for (int i = 0; i < orderedPlacements.Count; i++)
            {
                if (i == 0)
                {
                    sapEmployeeRequest.employment.primary = new PrimaryPosition()
                    {
                        serviceDate = updateSapRequest.Placements[i].PositionStartDate.ToString("yyyy-MM-dd"),
                        startDate = updateSapRequest.Placements[i].PositionStartDate.ToString("yyyy-MM-dd"),
                        endDate = updateSapRequest.Placements[i].PositionEndDate?.ToString("yyyy-MM-dd"),
                        positionPayScaleLevel = updateSapRequest.Placements[i].PayScaleLevel,
                        positionCode = updateSapRequest.Placements[i].StaffPositionID,
                        originalStartDate = updateSapRequest.Placements[i].PositionStartDate.ToString("yyyy-MM-dd"),
                    };
                }
                else
                    sapEmployeeRequest.employment.secondary.Add(ConstructSecondaryPosition(updateSapRequest.Placements[i]));
            }

            return new SapRequest
            {
                employee = sapEmployeeRequest               
            };
        }

        private SecondaryPosition ConstructSecondaryPosition(GetPlacementRecord placementRecord)
        {
            //Secondary position does not contain as many fields as the primary, therefore they do not use the same model.
            return new SecondaryPosition()
            {
                serviceDate = placementRecord.PositionStartDate.ToString("yyyy-MM-dd"),
                startDate = placementRecord.PositionStartDate.ToString("yyyy-MM-dd"),
                endDate = placementRecord.PositionEndDate?.ToString("yyyy-MM-dd"),
                positionPayScaleLevel = placementRecord.PayScaleLevel,
                positionCode = placementRecord.StaffPositionID
            };
        }

        private async Task<HttpResponseMessage> CallSapCampusJobsAPI(SapRequest sapRequest, string accessToken)
        {          
            using (HttpClient httpClient = new())
            {
                httpClient.BaseAddress = new Uri(_config["sapCampusJobsUrl"]);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                StringContent stringContent = new(JsonConvert.SerializeObject(sapRequest), Encoding.UTF8, "application/json");
                return await httpClient.PostAsync(httpClient.BaseAddress, stringContent);
            }
        }

        private TargetConnectCustomerUserPatch ConstructTargetConnectCustomUserUpdatePayload(SapResponse sapResponse, UpdateSapRequest updateSapRequest)
        {
            return new TargetConnectCustomerUserPatch()
            {
                Username = updateSapRequest.CustomUser.username,
                CustomFields =
                [
                    new CustomFields()
                    {
                        VALUE41 = new VALUE41()
                        {
                            name = "Person Ref",
                            value = new VALUE41.Value()
                            {
                                text = sapResponse.staffId,
                            }
                        },
                        //Update to TargetConnect to Inform is the SAP request was successful. 
                        SapStatus = new Trentstatus()
                        {
                            name = "SAP Status",
                            value = new Trentstatus.Value()
                            {
                                text = "On Sap",
                                code = "On Sap"
                            }
                        }
                    }
                ]
            };
        }
    }

}
