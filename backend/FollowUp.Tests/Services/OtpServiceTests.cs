using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Services;
using FollowUp.Infrastructure.Data;
using FollowUp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Resolve Task ambiguity
using Task = System.Threading.Tasks.Task;

namespace FollowUp.Tests.Services;

/// <summary>
/// Unit tests for OtpService - Two-Factor Authentication
/// Tests OTP generation, verification, and policy enforcement
/// </summary>
public class OtpServiceTests
{
    private readonly Mock<ISmsService> _mockSmsService;
    private readonly Mock<ILogger<OtpService>> _mockLogger;
    private readonly FollowUpDbContext _dbContext;
    private readonly OtpService _otpService;

    public OtpServiceTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<FollowUpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new FollowUpDbContext(options);
        _mockSmsService = new Mock<ISmsService>();
        _mockLogger = new Mock<ILogger<OtpService>>();

        _otpService = new OtpService(_dbContext, _mockSmsService.Object, _mockLogger.Object);
    }

    #region RequiresOtp Tests

    [Fact]
    public void RequiresOtp_AdminWithSameDevice_ReturnsFalse()
    {
        // Arrange - New policy: ALL roles use device binding
        var adminUser = new User
        {
            UserId = 1,
            Username = "admin",
            Role = UserRole.Admin,
            RegisteredDeviceId = "device123"
        };

        // Act
        var result = _otpService.RequiresOtp(adminUser, "device123");

        // Assert - Same device = no OTP needed
        Assert.False(result, "Admin with same registered device should NOT require OTP");
    }

    [Fact]
    public void RequiresOtp_AdminWithNewDevice_ReturnsTrue()
    {
        // Arrange
        var adminUser = new User
        {
            UserId = 1,
            Username = "admin",
            Role = UserRole.Admin,
            RegisteredDeviceId = "oldDevice"
        };

        // Act
        var result = _otpService.RequiresOtp(adminUser, "newDevice");

        // Assert - Different device = OTP required
        Assert.True(result, "Admin with new device should require OTP");
    }

    [Fact]
    public void RequiresOtp_SupervisorWithSameDevice_ReturnsFalse()
    {
        // Arrange - New policy: ALL roles use device binding
        var supervisorUser = new User
        {
            UserId = 2,
            Username = "supervisor",
            Role = UserRole.Supervisor,
            RegisteredDeviceId = "device456"
        };

        // Act
        var result = _otpService.RequiresOtp(supervisorUser, "device456");

        // Assert - Same device = no OTP needed
        Assert.False(result, "Supervisor with same registered device should NOT require OTP");
    }

    [Fact]
    public void RequiresOtp_SupervisorWithNoDevice_ReturnsTrue()
    {
        // Arrange
        var supervisorUser = new User
        {
            UserId = 2,
            Username = "supervisor",
            Role = UserRole.Supervisor,
            RegisteredDeviceId = null // First login
        };

        // Act
        var result = _otpService.RequiresOtp(supervisorUser, "newDevice");

        // Assert - First login = OTP required
        Assert.True(result, "Supervisor with no registered device should require OTP");
    }

    [Fact]
    public void RequiresOtp_WorkerWithNoRegisteredDevice_ReturnsTrue()
    {
        // Arrange
        var workerUser = new User
        {
            UserId = 3,
            Username = "worker",
            Role = UserRole.Worker,
            RegisteredDeviceId = null // No device registered yet
        };

        // Act
        var result = _otpService.RequiresOtp(workerUser, "newDevice");

        // Assert
        Assert.True(result, "Workers with no registered device should require OTP for first login");
    }

    [Fact]
    public void RequiresOtp_WorkerWithSameDevice_ReturnsFalse()
    {
        // Arrange
        var workerUser = new User
        {
            UserId = 4,
            Username = "worker",
            Role = UserRole.Worker,
            RegisteredDeviceId = "registeredDevice123"
        };

        // Act
        var result = _otpService.RequiresOtp(workerUser, "registeredDevice123");

        // Assert
        Assert.False(result, "Workers with same registered device should NOT require OTP");
    }

    [Fact]
    public void RequiresOtp_WorkerWithDifferentDevice_ReturnsTrue()
    {
        // Arrange
        var workerUser = new User
        {
            UserId = 5,
            Username = "worker",
            Role = UserRole.Worker,
            RegisteredDeviceId = "oldDevice"
        };

        // Act
        var result = _otpService.RequiresOtp(workerUser, "newDevice");

        // Assert
        Assert.True(result, "Workers with different device should require OTP");
    }

    #endregion

    #region MaskPhoneNumber Tests

    [Theory]
    [InlineData("0599123456", "****3456")]
    [InlineData("+970599123456", "****3456")]
    [InlineData("123", "****")]
    [InlineData("", "****")]
    public void MaskPhoneNumber_ReturnsCorrectMaskedFormat(string phoneNumber, string expected)
    {
        // Act
        var result = _otpService.MaskPhoneNumber(phoneNumber);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region GenerateAndSendOtpAsync Tests

    [Fact]
    public async Task GenerateAndSendOtpAsync_UserWithNoPhone_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "noPhoneUser",
            PhoneNumber = null
        };

        // Act
        var result = await _otpService.GenerateAndSendOtpAsync(user);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_SmsFails_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "testUser",
            PhoneNumber = "0599123456"
        };

        _mockSmsService.Setup(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _otpService.GenerateAndSendOtpAsync(user);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAndSendOtpAsync_Success_ReturnsSessionToken()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "testUser",
            PhoneNumber = "0599123456"
        };

        _mockSmsService.Setup(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _otpService.GenerateAndSendOtpAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    #endregion

    #region VerifyOtpAsync Tests

    [Fact]
    public async Task VerifyOtpAsync_InvalidSessionToken_ReturnsFalse()
    {
        // Act
        var (success, userId, error, remaining) = await _otpService.VerifyOtpAsync("invalidToken", "123456");

        // Assert
        Assert.False(success);
        Assert.Null(userId);
        Assert.NotNull(error);
    }

    #endregion
}
