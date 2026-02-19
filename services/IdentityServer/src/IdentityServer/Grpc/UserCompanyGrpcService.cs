using Grpc.Core;
using IdentityServer.Entities;
using Microsoft.AspNetCore.Identity;
using Shared.Grpc.Identity;

namespace IdentityServer.Grpc;

public class UserCompanyGrpcService(UserManager<ApplicationUser> userManager) : UserCompanyService.UserCompanyServiceBase
{
    public override async Task<ClearUserCompanyResponse> ClearUserCompany(
        ClearUserCompanyRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required."));
        }

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
        }

        user.CompanyId = null;
        await userManager.UpdateAsync(user);
        return new ClearUserCompanyResponse { Success = true };
    }

    public override async Task<SetUserCompanyResponse> SetUserCompany(
        SetUserCompanyRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.CompanyId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId and CompanyId are required."));
        }

        if (!Guid.TryParse(request.CompanyId, out var companyId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid CompanyId."));
        }

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
        }

        user.CompanyId = companyId;
        await userManager.UpdateAsync(user);
        return new SetUserCompanyResponse { Success = true };
    }
}
