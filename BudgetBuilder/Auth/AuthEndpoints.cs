using BudgetBuilder.Auth.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;
using O9d.AspNet.FluentValidation;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace BudgetBuilder.Auth
{
    public static class AuthEndpoints
    {
        public static void AddAuthApi(this WebApplication app)
        {
            //register
            app.MapPost("api/v1/register", async (UserManager<BudgetRestUser> userManager, RegisterUserDto registerUserDto) =>
            {
                var user = await userManager.FindByNameAsync(registerUserDto.Username);
                if (user != null)
                {
                    return Results.UnprocessableEntity("Username already taken");
                }
                var newUser = new BudgetRestUser
                {
                    Email = registerUserDto.Email,
                    UserName = registerUserDto.Username
                };

                var createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);
                if(!createUserResult.Succeeded)
                {
                    return Results.UnprocessableEntity();
                }
                await userManager.AddToRoleAsync(newUser, BudgetRoles.BudgetUser);

                return Results.Created("api/v1/login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));
            }); 

            app.MapPost("api/v1/registerManager", async (UserManager<BudgetRestUser> userManager, RegisterUserDto registerUserDto) =>
            {
                var user = await userManager.FindByNameAsync(registerUserDto.Username);
                if (user != null)
                {
                    return Results.UnprocessableEntity("Username already taken");
                }
                var newUser = new BudgetRestUser
                {
                    Email = registerUserDto.Email,
                    UserName = registerUserDto.Username
                };

                var createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);
                if(!createUserResult.Succeeded)
                {
                    return Results.UnprocessableEntity();
                }
                await userManager.AddToRoleAsync(newUser, BudgetRoles.BudgetUser);
                await userManager.AddToRoleAsync(newUser, BudgetRoles.CompanyManager);

                return Results.Created("api/v1/login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));
            });

            app.MapPut("api/v1/supervise/{username}", [Authorize(Roles = BudgetRoles.Admin)] async (UserManager<BudgetRestUser> userManager, string username,[Validate] SupervisorDto updateUserDto, HttpContext httpContext) =>
            {
                var user = await userManager.FindByNameAsync(username);
                var supervisor = await userManager.FindByNameAsync(updateUserDto.supervisor);
                if (user == null || supervisor == null)
                {
                    return Results.UnprocessableEntity("User or supervisor does not exist");
                }
                if(user.SupervisorId != null)
                {
                    return Results.UnprocessableEntity("User already has a supervisor");                    
                }
                user.SupervisorId = supervisor.Id;
                await userManager.UpdateAsync(user);
                return Results.Ok();
            });

            //login
            app.MapPost("api/v1/login", async (UserManager<BudgetRestUser> userManager,JwtTokenService jwtTokenService, LoginUserDto loginUserDto) =>
            {
                var user = await userManager.FindByNameAsync(loginUserDto.Username);
                if (user == null)
                {
                    return Results.UnprocessableEntity("Username or password was incorrect");
                }

                var isPasswordValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);
                if(!isPasswordValid)
                {
                    return Results.UnprocessableEntity("Username or password was incorrect");
                }

                user.forceRelogin = false;
                await userManager.UpdateAsync(user);

                var roles = await userManager.GetRolesAsync(user);

                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken,refreshToken));
            });

            app.MapPost("api/v1/logout", async (UserManager<BudgetRestUser> userManager, JwtTokenService jwtTokenService, HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var user = await userManager.FindByIdAsync(userId);
                user.forceRelogin = true;
                if(user == null)
                {
                    return Results.UnprocessableEntity();
                }
                await userManager.UpdateAsync(user);
                return Results.Ok();
            });

            //accessToken
            app.MapPost("api/v1/accessToken", async (UserManager<BudgetRestUser> userManager, JwtTokenService jwtTokenService, RefreshAccessTokenDto refreshAccessTokenDto) =>
            {
                if(!jwtTokenService.TryParseRefreshToken(refreshAccessTokenDto.RefreshToken, out var claims)) 
                {
                    return Results.UnprocessableEntity();
                }

                var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var user = await userManager.FindByIdAsync(userId);
                if(user == null)
                {
                    return Results.UnprocessableEntity("Invalid token");
                }

                if(user.forceRelogin) 
                {
                    return Results.UnprocessableEntity();
                }

                var roles = await userManager.GetRolesAsync(user);

                var accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                var refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));

            });

        }
    }
}

public record UserDto (string UserId, string UserName, string Email);
public record RegisterUserDto(string Username, string Email, string Password);
public record LoginUserDto(string Username, string Password);
public record SuccessfulLoginDto(string AccessToken, string RefreshToken);
public record RefreshAccessTokenDto(string RefreshToken);
public record SupervisorDto(string supervisor);
