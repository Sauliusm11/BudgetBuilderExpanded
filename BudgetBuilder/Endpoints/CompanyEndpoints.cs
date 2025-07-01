using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using BudgetBuilder.Auth.Model;
using BudgetBuilder.Data;
using BudgetBuilder.Data.Dtos;
using BudgetBuilder.Data.Entities;
using BudgetBuilder.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using O9d.AspNet.FluentValidation;

namespace BudgetBuilder.Endpoints
{
    public class CompanyEndpoints
    {
        public static void AddCompanyApi(RouteGroupBuilder companiesGroup)
        {
            companiesGroup.MapGet("companies", [Authorize(Roles = BudgetRoles.Admin)] async ([AsParameters] PagingParameters pagingParameters, BudgetDbContext dbContext, LinkGenerator linkGenerator, HttpContext httpContext) =>
            {

                IQueryable<Company> queryable = dbContext.Companies.AsQueryable().OrderBy(o => o.Id);
                int? pageNumber = pagingParameters.PageNumber;
                int? pageSize = pagingParameters.PageSize;
                if(pageNumber == null || pageSize == null)
                {
                    return null;
                }
                PagedList<Company> pagedList = await PagedList<Company>.CreateAsync(queryable, pageNumber.Value, pageSize.Value);

                string? previousPageLink = pagedList.HasPrevious ? linkGenerator.GetUriByName(httpContext, "GetCompanies", new { pageNumber = pagingParameters.PageNumber - 1, pageSize = pagingParameters.PageSize }) : null;
                string? nextPageLink = pagedList.HasNext ? linkGenerator.GetUriByName(httpContext, "GetCompanies", new { pageNumber = pagingParameters.PageNumber + 1, pageSize = pagingParameters.PageSize }) : null;

                var paginationMetaData = new PaginationMetadata(pagedList.TotalCount, pagedList.PageSize, pagedList.CurrentPage, pagedList.TotalPages, previousPageLink, nextPageLink);
                httpContext.Response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationMetaData));
                //https://stackoverflow.com/a/56959114
                httpContext.Response.Headers.Append("Access-Control-Expose-Headers", "Pagination");


                return pagedList.Select(company => new CompanyDto(company.Id, company.Name, company.EstablishedDate));
                //return (await dbContext.Companies.ToListAsync(cancellationToken)).Select(company => new CompanyDto(company.Id, company.Name, company.EstablishedDate));
            }).WithName("GetCompanies");

            companiesGroup.MapGet("companies/{companyId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, int companyId, HttpContext httpContext) =>
            {
                Company? company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                if (company == null)
                {
                    //404
                    return Results.NotFound();
                }

                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != company.UserId)
                {
                    return Results.Forbid();
                }
                //200
                return Results.Ok(new CompanyDto(company.Id, company.Name, company.EstablishedDate));
            }).WithName("GetCompany");

            companiesGroup.MapPost("companies", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, [Validate] CreateCompanyDto createCompanyDto, LinkGenerator linkGenerator, HttpContext httpContext) =>
            {
                string? userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (userId == null)
                {
                    return Results.Forbid();
                }

                var company = new Company()
                {
                    Name = createCompanyDto.Name,
                    EstablishedDate = createCompanyDto.EstablishedDate,
                    UserId = userId
                };

                dbContext.Companies.Add(company);

                await dbContext.SaveChangesAsync();

                IEnumerable<LinkDto> links = CreateLinks(company.Id, httpContext, linkGenerator);
                var companyDto = new CompanyDto(company.Id, company.Name, company.EstablishedDate);

                var resource = new ResourceDto<CompanyDto>(companyDto, links.ToArray());
                //201
                return Results.Created($"api/v1/companies/{company.Id}", resource);

            }).WithName("CreateCompany");

            companiesGroup.MapPut("companies/{companyId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, [Validate] UpdateCompanyDto updateCompanyDto, int companyId, HttpContext httpContext) =>
            {
                Company? company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                if (company == null)
                {
                    //404
                    return Results.NotFound();
                }

                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != company.UserId)
                {
                    return Results.Forbid();
                }

                if (updateCompanyDto.Name == null || updateCompanyDto.EstablishedDate.Equals(DateTime.MinValue))
                {
                    return Results.UnprocessableEntity();
                }
                company.Name = updateCompanyDto.Name;
                company.EstablishedDate = updateCompanyDto.EstablishedDate;

                dbContext.Companies.Update(company);
                await dbContext.SaveChangesAsync();
                //200
                return Results.Ok(new CompanyDto(company.Id, company.Name, company.EstablishedDate));
            }).WithName("EditCompany");

            companiesGroup.MapDelete("companies/{companyId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, int companyId, HttpContext httpContext) =>
            {
                Company? company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                if (company == null)
                {
                    //404
                    return Results.NotFound();
                }

                if (!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != company.UserId)
                {
                    return Results.Forbid();
                }
                dbContext.Companies.Remove(company);
                await dbContext.SaveChangesAsync();

                return Results.Ok();
            }).WithName("DeleteCompany");
        }
        static IEnumerable<LinkDto> CreateLinks(int companyId, HttpContext httpContext, LinkGenerator linkGenerator)
        {
            string? getUri = linkGenerator.GetUriByName(httpContext, "GetCompany", new { companyId });
            string? editUri = linkGenerator.GetUriByName(httpContext, "EditCompany", new { companyId });
            string? deleteUri = linkGenerator.GetUriByName(httpContext, "DeleteCompany", new { companyId });
            ArgumentNullException.ThrowIfNull(getUri);
            ArgumentNullException.ThrowIfNull(editUri);
            ArgumentNullException.ThrowIfNull(deleteUri);
            yield return new LinkDto(getUri, "self", "GET");
            yield return new LinkDto(editUri, "edit", "PUT");
            yield return new LinkDto(deleteUri, "delete", "DELETE");
        }
    }
}
