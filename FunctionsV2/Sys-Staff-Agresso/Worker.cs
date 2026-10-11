using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using FunctionV2.Sys_Staff_Agresso.Helpers;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Data;
using FunctionsV2.Sys_Staff_Agresso;
using FunctionsV2.Common;
using Microsoft.Extensions.Configuration;
using FunctionsV2.Helpers;

namespace FunctionV2.Sys_Staff_Agresso
{
    public partial class UpdateAgressoStaff
    {
        public partial class Worker
        {
            private readonly IConfiguration _config;
            private readonly IKeyVault _kv;

            private Helpers.WSCreds _wscreds;
            private readonly Employee e;

            public Worker(IConfiguration config, IKeyVault kv, Employee employee)
            {
                this._config = config;
                this._kv = kv;
                this.e = employee;
            }

            private bool PreProcessCheck()
            {
                if (e != null && e.mainPosition != null)
                {
                    if (e.mainPosition.faculty != "SUMS")
                    {
                        return true;
                    }
                }
                return false;
            }

            private async Task<Agresso.Personnel.PersonnelObject> upsertPersonnelUnit(Agresso.Personnel.PersonnelV201010SoapClient pClient, Agresso.Personnel.PersonnelObject p)
            {
                bool pExists = true;
                
                if (p is null)
                {
                    pExists = false;

                    p = new Agresso.Personnel.PersonnelObject()
                    {
                        MainResID = e.personal_reference,
                        Company = e.Company,
                        ResourceId = e.personal_reference,
                        AddressList = new Agresso.Personnel.AddressUnitType[] {
                            new Agresso.Personnel.AddressUnitType() {
                                eMail = e.email_address,
                                Address = e.pay_point,
                                CountryCode = e.Company == "RM" ? "MY" : "GB"
                            }
                        },
                        RelationValueList = new Agresso.Personnel.PersonnelRelationValueUnitType[]
                        {
                            new Agresso.Personnel.PersonnelRelationValueUnitType() {
                                ResourceId = e.personal_reference,
                                Status = "N",
                                RelationValue = "STF",
                                RelAttrId = "B2",
                                DateFrom = e.uni_start_date,
                                DateTo = Helpers.statics.dateToMax
                            },
                            new Agresso.Personnel.PersonnelRelationValueUnitType() {
                                ResourceId = e.personal_reference,
                                Status = "N",
                                RelationValue = "STF",
                                RelAttrId = "B3",
                                DateFrom = e.uni_start_date,
                                DateTo = Helpers.statics.dateToMax
                            }
                        },
                        SupplierDetails = new Agresso.Personnel.PersonnelSupplierUnitType()
                        {
                            AparGrId = "30",
                            PayMethod = e.Company == "RM" ? "AP" : "IP",
                            BankAccount = Common_Crypt.DecryptCombined("F957FDFE-FECB-4F56-BE26-2B761F43C2A9", e.bank_account, "0"),
                            PostalAcc = Common_Crypt.DecryptCombined("F957FDFE-FECB-4F56-BE26-2B761F43C2A9", e.roll_number, "0"),
                            ClearingCode = Common_Crypt.DecryptCombined("F957FDFE-FECB-4F56-BE26-2B761F43C2A9", e.sort_code, "0")
                        },
                        DateFrom = e.uni_start_date,
                        EducationYear = 0,
                        Language = "EN",
                        ParttimePct = 100,
                        PayPeriod = 0,
                        PayStatus = "N",
                        PayYear = 0,
                        PeriodFrom = statics.GetPeriodFrom(e.uni_start_date),
                        ResourceTyp = "E",
                        SupplierId = e.personal_reference,
                        TransId = 0
                    };
                }

                if (p.TaxNumber == "0")
                    p.TaxNumber = string.Empty;

                if (p.TaxType == "0")
                    p.TaxType = string.Empty;

                if (p.Title == "0")
                    p.Title = string.Empty;

                p.DateTo = e.uni_leave_date.GetValueOrDefault(statics.dateToMax).Date;
                p.PeriodTo = statics.GetPeriodTo(p.DateTo.Date);
                p.Status = "N";

                p.ShortName = e.personal_reference;
                p.SupplierId = e.personal_reference;

                p.FirstName = e.prefered_name;
                p.SurName = e.known_as_surname;
                p.Name = $"{e.known_as_surname}, {e.prefered_name}";

                p.Language = "EN";
                p.PayStatus = "N";


                if (pExists)
                {
                    var pu = JsonConvert.DeserializeObject<Agresso.Personnel.PersonnelUnitType>(JsonConvert.SerializeObject(p));
                    var r = await pClient.UpdatePersonnelAsync(pu, true, true, _wscreds.pCreds);

                    if (r.Body.UpdatePersonnelResult.ReturnCode != 12 && r.Body.UpdatePersonnelResult.ReturnText != "Update OK.")
                    {
                        throw new AgressoStaffException("upsertPersonnelUnit", r.Body.UpdatePersonnelResult);
                    }
                }
                else
                {
                    var r = await pClient.CreatePersonnelAsync(p, true, _wscreds.pCreds);
                    if (r.Body.CreatePersonnelResult.ReturnCode != 12 && r.Body.CreatePersonnelResult.ReturnText != "Saved OK.")
                    {
                        throw new AgressoStaffException("upsertPersonnelUnit", r.Body.CreatePersonnelResult);
                    }
                }
                return p;
            }


