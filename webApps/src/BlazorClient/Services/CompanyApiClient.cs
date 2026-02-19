using System.Net.Http.Json;
using System.Text.Json;
using BlazorClient.Configuration;
using BlazorClient.Interfaces;
using BlazorClient.Models.DTO;

namespace BlazorClient.Services;

public class CompanyApiClient(HttpClient http) : ICompanyApiClient
{
    public async Task<CompanyDto?> GetCurrentCompanyAsync(CancellationToken ct = default)
    {
        var query = """
            query GetCompany {
                companies(first: 1) {
                    nodes { id name description maxUsers }
                    totalCount
                }
            }
            """;
        var result = await ExecuteGraphQl<CompaniesResponse>(query, null, ct);
        return result.Companies.Nodes.FirstOrDefault();
    }

    public async Task<List<CompanyMemberDto>> GetMembersAsync(CancellationToken ct = default)
    {
        var query = """
            query GetMembers {
                companyMembers(first: 100) {
                    nodes { id userId role isActive }
                }
            }
            """;
        var result = await ExecuteGraphQl<CompanyMembersResponse>(query, null, ct);
        return result.CompanyMembers.Nodes;
    }

    public async Task<List<InvitationDto>> GetInvitationsAsync(CancellationToken ct = default)
    {
        var query = """
            query GetInvitations {
                invitations(first: 100) {
                    nodes { id email role expiresAt usedAt }
                }
            }
            """;
        var result = await ExecuteGraphQl<InvitationsResponse>(query, null, ct);
        return result.Invitations.Nodes;
    }

    public async Task<InvitationResultDto> CreateInvitationAsync(string? email, string role, int expiresInDays, string baseUrl, CancellationToken ct = default)
    {
        var query = """
            mutation CreateInvitation($email: String, $role: CompanyRole!, $expiresInDays: Int!, $baseUrl: String!) {
                createInvitation(email: $email, role: $role, expiresInDays: $expiresInDays, baseUrl: $baseUrl) {
                    id inviteLink expiresAt
                }
            }
            """;
        var variables = new
        {
            email = string.IsNullOrWhiteSpace(email) ? null : email,
            role,
            expiresInDays,
            baseUrl
        };
        var result = await ExecuteGraphQl<CreateInvitationResponse>(query, variables, ct);
        return result.CreateInvitation;
    }

    private async Task<T> ExecuteGraphQl<T>(string query, object? variables, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiEndpoints.Api}/company/graphql");
        var payload = new { query, variables };
        request.Content = JsonContent.Create(payload);

        var response = await http.SendAsync(request, ct);
        var jsonString = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"HTTP {response.StatusCode}: {jsonString}");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(jsonString, options);
        return gqlResponse!.Data!;
    }

    private class CompaniesResponse
    {
        public CompanyNodesResponse Companies { get; set; } = new();
    }

    private class CompanyNodesResponse
    {
        public List<CompanyDto> Nodes { get; set; } = [];
    }

    private class CompanyMembersResponse
    {
        public CompanyMemberNodesResponse CompanyMembers { get; set; } = new();
    }

    private class CompanyMemberNodesResponse
    {
        public List<CompanyMemberDto> Nodes { get; set; } = [];
    }

    private class InvitationsResponse
    {
        public InvitationNodesResponse Invitations { get; set; } = new();
    }

    private class InvitationNodesResponse
    {
        public List<InvitationDto> Nodes { get; set; } = [];
    }

    private class CreateInvitationResponse
    {
        public InvitationResultDto CreateInvitation { get; set; } = null!;
    }
}
