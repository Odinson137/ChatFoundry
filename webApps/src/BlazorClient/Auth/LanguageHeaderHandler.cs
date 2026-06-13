using System.Globalization;
using System.Net.Http.Headers;

namespace BlazorClient.Auth;

public class LanguageHeaderHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var culture = CultureInfo.CurrentCulture.Name;
        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture));
        return base.SendAsync(request, cancellationToken);
    }
}