            private async Task upsertPerRelValues(Agresso.Personnel.PersonnelV201010SoapClient pClient, Agresso.Personnel.PersonnelObject p)
            {
                List<Agresso.Personnel.PersonnelRelationValueUnitType> relValuesNew = new List<Agresso.Personnel.PersonnelRelationValueUnitType>();

                var b2 = p.RelationValueList.SingleOrDefault(r => r.RelAttrId == "B2");
                var b3 = p.RelationValueList.SingleOrDefault(r => r.RelAttrId == "B3");

                if (b2 is null)
                {
                    b2 = new Agresso.Personnel.PersonnelRelationValueUnitType()
                    {
                        ResourceId = e.personal_reference,
                        Status = "N",
                        RelationValue = "STF",
                        RelAttrId = "B2",
                        DateFrom = p.DateFrom.Date,
                        DateTo = Helpers.statics.dateToMax
                    };
                    relValuesNew.Add(b2);
                }

                if (b3 is null)
                {
                    b3 = new Agresso.Personnel.PersonnelRelationValueUnitType()
                    {
                        ResourceId = e.personal_reference,
                        Status = "N",
                        RelationValue = "STF",
                        RelAttrId = "B3",
                        DateFrom = p.DateFrom.Date,
                        DateTo = Helpers.statics.dateToMax
                    };
                    relValuesNew.Add(b3);
                }

                if (relValuesNew.Count > 0)
                {
                    var resp = await pClient.AddRelationValuesAsync(e.Company, e.personal_reference, relValuesNew.ToArray(), true, _wscreds.pCreds);
                    if (resp.Body.AddRelationValuesResult.ReturnCode != 12 || resp.Body.AddRelationValuesResult.ReturnText != "Saved OK.")
                    {
                        throw new AgressoStaffException("upsertPerRelValues", resp.Body.AddRelationValuesResult);
                    }
                }

                if (!b2.DateTo.Equals(Helpers.statics.dateToMax) || !b3.DateTo.Equals(Helpers.statics.dateToMax))
                {
                    b2.DateTo = Helpers.statics.dateToMax;
                    b3.DateTo = Helpers.statics.dateToMax;

                    var resp = await pClient.UpdateRelationValuesAsync(e.Company, e.personal_reference, new Agresso.Personnel.PersonnelRelationValueUnitType[] { b2, b3 }, false, true, _wscreds.pCreds);
                    if (resp.Body.UpdateRelationValuesResult.ReturnCode != 12 || resp.Body.UpdateRelationValuesResult.ReturnText != "Update OK.")
                    {
                        throw new AgressoStaffException("upsertPerRelValues", resp.Body.UpdateRelationValuesResult);
                    }
                }
            }

            private async Task upsertPerAdd(Agresso.Personnel.PersonnelV201010SoapClient pClient, Agresso.Personnel.PersonnelObject p)
            {
                var perAdd = p.AddressList.SingleOrDefault(a => a.AddressType == "1" && a.SequenceNo == 0);

                if (perAdd is null)
                {
                    perAdd = new Agresso.Personnel.AddressUnitType() { AddressType = "1", SequenceNo = 0, eMail = e.email_address, Address = e.pay_point };
                    var resp = await pClient.AddAddressAsync(e.Company, e.personal_reference, new[] { perAdd }, true, _wscreds.pCreds);
                    if (resp.Body.AddAddressResult.ReturnCode != 1 || resp.Body.AddAddressResult.ReturnText != "Saved OK.")
                    {
                        throw new AgressoStaffException(resp.Body.AddAddressResult.PersonnelAddressValidationResponseList.First().FieldErrors.First().ErrorMessage);
                    }
                }
                else
                {
                    perAdd.eMail = e.email_address;
                    perAdd.Address = e.pay_point;
                    var resp = await pClient.UpdateAddressAsync(e.Company, e.personal_reference, new[] { perAdd }, true, true, _wscreds.pCreds);
                    if (resp.Body.UpdateAddressResult.ReturnCode != 12 || resp.Body.UpdateAddressResult.ReturnText != "Update OK.")
                    {
                        throw new AgressoStaffException(resp.Body.UpdateAddressResult.PersonnelAddressValidationResponseList.First().FieldErrors.First().ErrorMessage);
                    }
                }
            }

