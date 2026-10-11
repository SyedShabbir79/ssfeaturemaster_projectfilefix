using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;
using FunctionsV2.Helpers;
using System.Threading.Tasks;
using System.Security;
using System.Net;

namespace FunctionV2.Sys_Staff_Agresso_v2.Helpers
{
    public class WSCreds
    {
        private readonly NetworkCredential agressoAccount;

        public WSCreds(string company, string username, string password)
        {
            if (String.IsNullOrWhiteSpace(username))
                throw new Exception("Missing Agresso Username");

            if (String.IsNullOrWhiteSpace(password))
                throw new Exception("Missing Agresso Password from KV");

            if (String.IsNullOrWhiteSpace(company))
                throw new Exception("Missing Agresso Company from Employee");

            this.agressoAccount = new NetworkCredential(username, password);
            this.agressoAccount.Domain = company;
        }


        public Agresso.Personnel.WSCredentials pCreds
        {
            get
            {
                return new Agresso.Personnel.WSCredentials()
                {
                    Client = agressoAccount.Domain,
                    Username = agressoAccount.UserName,
                    Password = agressoAccount.Password
                };
            }

        }

        public Agresso.Supplier.WSCredentials sCreds
        {
            get
            {
                return new Agresso.Supplier.WSCredentials()
                {
                    Client = agressoAccount.Domain,
                    Username = agressoAccount.UserName,
                    Password = agressoAccount.Password
                };
            }

        }

        public Agresso.Users.WSCredentials uCreds
        {
            get
            {
                return new Agresso.Users.WSCredentials()
                {
                    Client = agressoAccount.Domain,
                    Username = agressoAccount.UserName,
                    Password = agressoAccount.Password
                };
            }
        }

        public Agresso.QueryEngine.WSCredentials qeCredsForcedClient(string forcedClient)
        {
            return new Agresso.QueryEngine.WSCredentials()
            {
                Client = forcedClient,
                Username = agressoAccount.UserName,
                Password = agressoAccount.Password
            };
        }



        public Agresso.QueryEngine.WSCredentials qeCreds
        {
            get
            {
                return new Agresso.QueryEngine.WSCredentials()
                {
                    Client = agressoAccount.Domain,
                    Username = agressoAccount.UserName,
                    Password = agressoAccount.Password
                };

            }
        }
    }

    public class statics
    {
        public static DateTime dateToMax = new System.DateTime(2099, 7, 31);

        public static int GetPeriodFrom(DateTime inDate)
        {
            var year = inDate.Year;
            var month = inDate.Month;

            if (inDate.Month >= 8)
                year = inDate.AddYears(1).Year;

            if (inDate.Month <= 7)
                month = inDate.Month + 5;

            else
                month = inDate.Month - 7;

            return int.Parse(year.ToString() + month.ToString("D2"));
        }

        public static int GetPeriodTo(DateTime? inDate)
        {
            if (inDate.HasValue)
                return GetPeriodFrom(inDate.Value);

            return 209912;
        }
    }
}