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
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BudgetBuilder.Endpoints
{
    public class PurchaseEndpoints
    {
        public static void AddPurchaseApi(RouteGroupBuilder purchasesGroup)
        {
            purchasesGroup.MapGet("purchases", [Authorize(Roles = BudgetRoles.BudgetUser)] async ([AsParameters] PagingParameters pagingParameters, BudgetDbContext dbContext, CancellationToken cancellationToken, int companyId, int departmentId, LinkGenerator linkGenerator, HttpContext httpContext, UserManager<BudgetRestUser> userManager) =>
            {
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
                if (department == null)
                {
                    //404
                    return Results.NotFound();
                }
                var purchase = await dbContext.Purchases.FirstOrDefaultAsync(d => d.Department.Id == departmentId);
                if (purchase == null)
                {
                    //200, no purchases
                    return Results.Ok();
                }
                var user = await userManager.FindByIdAsync(httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub));
                var supervisor = user.SupervisorId;
                if(!httpContext.User.IsInRole(BudgetRoles.Admin) && supervisor == null)
                {
                    return Results.Forbid();
                }
                if (!(httpContext.User.IsInRole(BudgetRoles.Admin) || (httpContext.User.IsInRole(BudgetRoles.CompanyManager) && department.UserId == httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub))) 
                && supervisor != department.UserId)
                {
                    return Results.Forbid();
                }

                var queryable = dbContext.Purchases.AsQueryable().OrderBy(o => o.Id).Where(o => o.Department.Company.Id == companyId && o.Department.Id == departmentId);
                var pagedList = await PagedList<Purchase>.CreateAsync(queryable, pagingParameters.PageNumber.Value, pagingParameters.PageSize.Value);

                var previousPageLink = pagedList.HasPrevious ? linkGenerator.GetUriByName(httpContext, "GetPurchases", new { pageNumber = pagingParameters.PageNumber - 1, pageSize = pagingParameters.PageSize }) : null;
                var nextPageLink = pagedList.HasNext ? linkGenerator.GetUriByName(httpContext, "GetPurchases", new { pageNumber = pagingParameters.PageNumber + 1, pageSize = pagingParameters.PageSize }) : null;
                var paginationMetaData = new PaginationMetadata(pagedList.TotalCount, pagedList.PageSize, pagedList.CurrentPage, pagedList.TotalPages, previousPageLink, nextPageLink);
                httpContext.Response.Headers.Add("Pagination", JsonSerializer.Serialize(paginationMetaData));
                //https://stackoverflow.com/a/56959114
                httpContext.Response.Headers.Add("Access-Control-Expose-Headers", "Pagination");

                return Results.Ok(pagedList.Select(purchase => new PurchaseDto(purchase.Id, purchase.Name, purchase.Approved, purchase.Amount, purchase.Cost, purchase.PurchaseDate)));
                //return Results.Ok((await dbContext.Purchases.ToListAsync(cancellationToken)).Where(d => d.Department.Id == departmentId).Select(purchase => new PurchaseDto(purchase.Id, purchase.Name, purchase.Approved, purchase.Amount, purchase.Cost, purchase.PurchaseDate)));
            }).WithName("GetPurchases");

            purchasesGroup.MapGet("purchases/{purchaseId}", [Authorize(Roles = BudgetRoles.BudgetUser)] async (BudgetDbContext dbContext, int companyId, int departmentId, int purchaseId, HttpContext httpContext) =>
            {
                var purchase = await dbContext.Purchases.FirstOrDefaultAsync(p => p.Id == purchaseId && p.Department.Company.Id == companyId && p.Department.Id == departmentId );
                if (purchase == null)
                {
                    //404
                    return Results.NotFound();
                }
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => departmentId == d.Id);
                if (!(httpContext.User.IsInRole(BudgetRoles.Admin) || httpContext.User.IsInRole(BudgetRoles.CompanyManager)) && purchase.UserId != httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub))
                {
                    if (httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != purchase.UserId)
                    {
                        return Results.Forbid();
                    }
                }
                //200
                return Results.Ok(new PurchaseDto(purchase.Id, purchase.Name, purchase.Approved, purchase.Amount, purchase.Cost, purchase.PurchaseDate));
            }).WithName("GetPurchase");

            purchasesGroup.MapPost("purchases", [Authorize(Roles = BudgetRoles.BudgetUser)] async (BudgetDbContext dbContext, [Validate] CreatePurchaseDto createPurchaseDto, int companyId, int departmentId, LinkGenerator linkGenerator, HttpContext httpContext, UserManager<BudgetRestUser> userManager) =>
            {
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
                if (department == null)
                {
                    //404
                    return Results.NotFound();
                }
                var purchase = new Purchase() { Name = createPurchaseDto.Name, Approved = httpContext.User.IsInRole(BudgetRoles.CompanyManager), Amount = createPurchaseDto.Amount, Cost = createPurchaseDto.Cost, PurchaseDate = createPurchaseDto.PurchaseDate, Department = department,
                UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                };
                var user = await userManager.FindByIdAsync(httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub));
                var supervisor = user.SupervisorId;
                if (supervisor == null)
                {
                    return Results.Forbid();
                }
                if (!(httpContext.User.IsInRole(BudgetRoles.Admin) || (httpContext.User.IsInRole(BudgetRoles.CompanyManager) && department.UserId == httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)))
                && supervisor != department.UserId)
                {
                    return Results.Forbid();
                }

                dbContext.Purchases.Add(purchase);
                await dbContext.SaveChangesAsync();


                var links = CreateLinks(companyId, departmentId, purchase.Id, httpContext, linkGenerator);
                var purchaseDto = new PurchaseDto(purchase.Id, purchase.Name, purchase.Approved, purchase.Amount, purchase.Cost, purchase.PurchaseDate);
                var resource = new ResourceDto<PurchaseDto>(purchaseDto, links.ToArray());
                //201
                return Results.Created($"api/v1/companies/{companyId}/departments/{department.Id}/purchases/{purchase.Id}", resource);
            }).WithName("CreatePurchase");

            purchasesGroup.MapPut("purchases/{purchaseId}", [Authorize(Roles = BudgetRoles.BudgetUser)] async (BudgetDbContext dbContext, [Validate] UpdatePurchaseDto updatePurchaseDto, int companyId, int departmentId, int purchaseId, HttpContext httpContext) =>
            { 
                var purchase = await dbContext.Purchases.FirstOrDefaultAsync(p => p.Id == purchaseId && p.Department.Company.Id == companyId && p.Department.Id == departmentId);
                if (purchase == null)
                {
                    //404
                    return Results.NotFound();
                }
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
                if (!(httpContext.User.IsInRole(BudgetRoles.Admin) || (httpContext.User.IsInRole(BudgetRoles.CompanyManager) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) == department.UserId)) && purchase.UserId != httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub))
                {
                    return Results.Forbid();
                }


                if (updatePurchaseDto.PurchaseDate.Equals(DateTime.MinValue))
                {
                    return Results.UnprocessableEntity();
                }
                purchase.Name = updatePurchaseDto.Name;
                purchase.Amount = updatePurchaseDto.Amount;
                purchase.Cost = updatePurchaseDto.Cost;
                purchase.PurchaseDate = updatePurchaseDto.PurchaseDate;
                purchase.Approved = httpContext.User.IsInRole(BudgetRoles.CompanyManager);

                dbContext.Purchases.Update(purchase);
                await dbContext.SaveChangesAsync();
                //200
                return Results.Ok(new PurchaseDto(purchase.Id, purchase.Name, purchase.Approved, purchase.Amount, purchase.Cost, purchase.PurchaseDate));
            }).WithName("EditPurchase");

            purchasesGroup.MapDelete("purchases/{purchaseId}", [Authorize(Roles = BudgetRoles.BudgetUser)] async (BudgetDbContext dbContext, int companyId, int departmentId, int purchaseId, HttpContext httpContext) =>
            {
                var purchase = await dbContext.Purchases.FirstOrDefaultAsync(p => p.Id == purchaseId && p.Department.Company.Id == companyId && p.Department.Id == departmentId);
                if (purchase == null)
                {
                    //404
                    return Results.NotFound();
                }
                var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.Company.Id == companyId);
                if (!(httpContext.User.IsInRole(BudgetRoles.Admin) || (httpContext.User.IsInRole(BudgetRoles.CompanyManager) && httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) == department.UserId)) && purchase.UserId != httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub))
                {
                    if (httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) != purchase.UserId)
                    {
                        return Results.Forbid();
                    }
                }
                dbContext.Purchases.Remove(purchase);
                await dbContext.SaveChangesAsync();

                return Results.Ok();
            }).WithName("DeletePurchase");
        }
        static IEnumerable<LinkDto> CreateLinks(int companyId, int departmentId, int purchaseId, HttpContext httpContext, LinkGenerator linkGenerator)
        {
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "GetPurchase", new { companyId, departmentId, purchaseId }), "self", "GET");
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "EditPurchase", new { companyId, departmentId, purchaseId }), "edit", "PUT");
            yield return new LinkDto(linkGenerator.GetUriByName(httpContext, "DeletePurchase", new { companyId, departmentId, purchaseId }), "delete", "DELETE");
        }

    }
}