            private async Task<Agresso.Personnel.PersonnelObject> upsertPersonnel(Agresso.Personnel.PersonnelV201010SoapClient pClient, Agresso.Personnel.PersonnelObject p)
            {
                bool pExists = false;

                if (p is not null)
                {
                    pExists = true;

                    if (p.RelationValueList.Count(rv => rv.RelAttrId == "B0") > 0)
                        throw new AgressoStaffException("UpdatePersonnel: B0 Relation Values found!");

                    if (p.RelationValueList.Count(rv => rv.Status == "P") > 0)
                        throw new AgressoStaffException("UpdatePersonnel: Parked Relation Values found!");

                    if (p.RelationValueList.Count(rv => rv.RelationValue == "STF" && (rv.RelAttrId == "B2" || rv.RelAttrId == "B3")) > 2)
                        throw new AgressoStaffException("UpdatePersonnel: Multiple B2/3 relational values detected!");
                }

                p = await upsertPersonnelUnit(pClient, p);

                //Upsert Personnel twice on creation as the name doesn't stick first time.
                if (!pExists)
                    p = await upsertPersonnelUnit(pClient, p);

                await upsertPerRelValues(pClient, p);
                await upsertPerAdd(pClient, p);
                return p;
            }

            private async Task<Agresso.Supplier.SupplierObject> upsertSupplier(Agresso.Supplier.SupplierSoapClient sClient, Agresso.Supplier.SupplierResponse sr)
            {
                sr.SupplierType = await upsertSupplierUnit(sClient, sr);
                if (sr.ReturnCode == 40)
                {
                    await upsertSupplierAddress(sClient, sr);
                    await upsertSupplierFlexi(sClient, sr);
                }
                return sr.SupplierType;
            }


            private async Task upsertSupplierAddress(Agresso.Supplier.SupplierSoapClient sClient, Agresso.Supplier.SupplierResponse sr)
            {
                var address = sr.SupplierType.AddressList.SingleOrDefault(a => a.SequenceNo == 0 && a.AddressType == "1");
                if (address is null)
                {
                    address = new Agresso.Supplier.AddressUnitType()
                    {
                        SequenceNo = 0,
                        AddressType = "1",
                        CountryCode = e.Company == "RM" ? "MY" : "GB",
                        eMail = e.email_address,
                        Address = e.pay_point
                    };

                    var r = await sClient.AddAddressAsync(e.Company, e.personal_reference, new Agresso.Supplier.AddressUnitType[] { address }, true, _wscreds.sCreds);
                    if (r.Body.AddAddressResult.ReturnCode != 0 || r.Body.AddAddressResult.ReturnText != "Saved OK")
                    {
                        throw new AgressoStaffException(r.Body.AddAddressResult.ChildErrorInfoList.FirstOrDefault().ErrorMessage);
                    }
                }
                else
                {
                    address.CountryCode = e.Company == "RM" ? "MY" : "GB";
                    address.eMail = e.email_address;
                    address.Address = e.pay_point;

                    var r = await sClient.UpdateAddressAsync(e.Company, e.personal_reference, new Agresso.Supplier.AddressUnitType[] { address }, true, true, _wscreds.sCreds);
                    if (r.Body.UpdateAddressResult.ReturnCode != 0 || r.Body.UpdateAddressResult.ReturnText != "Saved OK")
                    {
                        throw new AgressoStaffException(r.Body.UpdateAddressResult.ChildErrorInfoList.FirstOrDefault().ErrorMessage);
                    }
                }
            }

