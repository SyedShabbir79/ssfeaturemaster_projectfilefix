using System;



/// <summary>
/// Duncan Taylor - 13/06/2023
/// StaffPayCategor custom class used by HRMS_Calculate_PayCategory Azure function
/// </summary>
/// 


namespace FunctionsV2.Sys_HRMS
{
    public static class HRMS_PayCategory
    {

        public class StaffPayCategory
        {
            public string error { get; set; }
            public bool hasError { get; set; } = false;
            public string payCategory { get; set; }
            public StaffPayCategory (string group, string subGroup, string paygrade)
            {
                // initialise int version of sub group
                int subGrpInt;
                // check for missing or invalid params
                if (String.IsNullOrEmpty (group)) { error = "Missing employee group"; hasError = true; payCategory = null; }
                if (String.IsNullOrEmpty (subGroup)) { error = "Missing employee subgroup"; hasError = true; payCategory = null; }
                if (!Int32.TryParse(subGroup, out subGrpInt)) { error = "Employee subgroup is not an Integer"; hasError = true; payCategory = null; }

                if (!hasError)
                {
                    switch (group)
                    {
                        case "A":
                            if (String.IsNullOrEmpty(paygrade))
                            {
                                error = "Paygrade argument is required for EG Code A."; hasError = true; payCategory = null;
                            }
                            else if (subGrpInt == 01)
                            {
                                switch (paygrade)
                                {
                                    case "PRz1":
                                        payCategory = "AR-Academic (Professor)"; break;
                                    case "PRz2":
                                        payCategory = "AR-Academic (Professor)"; break;
                                    case "PRz3":
                                        payCategory = "AR-Academic (Professor)"; break;
                                    case "PRz4":
                                        payCategory = "AR-Academic (Professor)"; break;
                                    case "G8":
                                        payCategory = "AR-Academic(Associate Professor)"; break;
                                    case "G7":
                                        payCategory = "AR-Academic (Lecturer)"; break;
                                }
                            }
                            else if (subGrpInt == 02)
                            {
                                switch (paygrade)
                                {
                                    case "PRz1":
                                        payCategory = "AR-Academic (Professor)"; break;
                                    case "PRz2":
                                        payCategory = "AR-Academic (Professor)"; break;
                                    case "PRz3":
                                        payCategory = "AR-Academic (Professor)"; break;
                                    case "PRz4":
                                        payCategory = "AR-Academic (Professor)"; break;
                                    case "G8":
                                        payCategory = "AR-Academic(Associate Professor)"; break;
                                    case "G7":
                                        payCategory = "AR-Academic (Lecturer)"; break;
                                }
                            }
                            break;
                        case "B":
                            if ((subGrpInt >= 10 && subGrpInt <= 12) || (subGrpInt >= 16 && subGrpInt <= 18))
                            {
                                payCategory = "PM-ALC"; break;
                            }
                            else if (subGrpInt >= 13 && subGrpInt <= 15)
                            {
                                payCategory = "AR-Research"; break;
                            }
                            break;
                        case "C":
                            if (subGrpInt >= 20 && subGrpInt <= 22)
                            {
                                payCategory = "AOS-Manual"; break;
                            }
                            else if (subGrpInt >= 23 && subGrpInt <= 25)
                            {
                                payCategory = "AOS-Security";
                            }
                            break;
                        case "D":
                            if (subGrpInt >= 30 && subGrpInt <= 32)
                            {
                                payCategory = "AAS-Clerical"; break;
                            }
                            else if (subGrpInt >= 33 && subGrpInt <= 35)
                            {
                                payCategory = "AAS-Technical"; break;
                            }
                            break;
                        case "E":
                            if (subGrpInt == 40)
                            {
                                payCategory = "KTP Associate"; break;
                            }
                            else if (subGrpInt == 41)
                            {
                                payCategory = "Farm Workers"; break;
                            }
                            else if (subGrpInt >= 44 && subGrpInt <= 45)
                            {
                                payCategory = "Clinical Academic"; break;
                            }
                            else if (subGrpInt == 46)
                            {
                                payCategory = "Casual"; break;
                            }
                            break;
                        case "F":
                            if (subGrpInt == 50)
                            {
                                payCategory = "Campus Jobs Worker"; break;
                            }
                            else if (subGrpInt == 51)
                            {
                                payCategory = "Fees & Claims"; break;
                            }
                            break;
                        case "G":
                            if (subGrpInt == 60)
                            {
                                payCategory = "International Academic"; break;
                            }
                            else if (subGrpInt == 61)
                            {
                                payCategory = "International Admin"; break;
                            }
                            if (subGrpInt == 62)
                            {
                                payCategory = "International Management"; break;
                            }
                            else if (subGrpInt == 63)
                            {
                                payCategory = "International Sessional"; break;
                            }
                            break;
                        case "H":
                            if (subGrpInt >= 70 && subGrpInt <= 73)
                            {
                                payCategory = "Non University Employee"; break;
                            }
                            break;
                        case "I":
                            if (subGrpInt == 80)
                            {
                                payCategory = "Pensioner"; break;
                            }
                            else if (subGrpInt == 81)
                            {
                                payCategory = "Fees & Claims"; break;
                            }
                            if (subGrpInt == 82)
                            {
                                payCategory = "Non University Employee"; break;
                            }
                            break;
                        default:
                            payCategory = null; break;
                    }
                }
            }
        }
    }
}
