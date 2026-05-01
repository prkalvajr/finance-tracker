using FinanceTracker.Application.DTOs.Users;
using FinanceTracker.Application.Exceptions;
using FinanceTracker.Application.Services;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace FinanceTracker.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();

    private UserService CreateService() => new(_userRepoMock.Object, _hasherMock.Object);

    [Fact]
    public async Task GetCurrentUser_WithValidUserId_ReturnsUserDto()
    {
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "x" };
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateService().GetCurrentUserAsync(user.UserId);

        result.Should().Be(new UserDto(user.UserId, user.Name, user.Email));
    }

    [Fact]
    public async Task GetCurrentUser_WithUnknownUserId_ThrowsNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateService().GetCurrentUserAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUser_WithNewName_UpdatesNameOnly()
    {
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "stored" };
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var request = new UpdateUserRequest(Name: "Alice Smith", Email: null, CurrentPassword: null, NewPassword: null);

        var result = await CreateService().UpdateUserAsync(user.UserId, request);

        result.Should().Be(new UserDto(user.UserId, "Alice Smith", "alice@example.com"));
        _userRepoMock.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.UserId == 7 && u.Name == "Alice Smith" && u.Email == "alice@example.com" && u.PasswordHash == "stored"),
            It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _hasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WithNewEmail_UpdatesEmailOnly()
    {
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "stored" };
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetByEmailAsync("new@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var request = new UpdateUserRequest(Name: null, Email: "new@example.com", CurrentPassword: null, NewPassword: null);

        var result = await CreateService().UpdateUserAsync(user.UserId, request);

        result.Should().Be(new UserDto(user.UserId, "Alice", "new@example.com"));
        _userRepoMock.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.UserId == 7 && u.Name == "Alice" && u.Email == "new@example.com" && u.PasswordHash == "stored"),
            It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByEmailAsync("new@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _hasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WithDuplicateEmail_ThrowsConflictException()
    {
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "stored" };
        var collidingUser = new User { UserId = 8, Name = "Bob", Email = "taken@example.com", PasswordHash = "y" };
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetByEmailAsync("taken@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(collidingUser);
        var request = new UpdateUserRequest(Name: null, Email: "taken@example.com", CurrentPassword: null, NewPassword: null);

        var act = () => CreateService().UpdateUserAsync(user.UserId, request);

        await act.Should().ThrowAsync<ConflictException>();
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WithNewPassword_VerifiesCurrentPasswordAndUpdates()
    {
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "old-hash" };
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("oldpass", "old-hash")).Returns(true);
        _hasherMock.Setup(h => h.Hash("newpass")).Returns("new-hash");
        var request = new UpdateUserRequest(Name: null, Email: null, CurrentPassword: "oldpass", NewPassword: "newpass");

        var result = await CreateService().UpdateUserAsync(user.UserId, request);

        result.Should().Be(new UserDto(user.UserId, "Alice", "alice@example.com"));
        _hasherMock.Verify(h => h.Verify("oldpass", "old-hash"), Times.Once);
        _hasherMock.Verify(h => h.Hash("newpass"), Times.Once);
        _userRepoMock.Verify(r => r.UpdateAsync(
            It.Is<User>(u => u.PasswordHash == "new-hash"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WithWrongCurrentPassword_ThrowsUnauthorizedException()
    {
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "old-hash" };
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("wrong", "old-hash")).Returns(false);
        var request = new UpdateUserRequest(Name: null, Email: null, CurrentPassword: "wrong", NewPassword: "newpass");

        var act = () => CreateService().UpdateUserAsync(user.UserId, request);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _hasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateUser_WithNewPasswordButNoCurrentPassword_ThrowsValidationException(string? currentPassword)
    {
        var user = new User { UserId = 7, Name = "Alice", Email = "alice@example.com", PasswordHash = "old-hash" };
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var request = new UpdateUserRequest(Name: null, Email: null, CurrentPassword: currentPassword, NewPassword: "newpass");

        var act = () => CreateService().UpdateUserAsync(user.UserId, request);

        await act.Should().ThrowAsync<ValidationException>();
        _hasherMock.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _hasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
