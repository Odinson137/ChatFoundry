using CompanyService.Data;
using CompanyService.Entities;
using CompanyService.Enums;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Shared.Grpc.Company;

namespace CompanyService.Grpc;

public class CompanyRegistrationGrpcService(CompanyDbContext db) : CompanyRegistrationService.CompanyRegistrationServiceBase
{
    public override async Task<CreateCompanyWithOwnerResponse> CreateCompanyWithOwner(
        CreateCompanyWithOwnerRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Name is required."));
        if (string.IsNullOrWhiteSpace(request.OwnerUserId) || !Guid.TryParse(request.OwnerUserId, out var ownerId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid OwnerUserId is required."));

        var company = new Company
        {
            Name = request.Name.Trim(),
            MaxUsers = 100
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync(context.CancellationToken);

        var member = new CompanyMember
        {
            CompanyId = company.Id,
            UserId = ownerId,
            Role = CompanyRole.Owner,
            IsActive = true
        };
        db.CompanyMembers.Add(member);
        await db.SaveChangesAsync(context.CancellationToken);

        return new CreateCompanyWithOwnerResponse { CompanyId = company.Id.ToString() };
    }

    public override async Task<ConsumeInviteResponse> ConsumeInvite(
        ConsumeInviteRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.InviteToken) || !Guid.TryParse(request.InviteToken, out var invitationId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid InviteToken is required."));
        if (string.IsNullOrWhiteSpace(request.UserId) || !Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid UserId is required."));

        var invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, context.CancellationToken);
        if (invitation == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Invitation not found."));
        if (invitation.UsedAt.HasValue)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invitation already used."));
        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Invitation expired."));

        invitation.UsedAt = DateTime.UtcNow;
        invitation.ModifiedAt = DateTime.UtcNow;

        var member = new CompanyMember
        {
            CompanyId = invitation.CompanyId,
            UserId = userId,
            Role = invitation.Role,
            IsActive = true
        };
        db.CompanyMembers.Add(member);
        await db.SaveChangesAsync(context.CancellationToken);

        return new ConsumeInviteResponse { CompanyId = invitation.CompanyId.ToString() };
    }

    public override async Task<RollbackMemberResponse> RollbackMember(
        RollbackMemberRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId) || !Guid.TryParse(request.CompanyId, out var companyId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid CompanyId is required."));
        if (string.IsNullOrWhiteSpace(request.UserId) || !Guid.TryParse(request.UserId, out var userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Valid UserId is required."));

        var member = await db.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == userId, context.CancellationToken);
        if (member != null)
        {
            db.CompanyMembers.Remove(member);
            var remaining = await db.CompanyMembers.CountAsync(m => m.CompanyId == companyId, context.CancellationToken);
            if (remaining == 0)
            {
                var company = await db.Companies.FindAsync([companyId], context.CancellationToken);
                if (company != null)
                    db.Companies.Remove(company);
            }
            await db.SaveChangesAsync(context.CancellationToken);
        }

        return new RollbackMemberResponse { Success = true };
    }
}
