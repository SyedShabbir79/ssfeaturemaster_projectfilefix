using CampusJobs.Placements;
using FunctionsV2.Helpers;
using FunctionsV2.Sys_CampusJobs.Models;
using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using FunctionsV2.Helpers.Models;
using System.Collections.Generic;

namespace FunctionsV2.Sys_CampusJobs.Services.Interfaces
{
    //Interface
    public interface IProcessPlacementsService 
    {
        public BaseFunctionResponse CampusJobsProcessPlacements(DateTime lastRunTime);
    }

    //Service
    public class ProcessPlacementsService : IProcessPlacementsService
    {
        private readonly IKeyVault _keyVault;
        private XDocument doc;

        public ProcessPlacementsService(IKeyVault keyVault)
        {
            _keyVault = keyVault;
        }

        public BaseFunctionResponse CampusJobsProcessPlacements(DateTime lastRunTime)
        {
            try
            {
                getPlacementRecordsResponse1 studentPlacements = GetStudentPlacements(lastRunTime);

                //In the event there are no results, return a success message with a null PlacementRecord.
                if (studentPlacements.@return.total == 0)
                {
                    return new BaseFunctionResponse
                    {
                        Response = null
                    };
                }

                List<PlacementRecord> result = ConvertResponseToJsonArray(studentPlacements);

                var x = new BaseFunctionResponse
                {
                    Response = new
                    {
                        Result = result

                    }
                };
                return x;
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

        private getPlacementRecordsResponse1 GetStudentPlacements(DateTime lastRunTime)
        {
            PlacementServiceClient placementsService = new();

            getPlacementRecordRequest placementRecordRequest = CreateGetPlacementRecordRequest(lastRunTime);
            credentials credentialsRequest = GetCredentials();

            return placementsService.getPlacementRecordsAsync(placementRecordRequest, credentialsRequest).Result;
        }

        private getPlacementRecordRequest CreateGetPlacementRecordRequest(DateTime lastRunTime)
        {
            return new getPlacementRecordRequest
            {
                status = [placementRecordStatus.PLACED],
                updated = new dateRange
                {
                    to = DateTime.Now,
                    toSpecified = true,
                    from = lastRunTime,
                    fromSpecified = true
                },
            };
        }

        private credentials GetCredentials()
        {
            //TODO: TB - Update these secret names to be in config. 
            return new credentials
            {
                departmentKey = "Campus Jobs",
                username = _keyVault.GetSecret("uks-campusjobs-placements-username").Result,
                customerKey = _keyVault.GetSecret("uks-campusjobs-placements-customerkey").Result,
                password = _keyVault.GetSecret("uks-campusjobs-placements-password").Result,
                source = "AIS"
            };
        }

        private List<PlacementRecord> ConvertResponseToJsonArray(getPlacementRecordsResponse1 studentPlacements)
        {
            string xmlString = SerializeToXmlString(studentPlacements);
            doc = XDocument.Parse(xmlString);

            List<PlacementRecord> placements = new();

            foreach (servicePlacementRecord item in studentPlacements.@return.records)
            {
                PlacementRecord placementRecord = new()
                {
                    StudentID = item.user.externalId,
                    StaffID = CommonCampusJobsService.GetCustomFieldValueXml("Person Ref", doc),
                    SapPositionID = CommonCampusJobsService.GetCustomFieldValueXml("SAP Position ID", doc),
                    PositionStartDate = DateTime.Parse(CommonCampusJobsService.GetCustomFieldValueXml("Assignment start date", doc)),
                    PositionEndDate = DateTime.Parse(CommonCampusJobsService.GetCustomFieldValueXml("Assignment end date", doc)),
                    UpdatedAt = item.updated,
                    PayScaleLevel = item.placement.salaryRange,
                    Username = item.user.username,
                    TargetConnectID = item.user.uniqueId,
                    ApplicationID = item.id
                };

                placements.Add(placementRecord);
            }
            return placements;
        }

        private string SerializeToXmlString(object obj)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
            using (StringWriter textWriter = new())
            {
                xmlSerializer.Serialize(textWriter, obj);
                return textWriter.ToString();
            }
        }
    }
}
