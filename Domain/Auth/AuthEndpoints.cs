using BudgetBuilder.Domain.Auth.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using O9d.AspNet.FluentValidation;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BudgetBuilder.Domain.Auth
{
    public static class AuthEndpoints
    {
        public static void AddAuthApi(this WebApplication app)
        {
            //register
            app.MapPost("api/v1/register", async (UserManager<BudgetRestUser> userManager, RegisterUserDto registerUserDto) =>
            {
                BudgetRestUser? user = await userManager.FindByNameAsync(registerUserDto.Username);
                if (user != null)
                {
                    return Results.UnprocessableEntity("Username already taken");
                }
                BudgetRestUser newUser = new()
                {
                    Email = registerUserDto.Email,
                    UserName = registerUserDto.Username
                };

                IdentityResult createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);
                if (!createUserResult.Succeeded)
                {
                    return Results.UnprocessableEntity();
                }
                await userManager.AddToRoleAsync(newUser, BudgetRoles.BudgetUser);

                return Results.Created("api/v1/login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));
            });

            app.MapPost("api/v1/registerManager", async (UserManager<BudgetRestUser> userManager, RegisterUserDto registerUserDto) =>
            {
                BudgetRestUser? user = await userManager.FindByNameAsync(registerUserDto.Username);
                if (user != null)
                {
                    return Results.UnprocessableEntity("Username already taken");
                }
                BudgetRestUser newUser = new()
                {
                    Email = registerUserDto.Email,
                    UserName = registerUserDto.Username
                };

                IdentityResult createUserResult = await userManager.CreateAsync(newUser, registerUserDto.Password);
                if (!createUserResult.Succeeded)
                {
                    return Results.UnprocessableEntity();
                }
                await userManager.AddToRoleAsync(newUser, BudgetRoles.BudgetUser);
                await userManager.AddToRoleAsync(newUser, BudgetRoles.CompanyManager);

                return Results.Created("api/v1/login", new UserDto(newUser.Id, newUser.UserName, newUser.Email));
            });

            app.MapPut("api/v1/supervise/{username}", [Authorize(Roles = BudgetRoles.Admin)] async (UserManager<BudgetRestUser> userManager, string username, [Validate] SupervisorDto updateUserDto, HttpContext httpContext) =>
            {
                BudgetRestUser? user = await userManager.FindByNameAsync(username);
                BudgetRestUser? supervisor = await userManager.FindByNameAsync(updateUserDto.Supervisor);
                if (user == null || supervisor == null)
                {
                    return Results.UnprocessableEntity("User or supervisor does not exist");
                }
                if (user.SupervisorId != null)
                {
                    return Results.UnprocessableEntity("User already has a supervisor");
                }
                user.SupervisorId = supervisor.Id;
                await userManager.UpdateAsync(user);
                return Results.Ok();
            });

            //login
            app.MapPost("api/v1/login", async (UserManager<BudgetRestUser> userManager, JwtTokenService jwtTokenService, LoginUserDto loginUserDto) =>
            {
                BudgetRestUser? user = await userManager.FindByNameAsync(loginUserDto.Username);
                if (user == null)
                {
                    return Results.UnprocessableEntity("Username or password was incorrect");
                }

                bool isPasswordValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);
                if (!isPasswordValid)
                {
                    return Results.UnprocessableEntity("Username or password was incorrect");
                }

                user.ForceRelogin = false;
                await userManager.UpdateAsync(user);

                IList<string> roles = await userManager.GetRolesAsync(user);

                if (user.UserName != null)
                {
                    string accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                    string refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                    return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));
                }
                else
                {
                    return Results.NotFound("Username not found");
                }
            });

            app.MapPost("api/v1/logout", async (UserManager<BudgetRestUser> userManager, JwtTokenService jwtTokenService, HttpContext httpContext) =>
            {
                string? userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (userId == null)
                {
                    return Results.UnprocessableEntity("User id not found");
                }
                BudgetRestUser? user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Results.UnprocessableEntity();
                }
                user.ForceRelogin = true;
                await userManager.UpdateAsync(user);
                return Results.Ok();

            });

            //accessToken
            app.MapPost("api/v1/accessToken", async (UserManager<BudgetRestUser> userManager, JwtTokenService jwtTokenService, RefreshAccessTokenDto refreshAccessTokenDto) =>
            {
                if (!jwtTokenService.TryParseRefreshToken(refreshAccessTokenDto.RefreshToken, out ClaimsPrincipal? claims))
                {
                    return Results.UnprocessableEntity();
                }
                if (claims == null)
                {
                    return Results.NotFound("Claim not found");
                }
                string? userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (userId == null)
                {
                    return Results.UnprocessableEntity("User id not found");
                }
                BudgetRestUser? user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Results.UnprocessableEntity("Invalid token");
                }

                if (user.ForceRelogin)
                {
                    return Results.UnprocessableEntity();
                }

                IList<string> roles = await userManager.GetRolesAsync(user);

                if (user.UserName == null)
                {
                    return Results.UnprocessableEntity("User not found");
                }
                string accessToken = jwtTokenService.CreateAccessToken(user.UserName, user.Id, roles);
                string refreshToken = jwtTokenService.CreateRefreshToken(user.Id);

                return Results.Ok(new SuccessfulLoginDto(accessToken, refreshToken));

            });

        }
    }
}
