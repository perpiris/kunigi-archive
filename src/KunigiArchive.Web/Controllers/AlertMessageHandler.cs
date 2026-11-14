using KunigiArchive.Web.Enums;
using KunigiArchive.Web.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace KunigiArchive.Web.Controllers;

public static class AlertMessageHandler
{
    private const string SuccessKey = "Success";
    private const string WarningKey = "Warning";
    private const string ErrorKey = "Error";

    public static void SetMessage(ITempDataDictionary tempData, ServiceResponse response,
        string? defaultErrorMessage = null)
    {
        string? message = response.Message ?? (response.Severity == ResponseSeverity.Error ? defaultErrorMessage : null);

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        switch (response.Severity)
        {
            case ResponseSeverity.Success:
                tempData[SuccessKey] = message;
                break;
            case ResponseSeverity.Warning:
                tempData[WarningKey] = message;
                break;
            case ResponseSeverity.Error:
                tempData[ErrorKey] = message;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void SetMessage(ITempDataDictionary tempData, string message, ResponseSeverity severity)
    {
        switch (severity)
        {
            case ResponseSeverity.Success:
                tempData[SuccessKey] = message;
                break;
            case ResponseSeverity.Warning:
                tempData[WarningKey] = message;
                break;
            case ResponseSeverity.Error:
                tempData[ErrorKey] = message;
                break;
        }
    }
}