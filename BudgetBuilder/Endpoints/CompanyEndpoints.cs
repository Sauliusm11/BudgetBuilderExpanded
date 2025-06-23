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
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using BudgetBuilder.Auth.Model;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;

namespace BudgetBuilder.Endpoints
{
    public class CompanyEndpoints
    {
        public static void AddCompanyApi(RouteGroupBuilder companiesGroup)
        {
            companiesGroup.MapGet("companies", [Authorize(Roles = BudgetRoles.Admin)] async ([AsParameters] PagingParameters pagingParameters,BudgetDbContext dbContext, LinkGenerator linkGenerator, HttpContext httpContext) =>
            {

                var queryable =  dbContext.Companies.AsQueryable().OrderBy(o => o.Id);
                var pagedList = await PagedList<Company>.CreateAsync(queryable, pagingParameters.PageNumber.Value, pagingParameters.PageSize.Value);

                var previousPageLink = pagedList.HasPrevious ? linkGenerator.GetUriByName(httpContext, "GetCompanies", new { pageNumber = pagingParameters.PageNumber - 1, pageSize = pagingParameters.PageSize }) : null;
                var nextPageLink = pagedList.HasNext ? linkGenerator.GetUriByName(httpContext, "GetCompanies", new { pageNumber = pagingParameters.PageNumber + 1, pageSize = pagingParameters.PageSize }) : null;

                var paginationMetaData = new PaginationMetadata(pagedList.TotalCount, pagedList.PageSize, pagedList.CurrentPage, pagedList.TotalPages, previousPageLink, nextPageLink);
                httpContext.Response.Headers.Add("Pagination",JsonSerializer.Serialize(paginationMetaData));
                //https://stackoverflow.com/a/56959114
                httpContext.Response.Headers.Add("Access-Control-Expose-Headers", "Pagination");


                return pagedList.Select(company => new CompanyDto(company.Id, company.Name, company.EstablishedDate));
                //return (await dbContext.Companies.ToListAsync(cancellationToken)).Select(company => new CompanyDto(company.Id, company.Name, company.EstablishedDate));
            }).WithName("GetCompanies");

            companiesGroup.MapGet("companies/{companyId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, int companyId, HttpContext httpContext) =>
            {
                var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
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
                var company = new Company() { Name = createCompanyDto.Name, EstablishedDate = createCompanyDto.EstablishedDate, 
                UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) };

                dbContext.Companies.Add(company);

                await dbContext.SaveChangesAsync();

                var links = CreateLinks(company.Id, httpContext, linkGenerator);
                var companyDto = new CompanyDto(company.Id, company.Name, company.EstablishedDate);

                var resource = new ResourceDto<CompanyDto>(companyDto, links.ToArray());
                //201
                return Results.Created($"api/v1/companies/{company.Id}", resource);
            }).WithName("CreateCompany");

            companiesGroup.MapPut("companies/{companyId}", [Authorize(Roles = BudgetRoles.CompanyManager)] async (BudgetDbContext dbContext, [Validate] UpdateCompanyDto updateCompanyDto, int companyId, HttpContext httpContext) =>
            {
                var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
                if (company == null)
                {
                    //404
                    return Results.NotFound();
                }

                if(!httpContext.User.IsInRole(BudgetRoles.Admin) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != company.UserId)
                {
                    return Results.Forbid();
                }

                if(updateCompanyDto.Name == null || updateCompanyDto.EstablishedDate.Equals(DateTime.MinValue))
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
                var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
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
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetCompany", new { companyId }), "self", "GET");
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "EditCompany", new { companyId }), "edit", "PUT");
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "DeleteCompany", new { companyId }), "delete", "DELETE");
        }
    }
}
