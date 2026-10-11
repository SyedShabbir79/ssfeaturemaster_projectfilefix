using System;

/// <summary>
/// Jean Guo - 18/08/2023
/// Helper Method to calculate location based on location code
/// </summary>
/// 


namespace FunctionsV2.Sys_HRMS
{
    public static class HRMS_Location
    {

        public class StaffLocation
        {
            public string location { get; set; }
            public string error { get; set; }
            public bool hasError { get; set; } = false;
            public StaffLocation(string locCode)
            {
                // check for missing or invalid params
                if (String.IsNullOrEmpty(locCode)) { error = "Missing location code"; hasError = true; location = null; }

                if (!hasError)
                {
                    switch (locCode.Substring(0, 1))
                    {
                        case "W" or "w":
                            location = "Whiteknights"; break;
                        case "G" or "g":
                            location = "Greenlands"; break;
                        case "L" or "l":
                            location = "London Road"; break;
                        case "M" or "m":
                            location = "Malaysia"; break;
                        case "S" or "s":
                            location = "South Africa"; break;
                        case "D" or "d":
                            location = "Germany"; break;
                        case "F" or "f":
                            location = "Finland"; break;
                        case "K" or "k":
                            location = "Other"; break;
                        case "T" or "t":
                            location = "Thames Valley Science Park"; break;
                        default:
                            location = null; break;
                    }
                }
            }
        }
    }
}
