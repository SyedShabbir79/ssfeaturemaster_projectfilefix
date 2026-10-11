using FunctionsV2.Sys_StudentApp.Models;
using FunctionsV2.Sys_StudentApp.Options;
using FunctionsV2.Sys_StudentApp.Services;
using FunctionsV2.Sys_StudentApp.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_StudentApp.Functions;

public sealed class GetStudentFunction
{
    private const int MaxUpnLength = 320;

    private readonly IStudentRepository _studentRepository;
    private readonly IStudentResponseMapper _mapper;
    private readonly StudentAppOptions _options;
    private readonly ILogger<GetStudentFunction> _logger;

    public GetStudentFunction(
        IStudentRepository studentRepository,
        IStudentResponseMapper mapper,
        IOptions<StudentAppOptions> options,
        ILogger<GetStudentFunction> logger)
    {
        _studentRepository = studentRepository;
        _mapper = mapper;
        _options = options.Value;
        _logger = logger;
    }

    [FunctionName("Sys_StudentApp_GetStudent")]
    [OpenApiOperation]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(StudentAppResponse))]
    [OpenApiParameter("user", In = Microsoft.OpenApi.Models.ParameterLocation.Query, Required = true, Type = typeof(string))]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? request.HttpContext.TraceIdentifier;
        var upn = GetQueryValue(request, "user")?.Trim();

        if (!IsValidUserParameter(upn))
        {
            return HttpJsonResult.Create(
                StatusCodes.Status400BadRequest,
                new ProblemResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Missing or invalid user query parameter",
                    Detail = "Call the endpoint using ?user=<student-upn>.",
                    TraceId = traceId
                });
        }

        _logger.LogInformation("Student lookup request received for UPN {StudentUpn}. TraceId {TraceId}", upn, traceId);

        try
        {
            var result = await _studentRepository.GetStudentAsync(upn!, cancellationToken).ConfigureAwait(false);

            if (result.HasStoredProcedureError)
            {
                _logger.LogError(
                    "Stored procedure returned an error for UPN {StudentUpn}. ErrorNumber {ErrorNumber}. ErrorState {ErrorState}. ErrorMessage {ErrorMessage}. TraceId {TraceId}",
                    upn,
                    result.Error?.ErrorNumber,
                    result.Error?.ErrorState,
                    result.Error?.ErrorMessage,
                    traceId);

                return HttpJsonResult.Create(
                    StatusCodes.Status502BadGateway,
                    new ProblemResponse
                    {
                        Status = StatusCodes.Status502BadGateway,
                        Title = "Student lookup failed",
                        Detail = "The student data source returned an error.",
                        TraceId = traceId
                    });
            }

            if (!result.Found)
            {
                _logger.LogInformation("No student record found for UPN {StudentUpn}. TraceId {TraceId}", upn, traceId);

                if (_options.Return404WhenStudentNotFound)
                {
                    return HttpJsonResult.Create(
                        StatusCodes.Status404NotFound,
                        new ProblemResponse
                        {
                            Status = StatusCodes.Status404NotFound,
                            Title = "Student not found",
                            Detail = "No student details were returned for the supplied UPN.",
                            TraceId = traceId
                        });
                }

                return HttpJsonResult.Create(StatusCodes.Status200OK, StudentAppResponse.Empty());
            }

            var response = _mapper.Map(result.Student!);
            return HttpJsonResult.Create(StatusCodes.Status200OK, response);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error while retrieving student details for UPN {StudentUpn}. TraceId {TraceId}", upn, traceId);

            return HttpJsonResult.Create(
                StatusCodes.Status502BadGateway,
                new ProblemResponse
                {
                    Status = StatusCodes.Status502BadGateway,
                    Title = "Student data source unavailable",
                    Detail = "The student data source could not be queried successfully.",
                    TraceId = traceId
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Request was cancelled by the caller for UPN {StudentUpn}. TraceId {TraceId}", upn, traceId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving student details for UPN {StudentUpn}. TraceId {TraceId}", upn, traceId);

            return HttpJsonResult.Create(
                StatusCodes.Status500InternalServerError,
                new ProblemResponse
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Unexpected error",
                    Detail = "An unexpected error occurred while processing the student lookup request.",
                    TraceId = traceId
                });
        }
    }

    private static string? GetQueryValue(HttpRequest request, string key)
    {
        return request.Query.TryGetValue(key, out StringValues values)
            ? values.FirstOrDefault()
            : null;
    }

    private static bool IsValidUserParameter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxUpnLength)
        {
            return false;
        }

        return !value.Contains('\r') && !value.Contains('\n');
    }
}
