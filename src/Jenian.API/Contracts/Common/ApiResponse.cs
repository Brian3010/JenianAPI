namespace Jenian.API.Contracts.Common
{
  public sealed record ApiResponse<T>(bool Success, T? Data, IReadOnlyList<string> Errors)
  {
    public static ApiResponse<T> Ok(T? data) {
      return new ApiResponse<T>(true, data, []);
    }

    public static ApiResponse<T> Fail(IReadOnlyList<string> errors) {
      return new ApiResponse<T>(false, Data: default, errors);
    }
  }

}

