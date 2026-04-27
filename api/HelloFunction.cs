using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Api;

public class HelloFunction
{
    [Function("hello")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hello")]
        HttpRequestData req)
    {
        var principal = ClientPrincipal.FromRequest(req);
        var name = principal?.UserDetails ?? "stranger";

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(
            JsonSerializer.Serialize(new { message = $"Hello, {name}!" }));
        return response;
    }
}

// Represents the user identity injected by Azure Static Web Apps
// in the x-ms-client-principal header (base64-encoded JSON).
public class ClientPrincipal
{
    public string? IdentityProvider { get; set; }
    public string? UserId { get; set; }
    public string? UserDetails { get; set; }
    public IEnumerable<string>? UserRoles { get; set; }

    public static ClientPrincipal? FromRequest(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("x-ms-client-principal", out var values))
            return null;

        var header = values.FirstOrDefault();
        if (string.IsNullOrEmpty(header))
            return null;

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header));
        return JsonSerializer.Deserialize<ClientPrincipal>(
            decoded,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
