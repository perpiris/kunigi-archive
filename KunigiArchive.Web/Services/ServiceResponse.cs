using KunigiArchive.Web.Enums;

namespace KunigiArchive.Web.Services;

public class ServiceResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public ResponseSeverity Severity { get; set; }

    public static ServiceResponse Successful(string? message = null)
    {
        return new ServiceResponse { Success = true, Message = message, Severity = ResponseSeverity.Success };
    }

    public static ServiceResponse Failure(string? message = null)
    {
        return new ServiceResponse { Success = false, Message = message, Severity = ResponseSeverity.Error };
    }

    public static ServiceResponse Warning(string? message = null)
    {
        return new ServiceResponse { Success = true, Message = message, Severity = ResponseSeverity.Warning };
    }
}

public class ServiceResponse<T> : ServiceResponse
{
    public T? Data { get; set; }

    public static ServiceResponse<T> Successful(T? data, string? message = null)
    {
        return new ServiceResponse<T>
        {
            Success = true, Data = data, Message = message, Severity = ResponseSeverity.Success
        };
    }

    public static new ServiceResponse<T> Failure(string? message = null)
    {
        return new ServiceResponse<T>
        {
            Success = false, Data = default, Message = message, Severity = ResponseSeverity.Error
        };
    }

    public static new ServiceResponse<T> Warning(string? message = null)
    {
        return new ServiceResponse<T>
        {
            Success = true, Data = default, Message = message, Severity = ResponseSeverity.Warning
        };
    }
}