            private async Task upsertSupplierFlexi(Agresso.Supplier.SupplierSoapClient sClient, Agresso.Supplier.SupplierResponse sr)
            {
                var fg = sr.SupplierType.FlexiGroupList.SingleOrDefault(fg => fg.FlexiGroup == "SUPPINFO");
                if (fg is not null)
                {
                    var fr = fg.FlexiFieldRowList.SingleOrDefault(fr => fr.RowNo == 0);
                    if (fr is not null)
                    {
                        var hp = fr.FlexiFieldList.SingleOrDefault(ff => ff.ColumnName == "homepostcode");
                        var c = fr.FlexiFieldList.SingleOrDefault(ff => ff.ColumnName == "country");

                        if (hp is null)
                            throw new AgressoStaffException("SUPPINFO Row 0, HomePostcode Missing!");

                        if (c is null)
                            throw new AgressoStaffException("SUPPINFO Row 0, Country Missing!");

                        hp.Value = e.post_code;
                        c.Value = e.country;

                        var r = await sClient.UpdateFlexiFieldRowAsync(e.Company, e.personal_reference, new Agresso.Supplier.FlexiGroupUnitType[] { fg }, true, true, _wscreds.sCreds);
                        if (r.Body.UpdateFlexiFieldRowResult.ReturnCode != 0 || r.Body.UpdateFlexiFieldRowResult.ReturnText != "Saved OK")
                        {
                            throw new AgressoStaffException(r.Body.UpdateFlexiFieldRowResult.ChildErrorInfoList.FirstOrDefault().ErrorMessage);
                        }

                        return;
                    }
                    throw new AgressoStaffException("SUPPINFO Row 0 Missing!");
                }
                throw new AgressoStaffException("SUPPINFO Missing!");
            }

            private async Task<Agresso.Supplier.SupplierObject> upsertSupplierUnit(Agresso.Supplier.SupplierSoapClient sClient, Agresso.Supplier.SupplierResponse sr)
            {
                var s1 = await sClient.GetSupplierAsync(e.Company, e.personal_reference, false, _wscreds.sCreds);
                sr = s1.Body.GetSupplierResult;

                if (sr.ReturnCode == 41)
                {
                    sr.SupplierType = new Agresso.Supplier.SupplierObject()
                    {
                        Company = e.Company,
                        SupplierID = e.personal_reference,
                        SupplierGroupID = "30",
                        BankAccount = "0",
                        PostalAccount = "0",
                        ClearingCode = "0",
                        TaxFilingRequirement = "0",
                        FlexiGroupList = new Agresso.Supplier.FlexiGroupUnitType[] {
                            new Agresso.Supplier.FlexiGroupUnitType() {  FlexiGroup = "SUPPINFO",
                                FlexiFieldRowList = new Agresso.Supplier.FlexiRowUnitType[]
                                {
                                    new Agresso.Supplier.FlexiRowUnitType()
                                    {
                                          RowNo = 0,
                                          FlexiFieldList = new Agresso.Supplier.FlexiFieldUnitType[]
                                          {
                                              new Agresso.Supplier.FlexiFieldUnitType() { ColumnName = "homepostcode", Value = e.post_code },
                                              new Agresso.Supplier.FlexiFieldUnitType() {ColumnName = "country", Value = e.country }
                                          }
                                    }
                                }
                            }
                        },
                        AddressList = new Agresso.Supplier.AddressUnitType[]
                        {
                            new Agresso.Supplier.AddressUnitType()
                            {
                                SequenceNo = 0,
                                AddressType = "1",
                                CountryCode = e.Company == "RM" ? "MY" : "GB",
                                eMail = e.email_address,
                                Address = e.pay_point
                            }
                        }
                    };
                }

                sr.SupplierType.PaymentTerms = "C0";
                sr.SupplierType.Language = "EN";
                sr.SupplierType.Currency = "GBP";
                sr.SupplierType.PayMethod = e.Company == "RM" ? "AP" : "IP";
                sr.SupplierType.ExternalReference = ".";

                sr.SupplierType.SupplierName = $"{e.prefered_name} {e.known_as_surname}";
                sr.SupplierType.CountryCode = e.Company == "RM" ? "MY" : "GB";
                sr.SupplierType.ShortName = e.known_as_surname.Substring(0, Math.Min(3, e.known_as_surname.Length)).ToUpper();

                if (e.Company == "RU")
                {
                    sr.SupplierType.BankAccount = Common_Crypt.DecryptCombined("F957FDFE-FECB-4F56-BE26-2B761F43C2A9", e.bank_account, "0");
                    sr.SupplierType.PostalAccount = Common_Crypt.DecryptCombined("F957FDFE-FECB-4F56-BE26-2B761F43C2A9", e.roll_number, "0");
                    sr.SupplierType.ClearingCode = Common_Crypt.DecryptCombined("F957FDFE-FECB-4F56-BE26-2B761F43C2A9", e.sort_code, "0");
                }

                // sr.SupplierType.TaxFilingRequirement = "0";
                sr.SupplierType.Status = "N";
                sr.SupplierType.ExpiryDate = e.uni_leave_date.GetValueOrDefault(Helpers.statics.dateToMax);

                if (string.IsNullOrWhiteSpace(sr.SupplierType.TaxFilingRequirement))
                {
                    sr.SupplierType.TaxFilingRequirement = "0";
                }

                if (sr.ReturnCode == 41)
                {
                    var r = await sClient.CreateSupplierAsync(sr.SupplierType, true, _wscreds.sCreds);
                    if (r.Body.CreateSupplierResult.ReturnCode != 0 || r.Body.CreateSupplierResult.ReturnText != "Saved OK")
                    {
                        throw new AgressoStaffException("upsertSupplierUnit", r.Body.CreateSupplierResult);
                    }
                }
                else
                {
                    var su = JsonConvert.DeserializeObject<Agresso.Supplier.SupplierUnitType>(JsonConvert.SerializeObject(sr.SupplierType));
                    var r = await sClient.UpdateSupplierAsync(su, true, true, _wscreds.sCreds);
                    if (r.Body.UpdateSupplierResult.ReturnCode != 0 || r.Body.UpdateSupplierResult.ReturnText != "Saved OK")
                    {
                        throw new AgressoStaffException("upsertSupplierUnit", r.Body.UpdateSupplierResult);
                    }
                }
                return sr.SupplierType;
            }

