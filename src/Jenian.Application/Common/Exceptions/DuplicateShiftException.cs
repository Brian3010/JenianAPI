namespace Jenian.Application.Common.Exceptions
{
  public class DuplicateShiftException : AppException
  {

    public DuplicateShiftException() : base(
      message: "A shift with the same start and end time already exists for this user.",
      statusCode: 409,
      errorCode: "DUPLICATE_SHIFT"
      ) {
    }
  }
}
