using System.ComponentModel.DataAnnotations;
using FollowUp.Core.DTOs.Auth;
using Xunit;

namespace FollowUp.Tests.Validation;

/// <summary>
/// Unit tests for input validation using Data Annotations
/// Ensures all DTOs properly validate user input
/// </summary>
public class InputValidationTests
{
    #region Helper Methods

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, validationResults, true);
        return validationResults;
    }

    private static bool IsValid(object model)
    {
        return ValidateModel(model).Count == 0;
    }

    #endregion

    #region LoginRequest Validation

    [Fact]
    public void LoginRequest_ValidData_PassesValidation()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "SecurePass123!"
        };

        // Act & Assert
        Assert.True(IsValid(request));
    }

    [Fact]
    public void LoginRequest_EmptyUsername_FailsValidation()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "",
            Password = "SecurePass123!"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void LoginRequest_EmptyPassword_FailsValidation()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = ""
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
    }

    #endregion

    #region RegisterRequest Validation

    [Fact]
    public void RegisterRequest_ValidData_PassesValidation()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Password = "SecurePass123!",
            FullName = "Test User",
            PhoneNumber = "0599123456",
            Role = Core.Enums.UserRole.Worker
        };

        // Act & Assert
        Assert.True(IsValid(request));
    }

    [Fact]
    public void RegisterRequest_ShortUsername_FailsValidation()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "ab", // Too short (< 3)
            Password = "SecurePass123!",
            FullName = "Test User",
            PhoneNumber = "0599123456",
            Role = Core.Enums.UserRole.Worker
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
    }

    #endregion

    #region VerifyOtpRequest Validation

    [Fact]
    public void VerifyOtpRequest_ValidData_PassesValidation()
    {
        // Arrange
        var request = new VerifyOtpRequest
        {
            SessionToken = "abc123xyz",
            OtpCode = "123456"
        };

        // Act & Assert
        Assert.True(IsValid(request));
    }

    [Fact]
    public void VerifyOtpRequest_ShortOtpCode_FailsValidation()
    {
        // Arrange
        var request = new VerifyOtpRequest
        {
            SessionToken = "abc123xyz",
            OtpCode = "12345" // Too short (5 digits instead of 6)
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void VerifyOtpRequest_LongOtpCode_FailsValidation()
    {
        // Arrange
        var request = new VerifyOtpRequest
        {
            SessionToken = "abc123xyz",
            OtpCode = "1234567" // Too long (7 digits instead of 6)
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void VerifyOtpRequest_EmptySessionToken_FailsValidation()
    {
        // Arrange
        var request = new VerifyOtpRequest
        {
            SessionToken = "",
            OtpCode = "123456"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
    }

    #endregion
}
