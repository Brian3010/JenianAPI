namespace JenianAPI.Helpers
{
  public static class IpHelper
  {
    public static string? GetClientIp(HttpContext httpContext) {
      if (httpContext == null)
        return null;

      // Try to get IP from reverse proxy headers
      var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
      if (!string.IsNullOrEmpty(forwardedFor)) {
        // Can contain multiple IPs, return the first
        return forwardedFor.Split(',').First().Trim();
      }

      // Fallback to direct remote IP
      return httpContext.Connection.RemoteIpAddress?.ToString();
    }
  }
}
