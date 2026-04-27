using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jenian.Application.Common
{
  public class ServiceResult<T>
  {
    public bool IsSuccess { get; set; }

    public T? Data { get; set; }

    public IReadOnlyList<string> Errors { get; set; } = [];

    public static ServiceResult<T> Success(T? data) {

      return new ServiceResult<T> {
        IsSuccess = true,
        Data = data,
        Errors = []
      };
    }

    public static ServiceResult<T> Failure(IReadOnlyList<string> errors) {
      return new ServiceResult<T> {
        IsSuccess = false,
        Errors = errors
      };
    }

  }
}