            public async Task<IActionResult> processUpdate()
            {
                if (PreProcessCheck())
                {
                    _wscreds = new WSCreds(e.Company, "ITESB", await _kv.GetSecret("UoR-Common-Agresso-Password"));

                    string agressoEndpoint = _config["agressoWebServices"];

                    if (String.IsNullOrWhiteSpace(agressoEndpoint) && !Uri.IsWellFormedUriString(agressoEndpoint, UriKind.Absolute))
                        throw new AgressoStaffException("Invalid agressoWebServices URI");

                    Agresso.Personnel.PersonnelV201010SoapClient pClient = new Agresso.Personnel.PersonnelV201010SoapClient();
                    pClient.Endpoint.Address = new System.ServiceModel.EndpointAddress(agressoEndpoint);

                    Agresso.Supplier.SupplierSoapClient sClient = new Agresso.Supplier.SupplierSoapClient();
                    sClient.Endpoint.Address = new System.ServiceModel.EndpointAddress(agressoEndpoint);

                    Agresso.Users.UserAdministrationV200702SoapClient uClient = new Agresso.Users.UserAdministrationV200702SoapClient();
                    uClient.Endpoint.Address = new System.ServiceModel.EndpointAddress(agressoEndpoint);

                    Agresso.QueryEngine.QueryEngineV201101SoapClient qeClient = new Agresso.QueryEngine.QueryEngineV201101SoapClient();
                    qeClient.Endpoint.Address = new System.ServiceModel.EndpointAddress(agressoEndpoint);

                    var pReq = pClient.GetPersonnelAsync(e.Company, e.personal_reference, false, _wscreds.pCreds);
                    var sReq = sClient.GetSupplierAsync(e.Company, e.personal_reference, false, _wscreds.sCreds);
                    var qeReq = qeClient.GetTemplateResultAsDataSetAsync(new Agresso.QueryEngine.InputForTemplateResult()
                    {
                        TemplateId = 9459,
                        SearchCriteriaPropertiesList = new Agresso.QueryEngine.SearchCriteriaProperties[] {
                            new Agresso.QueryEngine.SearchCriteriaProperties() {
                                ColumnName = "dim_value", DataType = 10, RestrictionType = "=", FromValue = e.personal_reference
                            }
                        }
                    }, _wscreds.qeCredsForcedClient("RU"));

                    var userDefexpReq = uClient.GetUserAsync("DEFEXP", _wscreds.uCreds);

                    await Task.WhenAll(pReq, sReq, qeReq, userDefexpReq);

                    var p = pReq.Result.Body.GetPersonnelResult;
                    var s = sReq.Result.Body.GetSupplierResult;
                    var qe = qeReq.Result.TemplateResult;

                    var activeUsers = qe.Tables["AgressoQE"].AsEnumerable().Select(r => new
                    {
                        dim_value = r.Field<string>("dim_value"),
                        status = r.Field<string>("status__2"),
                        username = r.Field<string>("user_id")
                    }).ToList();

                    List<string> usernames = activeUsers.Where(u => u.status == "N").Select(u => u.username.ToUpper()).ToList();
                    usernames.Add(e.username.ToUpper());

                    List<Agresso.Users.User> au = new List<Agresso.Users.User>();

                    foreach (var username in usernames.Distinct())
                    {
                        var uReq = await uClient.GetUserAsync(username, _wscreds.uCreds);
                        if (uReq.Body.GetUserResult != null && uReq.Body.GetUserResult.Status == Agresso.Users.Status.Active)
                            au.Add(uReq.Body.GetUserResult);
                    }


                    var userDefexp = userDefexpReq.Result.Body.GetUserResult;


                    if (e.staff_type == "E" && e.uni_leave_date.GetValueOrDefault(statics.dateToMax) > System.DateTime.Now.Date)
                    {
                        //Current Employee, Upsert PSU
                        p = await upsertPersonnel(pClient, p);
                        s.SupplierType = await upsertSupplier(sClient, s);

                        await upsertUser(uClient, au, userDefexp);
                        return new OkObjectResult(new { action = "upsert", p = p, s = s.SupplierType, u = au });
                    }
                    else
                    {
                        if (p is not null && p.Status == "N")
                            //Leaver with active P, park P
                            await parkPersonnel(pClient, p);

                        if (s is not null && s.SupplierType.Status == "N")
                            //Leaver with active S, park S
                            await parkSupplier(sClient, s);

                        if (au.Count > 0)
                            await parkUsers(uClient, au);

                        return new OkObjectResult(new { action = "park", p = p, s = s.SupplierType, u = au });
                    }
                }
                return new NoContentResult();
            }

