using BudgetBuilder.Data.Entities;
using BudgetBuilder.Data;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using O9d.AspNet.FluentValidation;
using System.ComponentModel.DataAnnotations;
using BudgetBuilder.Data.Dtos;
using BudgetBuilder.Helpers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.ComponentModel.Design;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using BudgetBuilder.Auth.Model;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BudgetBuilder.Endpoints
{
    public class DepartmentEndpoints
    {
        public static void AddDepartmentApi(RouteGroupBuilder departmentsGroup)
        {
            departmentsGroup.MapGet("departments", [Authorize(Roles = BudgetRoles.CompanyManager)] async ([AsParameters] PagingParameters pagingParameters, BudgetDbContext dbContext, CancellationToken cancellationToken, int companyId, LinkGenerator linkGenerator, HttpContext httpContext) =>
            {
                var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                if (company == null)
                {
                    //404
                    return Results.NotFound();
                }
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Company.Id == companyId);
                if (department == null)
                {
                    //200, no departments
                    return Results.Ok();
                }

                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != company.UserId)
                {
                    return Results.Forbid();
                }
                var queryable = dbContext.Departments.AsQueryable().OrderBy(o => o.Id).Where(o => o.Company.Id == companyId);
                var pagedList = await PagedList<Department>.CreateAsync(queryable, pagingParameters.PageNumber.Value, pagingParameters.PageSize.Value);

                var previousPageLink = pagedList.HasPrevious ? linkGenerator.GetUriByName(httpContext, "GetDepartments", new { pageNumber = pagingParameters.PageNumber - 1, pageSize = pagingParameters.PageSize }) : null;
                var nextPageLink = pagedList.HasNext ? linkGenerator.GetUriByName(httpContext, "GetDepartments", new { pageNumber = pagingParameters.PageNumber + 1, pageSize = pagingParameters.PageSize }) : null;
                var paginationMetaData = new PaginationMetadata(pagedList.TotalCount, pagedList.PageSize, pagedList.CurrentPage, pagedList.TotalPages, previousPageLink, nextPageLink);
                httpContext.Response.Headers.Add("Pagination", JsonSerializer.Serialize(paginationMetaData));
                //https://stackoverflow.com/a/56959114
                httpContext.Response.Headers.Add("Access-Control-Expose-Headers", "Pagination");

                return Results.Ok(pagedList.Select(department => new DepartmentDto(department.Id, department.Name)));

                // return Results.Ok((await dbContext.Departments.ToListAsync(cancellationToken)).Where(d => d.Company.Id == companyId).Select(department => new DepartmentDto(department.Id, department.Name)));
            }).WithName("GetDepartmnets");

            departmentsGroup.MapGet("departments/{departmentId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, int companyId,int departmentId, HttpContext httpContext) =>
            {
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId );
                if (department == null)
                {
                    //404
                    return Results.NotFound();
                }
                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != department.UserId)
                {
                    return Results.Forbid();
                }

                //200
                return Results.Ok(new DepartmentDto(department.Id, department.Name));
            }).WithName("GetDepartment");

            departmentsGroup.MapPost("departments", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, [Validate] CreateDepartmentDto createDepartmentDto, int companyId, LinkGenerator linkGenerator, HttpContext httpContext) =>
            {
                var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                if (company == null)
                {
                    //404
                    return Results.NotFound();
                }
                var department = new Department() { Name = createDepartmentDto.Name, Company = company,
                UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                };

                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != company.UserId)
                {
                    return Results.Forbid();
                }

                dbContext.Departments.Add(department);
                await dbContext.SaveChangesAsync();

                var links = CreateLinks(department.Company.Id, department.Id, httpContext, linkGenerator);
                var departmentDto = new DepartmentDto(department.Id, department.Name);
                var resource = new ResourceDto<DepartmentDto>(departmentDto, links.ToArray());
                //201
                return Results.Created($"api/v1/companies/{company.Id}/departments/{department.Id}", resource);
            }).WithName("CreateDepartment");

            departmentsGroup.MapPut("departments/{departmentId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, [Validate] UpdateDepartmentDto updateDepartmentDto, int companyId, int departmentId, HttpContext httpContext) =>
            {
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
                if (department == null)
                {
                    //404
                    return Results.NotFound();
                }
                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != department.UserId)
                {
                    return Results.Forbid();
                }

                department.Name = updateDepartmentDto.Name;

                dbContext.Departments.Update(department);
                await dbContext.SaveChangesAsync();
                //200
                return Results.Ok(new DepartmentDto(department.Id, department.Name));
            }).WithName("EditDepartment");
            departmentsGroup.MapDelete("departments/{departmentId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, int companyId, int departmentId, HttpContext httpContext) =>
            {
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
                if (department == null)
                {
                    //404
                    return Results.NotFound();
                }
                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != department.UserId)
                {
                    return Results.Forbid();
                }

                dbContext.Departments.Remove(department);
                await dbContext.SaveChangesAsync();

                return Results.Ok();
            }).WithName("DeleteDepartment");
        }

        static IEnumerable<LinkDto> CreateLinks(int companyId, int departmentId, HttpContext httpContext, LinkGenerator linkGenerator)
        {
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetDepartment", new { companyId, departmentId }), "self", "GET");
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "EditDepartment", new { companyId, departmentId }), "edit", "PUT");
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "DeleteDepartment", new { companyId, departmentId }), "delete", "DELETE");
        }

    }
}
