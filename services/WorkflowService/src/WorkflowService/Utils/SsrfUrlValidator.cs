using System.Net;

namespace WorkflowService.Utils;

/// <summary>
/// Validates URLs for SSRF (Server-Side Request Forgery) protection.
/// Blocks localhost, loopback, private and link-local IPs, and cloud metadata endpoints.
/// </summary>
public sealed class SsrfUrlValidator
{
    /// <summary>
    /// Returns true if the URL is allowed for outbound HTTP requests; false if it targets a blocked host.
    /// </summary>
    /// <param name="url">Absolute URL (after variable substitution).</param>
    /// <param name="blockReason">If blocked, the reason (e.g. "localhost").</param>
    public bool IsUrlAllowed(string url, out string? blockReason)
    {
        blockReason = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            blockReason = "URL is empty";
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.IsAbsoluteUri)
        {
            blockReason = "Invalid URL";
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            blockReason = "Only http and https are allowed";
            return false;
        }

        var host = uri.Host;
        if (string.IsNullOrEmpty(host))
        {
            blockReason = "Missing host";
            return false;
        }

        // Block literal hostnames that resolve to loopback
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            blockReason = "localhost";
            return false;
        }

        // Check if host is an IP address
        if (IPAddress.TryParse(host, out var ip))
            return IsIpAllowed(ip, out blockReason);

        // IPv6 in brackets (Uri.Host already strips brackets)
        if (host.StartsWith('[') && host.EndsWith(']') && IPAddress.TryParse(host.AsSpan(1, host.Length - 2), out var ip6))
            return IsIpAllowed(ip6, out blockReason);

        // Hostname: allow (we don't resolve to avoid DNS rebinding complexity; optional future: resolve and re-check)
        return true;
    }

    private static bool IsIpAllowed(IPAddress ip, out string? blockReason)
    {
        blockReason = null;

        if (IPAddress.IsLoopback(ip))
        {
            blockReason = "loopback";
            return false;
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            // 10.0.0.0/8
            if (bytes[0] == 10) { blockReason = "private network (10.0.0.0/8)"; return false; }
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) { blockReason = "private network (172.16.0.0/12)"; return false; }
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) { blockReason = "private network (192.168.0.0/16)"; return false; }
            // 169.254.0.0/16 (link-local, includes cloud metadata 169.254.169.254)
            if (bytes[0] == 169 && bytes[1] == 254) { blockReason = "link-local / metadata"; return false; }
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv4MappedToIPv6)
                return IsIpAllowed(ip.MapToIPv4(), out blockReason);
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
            {
                blockReason = "IPv6 link-local or unique local";
                return false;
            }
        }

        return true;
    }
}
