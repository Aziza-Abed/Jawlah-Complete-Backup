using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using Xunit;

namespace FollowUp.Tests.Models;

/// <summary>
/// Unit tests for Entity models
/// Tests entity initialization, property defaults, and relationships
/// </summary>
public class EntityTests
{
    #region User Entity Tests

    [Fact]
    public void User_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void User_SetProperties_PropertiesAreSet()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "testuser",
            FullName = "Test User",
            Email = "test@example.com",
            PhoneNumber = "0599123456",
            Role = UserRole.Worker,
            Status = UserStatus.Active
        };

        // Assert
        Assert.Equal(1, user.UserId);
        Assert.Equal("testuser", user.Username);
        Assert.Equal("Test User", user.FullName);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("0599123456", user.PhoneNumber);
        Assert.Equal(UserRole.Worker, user.Role);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    #endregion

    #region TwoFactorCode Entity Tests

    [Fact]
    public void TwoFactorCode_NewInstance_HasCorrectDefaults()
    {
        // Arrange & Act
        var code = new TwoFactorCode();

        // Assert
        Assert.False(code.IsUsed);
        Assert.Equal(0, code.FailedAttempts);
        Assert.Equal("Login", code.Purpose);
    }

    [Fact]
    public void TwoFactorCode_SetExpiry_ExpiryIsSet()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        var code = new TwoFactorCode
        {
            UserId = 1,
            CodeHash = "hashedcode",
            ExpiresAt = expiresAt,
            PhoneNumber = "0599123456"
        };

        // Assert
        Assert.Equal(expiresAt, code.ExpiresAt);
        Assert.Equal("hashedcode", code.CodeHash);
    }

    [Fact]
    public void TwoFactorCode_IsExpired_ReturnsCorrectly()
    {
        // Arrange
        var expiredCode = new TwoFactorCode
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // Already expired
        };

        var validCode = new TwoFactorCode
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(5) // Still valid
        };

        // Act & Assert
        Assert.True(expiredCode.ExpiresAt < DateTime.UtcNow, "Expired code should be in the past");
        Assert.True(validCode.ExpiresAt > DateTime.UtcNow, "Valid code should be in the future");
    }

    #endregion

    #region Zone Entity Tests

    [Fact]
    public void Zone_SetIsActive_PropertyIsSet()
    {
        // Arrange & Act
        var zone = new Zone
        {
            ZoneId = 1,
            ZoneName = "Test Zone",
            IsActive = true
        };

        // Assert
        Assert.True(zone.IsActive);
        Assert.Equal("Test Zone", zone.ZoneName);
    }

    #endregion

    #region Enum Value Tests

    [Theory]
    [InlineData(UserRole.Admin, "Admin")]
    [InlineData(UserRole.Supervisor, "Supervisor")]
    [InlineData(UserRole.Worker, "Worker")]
    public void UserRole_EnumValues_AreCorrect(UserRole role, string expectedName)
    {
        // Assert
        Assert.Equal(expectedName, role.ToString());
    }

    [Theory]
    [InlineData(TaskPriority.Low, 0)]
    [InlineData(TaskPriority.Medium, 1)]
    [InlineData(TaskPriority.High, 2)]
    [InlineData(TaskPriority.Urgent, 3)]
    public void TaskPriority_EnumValues_HaveCorrectOrder(TaskPriority priority, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)priority);
    }

    [Theory]
    [InlineData(TaskType.GarbageCollection, 0)]
    [InlineData(TaskType.StreetSweeping, 1)]
    [InlineData(TaskType.ContainerMaintenance, 2)]
    [InlineData(TaskType.Other, 99)]
    public void TaskType_EnumValues_AreCorrect(TaskType taskType, int expectedValue)
    {
        // Assert
        Assert.Equal(expectedValue, (int)taskType);
    }

    #endregion
}
