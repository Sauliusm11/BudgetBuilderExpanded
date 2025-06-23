using BudgetBuilder.Auth;
using BudgetBuilder.Auth.Model;
using BudgetBuilder.Data;
using BudgetBuilder.Endpoints;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidAudience = builder.Configuration["Jwt:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]));
});

builder.Services.AddAuthorization();

var app = builder.Build();

var companiesGroup = app.MapGroup("/api/v1").WithValidationFilter();
CompanyEndpoints.AddCompanyApi(companiesGroup);
var departmentsGroup = app.MapGroup("/api/v1/companies/{companyId}").WithValidationFilter();
DepartmentEndpoints.AddDepartmentApi(departmentsGroup);
var purchasesGroup = app.MapGroup("/api/v1/companies/{companyId}/departments/{departmentId}").WithValidationFilter();
PurchaseEndpoints.AddPurchaseApi(purchasesGroup);

app.AddAuthApi();



app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

using var scope = app.Services.CreateScope();
var dbcontext = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
dbcontext.Database.Migrate();
var dbSeeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();

await dbSeeder.SeedAsync();

app.Run();


