using FunctionsV2;
using FunctionsV2.Helpers;
using FunctionsV2.Helpers.Models;
using FunctionsV2.Sys_SymLib.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute.Exceptions;
using SIRSIDynix.Circulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionV2.Sys_SymLib
{

    //Interface
    public interface ISymLibService
    {
        BaseFunctionResponse GetUserFromSymLib(GetRequest request);

        BaseFunctionResponse UpsertUserInSymLib(UpsertRequest request);
    }

    //Service
    public class SymLibService : ISymLibService
    {
        private readonly ICache _cache;
        private readonly IConfiguration _config;

        public SymLibService(ICache cache,
            IConfiguration config)
        {
            _cache = cache;
            _config = config;
        }

        public BaseFunctionResponse GetUserFromSymLib(GetRequest request)
        {
            try
            {
                //Get new SessionToken
                string sessionToken = GetSessionToken(request.ClientId, request.Login).Result;
                    //DEV-TB: Should clintId and login be in config / in key vault instead of inbound parameters

                //Define endpoint
                CirculationEndpointClient client = new CirculationEndpointClient(
                    new CirculationEndpointClient.EndpointConfiguration(),
                    $"{_config["symlibRootURL"]}/soap/circulation"
                );

                //Call GetUser endpoint
                try
                {
                    //Call GetUser endpoint with 'PersonalReference'
                    getUserResponse1 getUserResponse = CallGetUserEndpoint(client, request.UserDetails.PersonalReference, sessionToken, request).Result;

                    return new BaseFunctionResponse
                    {
                        Response = getUserResponse.GetUserResponse.user
                    };
                }
                catch (Exception ex_personalRef)
                {
                    //Check if failure was because a user was not found
                    if (ex_personalRef.Message.ToUpper().Contains("USER NOT FOUND"))
                    {
                        try
                        {
                            //Instead, call GetUser endpoint with 'username' to see if they can be found
                            getUserResponse1 getUserResponse = CallGetUserEndpoint(client, request.UserDetails.Username, sessionToken, request).Result;

                            return new BaseFunctionResponse
                            {
                                Response = getUserResponse.GetUserResponse.user
                            };
                        }
                        catch (Exception ex_userName)
                        {
                            //If error message was not due to user not found, return thrown error
                            if (!ex_userName.Message.ToUpper().Contains("USER NOT FOUND"))
                            {
                                return new BaseFunctionResponse
                                {
                                    Error = new BaseFunctionResponseError
                                    {
                                        Code = $"{((int)ErrorCodes.Unknown)}",
                                        Message = $"{ex_userName.Message}"
                                    }
                                };
                            }
                            else
                            {
                                //If user was not found, return 'USER NOT FOUND' in response so LA can identify.
                                return new BaseFunctionResponse
                                {
                                    Error = new BaseFunctionResponseError
                                    {
                                        Code = $"{((int)ErrorCodes.NotFound)}",
                                        Message = $"USER NOT FOUND"
                                    }
                                };
                            }
                        }
                    }
                    else
                    {
                        //If failure when getting with 'personal_reference' was not related to 'USER NOT FOUND'
                        return new BaseFunctionResponse
                        {
                            Error = new BaseFunctionResponseError
                            {
                                Code = $"{((int)ErrorCodes.Unknown)}",
                                Message = $"{ex_personalRef.Message}"
                            }
                        };
                    }
                }                   
            }
            catch (Exception ex)
            {
                //Catch any exception that may have occurred before calling the GetUser endpoint
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

        public BaseFunctionResponse UpsertUserInSymLib(UpsertRequest request)
        {
            try
            {
                //Get new SessionToken
                string sessionToken = GetSessionToken(request.ClientId, request.Login).Result;

                //Define client
                CirculationEndpointClient client = new CirculationEndpointClient(
                    new CirculationEndpointClient.EndpointConfiguration(),
                    $"{_config["symlibRootURL"]}/soap/circulation"
                );

                SaveUserRequest saveUserRequest = new();
                switch(request.DataSource.ToUpper())
                {
                    case "CARDS-STAFF":
                    case "CARDS-STUDENT":
                        CardsClass cardsUpsertRequest = JsonConvert.DeserializeObject<CardsClass>(request.RequestObject.ToString());

                        BaseFunctionResponse getUserResponse = GetUserFromSymLib(new GetRequest
                        {
                            ClientId = request.ClientId,
                            Login = request.Login,
                            UserDetails = new UserDetails
                            {
                                PersonalReference = cardsUpsertRequest.UniversityId,
                                Username = cardsUpsertRequest.upn.Split('@')[0]
                            }
                        });

                        if(!getUserResponse.Error.IsNullOrDefault())
                            return getUserResponse;
                        //TODO - Need to improve this response

                        //Get getUser object 
                        string jsonGetUserResponse = JsonConvert.SerializeObject(getUserResponse.Response);
                        User getUserObject = JsonConvert.DeserializeObject<User>(jsonGetUserResponse);

                        //Compose Upsert Request
                        saveUserRequest = ComposeCardUpdateRequest(cardsUpsertRequest, request.OverrideCode, getUserObject);
                        break;

                    case "HRMS-STAFF":
                        StaffClass hrmsRequest = JsonConvert.DeserializeObject<StaffClass>(request.RequestObject.ToString());
                        HrmsUpsertResponse response = ComposeHRMSUpsertRequest(request, hrmsRequest).Result;                                                

                        if(response.Message != null)
                        {
                            return new BaseFunctionResponse
                            {
                                Error = new BaseFunctionResponseError
                                {
                                    Code = response.Code,
                                    Message = response.Message
                                }
                            };
                        }

                        //Compose Upsert Request
                        saveUserRequest = ComposeHrmsUpdateRequest(response.User, request.OverrideCode);
                        break;
                }                

                //Call SaveUser endpoint
                saveUserResponse1 saveUserResponse = CallUpsertUserEndpoint(client, sessionToken, request, saveUserRequest).Result;

                return new BaseFunctionResponse
                {
                    Response = new
                    {
                        result = saveUserResponse
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

        //----------------------------
        
        private async Task<getUserResponse1> CallGetUserEndpoint(CirculationEndpointClient client, string identifier, string sessionToken, GetRequest request)
        {
            return await client.getUserAsync(
                new SdHeader()
                {
                    locale = "en",
                    clientID = request.ClientId,
                    sessionToken = sessionToken
                },
                new GetUserRequest()
                {
                    ItemElementName = ItemChoiceType3.altID,
                    Item = identifier
                }
            );
        }

        private async Task<saveUserResponse1> CallUpsertUserEndpoint(CirculationEndpointClient client, string sessionToken, UpsertRequest request, SaveUserRequest saveUserRequest)
        {
            return await client.saveUserAsync(
                new SdHeader()
                { 
                    locale = "en",
                    clientID = request.ClientId,
                    sessionToken = sessionToken 
                }, 
                saveUserRequest 
            );
        }

        private SaveUserRequest ComposeCardUpdateRequest(CardsClass cardsRequest, string overrideCode, User existingUser)
        {
            existingUser.pin = cardsRequest.EmosPin;
            existingUser.userID = cardsRequest.EmosId;

            return new SaveUserRequest()
            {
                user = existingUser,
                privilegeOverride = overrideCode,
                webAuthIDOverride = overrideCode,
                userEditOverride = overrideCode
            };            
        }

        private SaveUserRequest ComposeHrmsUpdateRequest(User user, string overrideCode)
        {
            return new SaveUserRequest()
            {
                user = user,
                privilegeOverride = overrideCode,
                webAuthIDOverride = overrideCode,
                userEditOverride = overrideCode
            };
        }

        private async Task<string> GetSessionToken(string clientId, string login)
        {
            string hashKey = $"UoR_SymLib_SessionToken-{_config["symlibRootURL"].Substring(33)}";
            ICache.RedisRead sessionToken = await _cache.CacheRead(new ICache.RedisRead() { key = hashKey });
            if (sessionToken.hasValue)
            {
                return sessionToken.value.ToString();
            }
            SIRSIDynix.Security.SecurityEndpointClient client = new SIRSIDynix.Security.SecurityEndpointClient(new SIRSIDynix.Security.SecurityEndpointClient.EndpointConfiguration(), $"{_config["symlibRootURL"]}/soap/security");
            var loginResponse = await client.loginUserAsync(new SIRSIDynix.Security.SdHeader() { locale = "en", clientID = clientId }, new SIRSIDynix.Security.LoginUserRequest() { login = login });

            await _cache.CacheWrite(new ICache.RedisWrite() { key = hashKey, value = loginResponse.LoginUserResponse.sessionToken, expiry = 900 });
            return loginResponse.LoginUserResponse.sessionToken;
        }

        private async Task<HrmsUpsertResponse> ComposeHRMSUpsertRequest(UpsertRequest request, StaffClass requestObject)
        {
            //Validate object, ensure all mandatory fields are present
            ParseStaff parseStaff = new ParseStaff(requestObject);
            StaffClass person = parseStaff.person;

            if(!parseStaff.isValid)
            {
                return new HrmsUpsertResponse
                {
                    Code = $"{((int)ErrorCodes.BadRequest)}",
                    Message = $"Mandatory data is missing from the request, please ensured the following fields are passed in; " +
                                    $"personal_reference, username, staff_type, mainposition, applications.dtt."
                };
            }
                        
            BaseFunctionResponse existingUser = GetUserFromSymLib(new GetRequest
            {
                ClientId = request.ClientId,
                Login = request.Login,
                UserDetails = new UserDetails
                {
                    PersonalReference = requestObject.personal_reference,
                    Username = requestObject.username
                }
            });

            //Convert GetUser response into User object 
            string jsonGetUserResponse = JsonConvert.SerializeObject(existingUser.Response);
            User getUserObject = JsonConvert.DeserializeObject<User>(jsonGetUserResponse);

            //Define overseas staff types
            string[] ExcludeOverseasStaff = { "M-P", "MH-P" };

            //If there is a response from get user (user exists)
            if (getUserObject != null)
            {
                User upsertUser = await UpdateUser(getUserObject, request, person);
                if(upsertUser != null)
                {
                    return new HrmsUpsertResponse
                    {
                        User = upsertUser
                    };
                }
                else
                {
                    return new HrmsUpsertResponse
                    {
                        User = getUserObject,
                        Code = $"{((int)ErrorCodes.OK)}",
                        Message = $"Duplicate payload, no changes to the existing record. "
                    };
                }
            }

            //Else if there is no response from get user (user does not exist) and employee is not from overseas 
            else if (getUserObject == null && !ExcludeOverseasStaff.Contains(person.overseas_staff) && person.staff_type == "E")
            {
                return new HrmsUpsertResponse
                {
                   User = await CreateUser(request, person)
                };
            }

            //Return with no data
            return new HrmsUpsertResponse
            {
                Code = $"{((int)ErrorCodes.OK)}",
                Message = $"No upsert required - Employee is overseas staff. "
            };
        }

        private async Task<User> CreateUser(UpsertRequest request, StaffClass person)
        {
            User newUser = new User() { altID = person.personal_reference };

            newUser.userID = string.Concat("X", person.personal_reference);
            newUser.groupID = string.Empty;
            newUser.webAuthID = person.username;
            newUser.lastName = string.Concat(person.known_as_surname, ",", person.prefered_name);
            newUser.firstName = string.Empty;
            newUser.middleName = string.Empty;
            newUser.title = string.Empty;
            newUser.suffix = string.Empty;
            newUser.preferredNameUsed = false;
            newUser.birthDate = new NillableDate() { Item = true };
            newUser.libraryID = "MAIN-LIB";
            newUser.checkoutHistoryRuleSpecified = true;
            newUser.checkoutHistoryRule = CheckoutHistoryRule.CIRCRULE;
            newUser.profileID = "STAFF";
            newUser.languageID = "ENGLISH";
            newUser.pin = person.known_as_surname.Substring(0, Math.Min(4, person.known_as_surname.Length)).ToUpper();

            newUser.privilegeExpirationDate = new NillableDate() { Item = true };
            if (person.uni_leave_date.HasValue)
            {
                newUser.privilegeExpirationDate = new NillableDate() { Item = person.uni_leave_date.Value.Date };
            }

            newUser.userStandingID = "OK";
            newUser.mailingAddressIndex = 1;
            newUser.userCategory = [new UserCategoryID() { index = 1, entryID = "S" }];


            AddressData address0 = new AddressData()
            {
                index = 0,
                indexSpecified = true,
                addressLine = [
                    new EntryLineData() { lineKey = 1, lineKeySpecified = true, entryTypeID = "DEPARTMENT", entryValue = person.MainPosition.FacultyorSchoolName },
                    new EntryLineData() { lineKey = 2, lineKeySpecified = true, entryTypeID = "LINE" },
                    new EntryLineData() { lineKey = 3, lineKeySpecified = true, entryTypeID = "EMAIL", entryValue = person.email_address },
                ]
            };

            AddressData address1 = new AddressData()
            {
                index = 1,
                indexSpecified = true,
                addressLine = [
                    new EntryLineData() { lineKey = 1, lineKeySpecified = true, entryTypeID = "LINE", entryValue = person.address_line1 },
                    new EntryLineData() { lineKey = 2, lineKeySpecified = true, entryTypeID = "LINE", entryValue = person.address_line2 },
                    new EntryLineData() { lineKey = 3, lineKeySpecified = true, entryTypeID = "LINE", entryValue = person.city },
                    new EntryLineData() { lineKey = 4, lineKeySpecified = true, entryTypeID = "LINE", entryValue = string.Join(", ", [person.county, person.post_code] )},
                ]
            };

            newUser.address = [address0, address1];
            newUser.extendedInfo = [new EntryLineData() { entryTypeID = "HOLD_ID", entryValue = person.personal_reference }];
            newUser.departmentID = person.applications.dtt.risis_code;

            return newUser;
        }

        private async Task<User> UpdateUser(User existingUser, UpsertRequest request, StaffClass person)
        {
            if (!string.IsNullOrWhiteSpace(existingUser.lastName) && existingUser.lastName.Contains(","))
            {
                string lastSurname = existingUser.lastName.Split(",").First();
                existingUser.pin = lastSurname.Substring(0, Math.Min(4, lastSurname.Length)).ToUpper();
            }
            string existingHash = CreateUserMD5(existingUser);

            existingUser.altID = person.personal_reference;
            existingUser.lastName = string.Concat(person.known_as_surname, ",", person.prefered_name);
            existingUser.pin = person.known_as_surname.Substring(0, Math.Min(4, person.known_as_surname.Length)).ToUpper();
            existingUser.webAuthID = person.username;

            existingUser.privilegeExpirationDate = new NillableDate() { Item = true };
            if (person.uni_leave_date.HasValue)
            {
                existingUser.privilegeExpirationDate = new NillableDate() { Item = person.uni_leave_date.Value.Date };
            }


            SortedList<int, AddressData> existingAddresses = new SortedList<int, AddressData>(existingUser.address.ToDictionary(s => s.index, s => s));
            if (existingAddresses.ContainsKey(0))
            {
                existingAddresses[0].addressLine.Where(l => l.entryTypeID == "DEPARTMENT").OrderBy(l => l.lineKey).First().entryValue = person.MainPosition.FacultyorSchoolName;
                existingAddresses[0].addressLine.Single(l => l.entryTypeID == "EMAIL").entryValue = person.email_address;
            }
            else
            {
                existingAddresses.Add(0, new AddressData()
                {
                    index = 0,
                    indexSpecified = true,
                    addressLine = [
                        new EntryLineData() { lineKey = 1, lineKeySpecified = true, entryTypeID = "DEPARTMENT", entryValue = person.MainPosition.FacultyorSchoolName },
                        new EntryLineData() { lineKey = 2, lineKeySpecified = true, entryTypeID = "LINE" },
                        new EntryLineData() { lineKey = 3, lineKeySpecified = true, entryTypeID = "EMAIL", entryValue = person.email_address },
                    ]
                });
            }

            if (existingAddresses.ContainsKey(1))
            {
                SortedList<int, EntryLineData> address1Lines = new SortedList<int, EntryLineData>(existingAddresses[1].addressLine.ToDictionary(s => s.lineKey, s => s));

                string[] dataAddressLines = [person.address_line1, person.address_line2, person.city, string.Join(", ", [person.county, person.post_code])];

                for (int i = 1; i <= 4; i++)
                {
                    if (!string.IsNullOrWhiteSpace(dataAddressLines[i - 1]))
                    {
                        address1Lines[i] = new EntryLineData() { lineKey = i, lineKeySpecified = true, entryValue = dataAddressLines[i - 1] };
                    }
                    else
                    {
                        address1Lines[i] = new EntryLineData() { lineKey = i, lineKeySpecified = true, entryValue = null };
                    }
                }
            }
            else
            {
                existingAddresses.Add(1, new AddressData()
                {
                    index = 1,
                    indexSpecified = true,
                    addressLine = [
                        new EntryLineData() { lineKey = 1, lineKeySpecified = true, entryTypeID = "LINE", entryValue = person.address_line1 },
                        new EntryLineData() { lineKey = 2, lineKeySpecified = true, entryTypeID = "LINE", entryValue = person.address_line2 },
                        new EntryLineData() { lineKey = 3, lineKeySpecified = true, entryTypeID = "LINE", entryValue = person.city },
                        new EntryLineData() { lineKey = 4, lineKeySpecified = true, entryTypeID = "LINE", entryValue = string.Join(", ", [person.county, person.post_code] )},
                    ]
                });
            }

            existingUser.address = existingAddresses.OrderBy(a => a.Key).Select(a => a.Value).ToArray();
            existingUser.departmentID = person.applications.dtt.risis_code;

            string updatedHash = CreateUserMD5(existingUser);
            if (existingHash != updatedHash)
            {
                return existingUser;
            }
            return null;
        }

        public static string CreateUserMD5(User user)
        {
            string input = JsonConvert.SerializeObject(user);
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
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
}
