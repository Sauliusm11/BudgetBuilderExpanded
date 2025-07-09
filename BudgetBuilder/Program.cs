using BudgetBuilder.Domain.Auth;
using BudgetBuilder.Domain.Auth.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using BudgetBuilder.Infrastructure;
using BudgetBuilder.API.Endpoints;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BudgetDbContext>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddTransient<JwtTokenService>();
builder.Services.AddScoped<AuthDbSeeder>();


builder.Services.AddIdentity<BudgetRestUser, IdentityRole>()
    .AddEntityFrameworkStores<BudgetDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader().
            AllowAnyMethod();
        });
});

string? secretString = builder.Configuration["Jwt:Secret"];
ArgumentNullException.ThrowIfNull(secretString);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretString));
});

builder.Services.AddAuthorization();

WebApplication app = builder.Build();

RouteGroupBuilder companiesGroup = app.MapGroup("/api/v1").WithValidationFilter();
CompanyEndpoints.AddCompanyApi(companiesGroup);
RouteGroupBuilder departmentsGroup = app.MapGroup("/api/v1/companies/{companyId}").WithValidationFilter();
DepartmentEndpoints.AddDepartmentApi(departmentsGroup);
RouteGroupBuilder purchasesGroup = app.MapGroup("/api/v1/companies/{companyId}/departments/{departmentId}").WithValidationFilter();
PurchaseEndpoints.AddPurchaseApi(purchasesGroup);

app.AddAuthApi();



app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

using IServiceScope scope = app.Services.CreateScope();
BudgetDbContext dbcontext = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
dbcontext.Database.Migrate();
AuthDbSeeder dbSeeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();

await dbSeeder.SeedAsync();

app.Run();


