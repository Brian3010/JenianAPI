using Jenian.Application.Common;

namespace Jenian.Tests.Application.Common
{
  public class ServiceResultTests
  {
    [Fact]
    public void Success_WithData_PreservesDataAndHasNoErrors() {
      var data = new object();

      var result = ServiceResult<object>.Success(data);

      Assert.True(result.IsSuccess);
      Assert.Same(data, result.Data);
      Assert.Empty(result.Errors);
      Assert.Null(result.FailureKind);
    }

    [Fact]
    public void Failure_WithoutClassification_PreservesErrorsAndHasNoData() {
      string[] errors = ["First error.", "Second error."];

      var result = ServiceResult<object>.Failure(errors);

      Assert.False(result.IsSuccess);
      Assert.Null(result.Data);
      Assert.Equal(errors, result.Errors);
      Assert.Null(result.FailureKind);
    }

    public static TheoryData<ServiceFailureKind> FailureKinds => new()
    {
      ServiceFailureKind.CredentialRejected,
      ServiceFailureKind.DependencyUnavailable,
      ServiceFailureKind.UnexpectedFailure
    };

    [Theory]
    [MemberData(nameof(FailureKinds))]
    public void Failure_WithClassification_PreservesClassificationAndErrors(ServiceFailureKind failureKind) {
      string[] errors = ["Operation failed."];

      var result = ServiceResult<object>.Failure(errors, failureKind);

      Assert.False(result.IsSuccess);
      Assert.Null(result.Data);
      Assert.Equal(errors, result.Errors);
      Assert.Equal<ServiceFailureKind?>(failureKind, result.FailureKind);
    }
  }
}
