using Jenian.Application.Common;

namespace Jenian.Application.Abstractions.Auth
{
  public class AuthTransactionResult<T>
  {

    public required ServiceResult<T> Result { get; init; }

    public required AuthTransactionDisposition Disposition { get; init; }
  }
}
