namespace JenianAPI.Errors
{
  public sealed class AppException : Exception
  {
    public int StatusCode { get; }
    public string? ErrorCode { get; }


    public AppException(string message, int statusCode = StatusCodes.Status400BadRequest, string? errorCode = null) : base(message) {
      StatusCode = statusCode;
      ErrorCode = errorCode;
    }
  }
}
