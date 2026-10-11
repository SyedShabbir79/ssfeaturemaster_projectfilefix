using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FunctionsV2.Sys_StudentApp.Utilities;

internal static class HttpJsonResult
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static IActionResult Create(int statusCode, object body)
    {
        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/json; charset=utf-8",
            Content = JsonSerializer.Serialize(body, JsonOptions)
        };
    }
}