            private async Task parkSupplier(Agresso.Supplier.SupplierSoapClient sClient, Agresso.Supplier.SupplierResponse sr)
            {
                sr.SupplierType.Status = "P";
                sr.SupplierType.ExpiryDate = e.uni_leave_date.GetValueOrDefault(Helpers.statics.dateToMax);

                var su = JsonConvert.DeserializeObject<Agresso.Supplier.SupplierUnitType>(JsonConvert.SerializeObject(sr.SupplierType));
                var r = await sClient.UpdateSupplierAsync(su, true, true, _wscreds.sCreds);
                if (r.Body.UpdateSupplierResult.ReturnCode != 0 || r.Body.UpdateSupplierResult.ReturnText != "Saved OK")
                {
                    throw new AgressoStaffException("parkSupplier", r.Body.UpdateSupplierResult);
                }
            }

            private async Task parkPersonnel(Agresso.Personnel.PersonnelV201010SoapClient pClient, Agresso.Personnel.PersonnelObject p)
            {
                p.DateTo = e.uni_leave_date.GetValueOrDefault(statics.dateToMax).Date;
                p.PeriodTo = statics.GetPeriodTo(p.DateTo.Date);
                p.Status = "P";

                var pu = JsonConvert.DeserializeObject<Agresso.Personnel.PersonnelUnitType>(JsonConvert.SerializeObject(p));
                var r = await pClient.UpdatePersonnelAsync(pu, true, true, _wscreds.pCreds);

                if (r.Body.UpdatePersonnelResult.ReturnCode != 12 && r.Body.UpdatePersonnelResult.ReturnText != "Update OK.")
                {
                    throw new AgressoStaffException("parkPersonnel", r.Body.UpdatePersonnelResult.PersonnelValidationResponseList.First().FieldErrors.First().ErrorMessage);
                }
            }

            private async Task parkUsers(Agresso.Users.UserAdministrationV200702SoapClient uClient, List<Agresso.Users.User> au)
            {
                foreach (var u in au)
                {
                    u.Status = Agresso.Users.Status.Parked;
                    u.ValidTo = System.DateTime.Now;
                    u.UserTypes = new Agresso.Users.UserType[] { new Agresso.Users.UserType() { ClientId = "RU", MasterFile = Agresso.Users.MasterFileType.Resource, MasterFileValue = "999999" } };

                    var uRoles = u.Roles;
                    foreach (string ar in uRoles.Where(r => r.Clients.Any(c => c.Status == Agresso.Users.Status.Active)).Select(r => r.RoleId))
                    {
                        foreach (var r in uRoles.Single(r => r.RoleId == ar).Clients)
                        {
                            r.Status = Agresso.Users.Status.Parked;
                        }
                    }
                    u.Roles = uRoles;


                    var uClients = u.Clients;
                    foreach (var c in uClients.Where(c=>c.Status == Agresso.Users.Status.Active))
                    {
                        c.Status = Agresso.Users.Status.Parked;
                    }
                    u.Clients = uClients.ToArray();


                    var uResp = await uClient.ModifyUsersAsync(new Agresso.Users.User[] { u }, true, _wscreds.uCreds);

                    if (uResp.Body.ModifyUsersResult.RequestCommitted != true)
                        throw new AgressoStaffException("parkUsers", uResp.Body.ModifyUsersResult);

                    var updatedU = await uClient.GetUserAsync(u.UserId, _wscreds.uCreds);

                    if (updatedU.Body.GetUserResult.Status != u.Status)
                        throw new AgressoStaffException("parkUsers", $"User updated but status is still {updatedU.Body.GetUserResult.Status} and not {u.Status}");
                }
            }

