namespace BudgetBuilder.Domain.Auth
{
    public record UserDto(string UserId, string UserName, string Email);
    public record RegisterUserDto(string Username, string Email, string Password);
    public record LoginUserDto(string Username, string Password);
    public record SuccessfulLoginDto(string AccessToken, string RefreshToken);
    public record RefreshAccessTokenDto(string RefreshToken);
    public record SupervisorDto(string Supervisor);
}
