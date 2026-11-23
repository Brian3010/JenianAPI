namespace Jenian.Application.Common
{
  public class ServiceResult<T>
  {
    public bool IsSuccess { get; set; }

    public T? Data { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = [];

    public ServiceFailureKind? FailureKind { get; set; }

    public static ServiceResult<T> Success(T? data) {

      return new ServiceResult<T> {
        IsSuccess = true,
        Data = data,
        Errors = []
      };
    }

    public static ServiceResult<T> Failure(IReadOnlyList<string> errors, ServiceFailureKind? failureKind = null) {
      return new ServiceResult<T> {
        IsSuccess = false,
        Errors = errors,
        FailureKind = failureKind
      };
    }

  }
}