            private async Task upsertUser(Agresso.Users.UserAdministrationV200702SoapClient uClient, List<Agresso.Users.User> au, Agresso.Users.User userDefexp)
            {
                if (e.Company != "RM" && !string.IsNullOrWhiteSpace(e.username) && !string.IsNullOrWhiteSpace(e.email_address))
                {
                    var primary = au.FirstOrDefault();

                    if (primary is null)
                    {
                        var bup = new Agresso.Users.BasicUserProperties() { Client = "RU", Email = e.email_address, UserId = e.username, UserName = e.username, Description = $"{e.prefered_name} {e.known_as_surname}", Password = Guid.NewGuid().ToString() };
                        var clone = await uClient.CreateUsersByCloningAsync(new Agresso.Users.BasicUserProperties[] { bup }, userDefexp.UserId, true, _wscreds.uCreds);
                        if (clone.Body.CreateUsersByCloningResult.RequestCommitted && clone.Body.CreateUsersByCloningResult.ItemsFailed == 0)
                        {
                            var getNewUser = await uClient.GetUserAsync(e.username, _wscreds.uCreds);
                            primary = getNewUser.Body.GetUserResult;
                            au.Add(primary);
                        }
                        else if (!clone.Body.CreateUsersByCloningResult.RequestCommitted && clone.Body.CreateUsersByCloningResult.Items.First().Message.Any(m => m == "User ID already exists."))
                        {
                            var getExistingUser = await uClient.GetUserAsync(bup.UserName, _wscreds.uCreds);
                            primary = getExistingUser.Body.GetUserResult;
                            au.Add(primary);
                        }
                    }

                    if (au.Count > 1)
                    {
                        primary = au.SingleOrDefault(u => u.IdentityServicesInfo.ToLower() == e.upn.ToLower());

                        if (primary is null)
                            primary = au.SingleOrDefault(u => u.DomainUser.ToLower() == $"rdg-home\\{e.username.ToLower()}");

                        if (primary is null)
                            primary = au.SingleOrDefault(u => u.UserName.ToLower() == e.username.ToLower());

                        if (primary is null)
                            primary = au.SingleOrDefault(u => u.UserId.ToLower() == e.username.ToLower());
                    }

                    if (primary is null)
                        throw new AgressoStaffException("Unable to identity Primary user account!");




                    string workNumber = string.Empty;
                    if (!string.IsNullOrWhiteSpace(e.work_internal_phone) && e.work_internal_phone != "100")
                    {
                        workNumber = e.work_internal_phone;
                        if (workNumber.Length > 35)
                            workNumber = workNumber.Substring(0, 35);
                    }


                    foreach (var u in au)
                    {
                        if (u.AddressList.Count(a => a.Type == "1") > 1)
                            throw new AgressoStaffException($"Multiple User Address Type 1 found on {u.UserId}!");

                        var add1 = u.AddressList.SingleOrDefault(a => a.Type == "1");
                        if (add1 is null)
                            add1 = new Agresso.Users.Address() { Type = "1", EMailList = new Agresso.Users.ArrayOfString() };

                        add1.Telephone = workNumber;
                        add1.EMailList.Clear();
                        add1.EMailList.Add(e.email_address);
                        add1.CountryCode = "GB";

                        u.AddressList = new Agresso.Users.Address[] { add1 };



                        var userRoles = u.Roles.ToList();
                        string[] requiredRoles = userDefexp.Roles.Select(r => r.RoleId).ToArray();
                        foreach (var rr in requiredRoles)
                        {
                            int roleCount = userRoles.Count(r => r.RoleId == rr);

                            if (roleCount > 1)
                                throw new AgressoStaffException($"Multiple roles {rr} for for user {u.UserId}");

                            if (roleCount == 1)
                                foreach (var r in userRoles.Single(r => r.RoleId == rr).Clients)
                                {
                                    r.Status = Agresso.Users.Status.Active;
                                }

                            if (roleCount == 0)
                                userRoles.Add(new Agresso.Users.RoleReference()
                                {
                                    RoleId = rr,
                                    Clients = new Agresso.Users.ClientReference[] {
                                        new Agresso.Users.ClientReference() {
                                            ClientId = "RU", Status = Agresso.Users.Status.Active } }
                                });

                        }

                        foreach (var r in userRoles.Where(r => r.Clients.Count(c => c.Status == Agresso.Users.Status.Terminated) > 0))
                        {
                            foreach (var c in r.Clients)
                            {
                                c.Status = Agresso.Users.Status.Parked;
                            }
                        }
                        u.Roles = userRoles.Distinct().ToArray();



                        var userClients = u.Clients.ToList();
                        int ruCount = userClients.Count(c => c.ClientId == "RU");
                        if (ruCount > 1)
                            throw new AgressoStaffException($"Multiple RU clients fround for user {u.UserId}");

                        if (ruCount == 1)
                            userClients.Single(r => r.ClientId == "RU").Status = Agresso.Users.Status.Active;

                        if (ruCount == 0)
                            userClients.Add(new Agresso.Users.ClientReference() { ClientId = "RU", Status = Agresso.Users.Status.Active });

                        foreach (var c in userClients.Where(c => c.Status == Agresso.Users.Status.Terminated))
                        {
                            c.Status = Agresso.Users.Status.Parked;
                        }

                        u.Clients = userClients.Distinct().ToArray();



                        var userTypes = u.UserTypes.ToList();
                        var utCount = userTypes.Count(ut => ut.ClientId == "RU" && ut.MasterFile == Agresso.Users.MasterFileType.Resource);
                        if (utCount > 1)
                            throw new AgressoStaffException($"Multiple userTypes found for {u.UserId}");

                        if (utCount == 1)
                            userTypes.Single(ut => ut.ClientId == "RU" && ut.MasterFile == Agresso.Users.MasterFileType.Resource).MasterFileValue = e.personal_reference;

                        if (utCount == 0)
                            userTypes.Add(new Agresso.Users.UserType()
                            {
                                ClientId = "RU",
                                MasterFile = Agresso.Users.MasterFileType.Resource,
                                MasterFileValue = e.personal_reference
                            });

                        u.UserTypes = userTypes.Distinct().ToArray();

                        if (u.UserId == primary.UserId)
                        {
                            u.ValidTo = e.uni_leave_date.GetValueOrDefault(Helpers.statics.dateToMax);
                            u.Status = Agresso.Users.Status.Active;
                            u.DomainUser = $"rdg-home\\{e.username}";
                            u.IdentityServicesInfo = e.upn;
                        }
                        else
                        {
                            u.ValidTo = DateTime.Today;
                            u.Status = Agresso.Users.Status.Parked;
                            u.UserTypes = new Agresso.Users.UserType[] { new Agresso.Users.UserType() {
                                ClientId = "RU",
                                MasterFile = Agresso.Users.MasterFileType.Resource,
                                MasterFileValue = "999999"
                            } };
                        }

                        u.Description = $"{e.prefered_name} {e.known_as_surname}";
                        u.DefaultClient = "RU";
                        u.ValidFrom = e.uni_start_date;

                        int requestCommits = 0;
                        bool requestCommitted = false;

                        while (requestCommits < 5 && !requestCommitted)
                        {
                            var uResp = await uClient.ModifyUsersAsync(new Agresso.Users.User[] { u }, true, _wscreds.uCreds);
                            var uResult = uResp.Body.ModifyUsersResult;

                            if (uResult.RequestCommitted != true)
                            {
                                if (uResult.Items.Any(e => e.Message.Any(m => m.StartsWith("Field 'Printer' has an invalid value"))))
                                {
                                    u.Printer = "DEFAULT";
                                }
                                else if (uResult.Items.Any(e => e.Message.Any(m => m.StartsWith("'domain_info' - Value already used for"))))
                                {
                                    var i = uResult.Items.Single();
                                    var m = i.Message.Single(m => m.StartsWith("'domain_info' - Value already used for"));
                                    var re = m.Split("'", StringSplitOptions.RemoveEmptyEntries);
                                    var dupUsername = re.LastOrDefault();

                                    var dupUserResult = await uClient.GetUserAsync(dupUsername, _wscreds.uCreds);
                                    dupUserResult.Body.GetUserResult.DomainUser = String.Empty;
                                    dupUserResult.Body.GetUserResult.IdentityServicesInfo = String.Empty;

                                    var dupUserResp = await uClient.ModifyUsersAsync(new Agresso.Users.User[] { dupUserResult.Body.GetUserResult }, true, _wscreds.uCreds);
                                    if (dupUserResp.Body.ModifyUsersResult.RequestCommitted != true)
                                        throw new AgressoStaffException($"Error updating old account: {dupUserResp.Body.ModifyUsersResult.Items.Single().Message.First()}");
                                }
                                else
                                {
                                    throw new AgressoStaffException($"Unhandled excution path: {uResult.Items.Single().Message.First()}");
                                }
                                requestCommits++;
                            }
                            else
                            {
                                var updatedU = await uClient.GetUserAsync(u.UserId, _wscreds.uCreds);
                                if (updatedU.Body.GetUserResult.Status != u.Status)
                                    throw new AgressoStaffException($"User updated but status is still {updatedU.Body.GetUserResult.Status} and not {u.Status}");

                                requestCommitted = true;
                            }
                        }

                        if (!requestCommitted)
                        {
                            throw new AgressoStaffException($"User update failed after multiple attempts {u.UserId}");
                        }

                    }
                }
            }
        }
    }
}