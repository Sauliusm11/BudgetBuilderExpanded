using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using BudgetBuilder.Domain.Data.Dtos;
using BudgetBuilder.Domain.Data.Entities;
using BudgetBuilder.Domain.Auth.Model;
using BudgetBuilder.Domain.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using O9d.AspNet.FluentValidation;
using BudgetBuilder.Infrastructure;

namespace BudgetBuilder.API.Endpoints
{
    public class DepartmentEndpoints
    {
        public static void AddDepartmentApi(RouteGroupBuilder departmentsGroup)
        {
            departmentsGroup.MapGet("departments", [Authorize(Roles = BudgetRoles.CompanyManager)] async ([AsParameters] PagingParameters pagingParameters, BudgetDbContext dbContext, CancellationToken cancellationToken, int companyId, LinkGenerator linkGenerator, HttpContext httpContext) =>
            {
                Company? company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                if (company == null)
                {
                    //404
                    return Results.NotFound();
                }
                Department? department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Company.Id == companyId);
                if (department == null)
                {
                    //200, no departments
                    return Results.Ok();
                }

                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != company.UserId)
                {
                    return Results.Forbid();
                }
                IQueryable<Department> queryable = dbContext.Departments.AsQueryable().OrderBy(o => o.Id).Where(o => o.Company.Id == companyId);
                int? pageNumber = pagingParameters.PageNumber;
                int? pageSize = pagingParameters.PageSize;
                if (pageNumber == null || pageSize == null)
                {
                    return Results.InternalServerError("Page not found");
                }
                PagedList<Department> pagedList = await PagedList<Department>.CreateAsync(queryable, pageNumber.Value, pageSize.Value);

                string? previousPageLink = pagedList.HasPrevious ? linkGenerator.GetUriByName(httpContext, "GetDepartments", new { pageNumber = pagingParameters.PageNumber - 1, pageSize = pagingParameters.PageSize }) : null;
                string? nextPageLink = pagedList.HasNext ? linkGenerator.GetUriByName(httpContext, "GetDepartments", new { pageNumber = pagingParameters.PageNumber + 1, pageSize = pagingParameters.PageSize }) : null;
                var paginationMetaData = new PaginationMetadata(pagedList.TotalCount, pagedList.PageSize, pagedList.CurrentPage, pagedList.TotalPages, previousPageLink, nextPageLink);
                httpContext.Response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationMetaData));
                //https://stackoverflow.com/a/56959114
                httpContext.Response.Headers.Append("Access-Control-Expose-Headers", "Pagination");

                return Results.Ok(pagedList.Select(department => new DepartmentDto(department.Id, department.Name)));

                // return Results.Ok((await dbContext.Departments.ToListAsync(cancellationToken)).Where(d => d.Company.Id == companyId).Select(department => new DepartmentDto(department.Id, department.Name)));
            }).WithName("GetDepartmnets");

            departmentsGroup.MapGet("departments/{departmentId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, int companyId, int departmentId, HttpContext httpContext) =>
            {
                Department? department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
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
                Company? company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                if (company == null)
                {
                    //404
                    return Results.NotFound();
                }
                string? userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (userId == null)
                {
                    return Results.Forbid();
                }

                var department = new Department()
                {
                    Name = createDepartmentDto.Name,
                    Company = company,
                    UserId = userId
                };

                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != company.UserId)
                {
                    return Results.Forbid();
                }

                dbContext.Departments.Add(department);
                await dbContext.SaveChangesAsync();

                IEnumerable<LinkDto> links = CreateLinks(department.Company.Id, department.Id, httpContext, linkGenerator);
                var departmentDto = new DepartmentDto(department.Id, department.Name);
                var resource = new ResourceDto<DepartmentDto>(departmentDto, links.ToArray());
                //201
                return Results.Created($"api/v1/companies/{company.Id}/departments/{department.Id}", resource);

            }).WithName("CreateDepartment");

            departmentsGroup.MapPut("departments/{departmentId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, [Validate] UpdateDepartmentDto updateDepartmentDto, int companyId, int departmentId, HttpContext httpContext) =>
            {
                Department? department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
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
                Department? department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
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
            string? getUri = linkGenerator.GetUriByName(httpContext, "GetDepartment", new { companyId, departmentId });
            string? editUri = linkGenerator.GetUriByName(httpContext, "EditDepartment", new { companyId, departmentId });
            string? deleteUri = linkGenerator.GetUriByName(httpContext, "DeleteDepartment", new { companyId, departmentId });
            ArgumentNullException.ThrowIfNull(getUri);
            ArgumentNullException.ThrowIfNull(editUri);
            ArgumentNullException.ThrowIfNull(deleteUri);
            yield return new LinkDto(getUri, "self", "GET");
            yield return new LinkDto(editUri, "edit", "PUT");
            yield return new LinkDto(deleteUri, "delete", "DELETE");
        }

    }
}
