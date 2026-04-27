using System.Net;
using System.Text.Json;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.Sqlite;

namespace Api;

public class EmployeesFunction
{
    // Azure App Service deploys with a read-only wwwroot, which causes SQLite
    // to fail with "database is locked" even for read-only queries (it can't
    // create lock/journal files alongside the .db). Workaround: copy the .db
    // to the writable temp directory on first use. Lazy<T> ensures it happens
    // exactly once across all requests.
    //
    // This is a SQLite-specific quirk; real SQL Server doesn't have an
    // equivalent issue, so this code disappears when we swap providers.
    private static readonly Lazy<string> WritableDbPath = new(() =>
    {
        var source = Path.Combine(AppContext.BaseDirectory, "employees.db");
        var dest = Path.Combine(Path.GetTempPath(), "employees.db");
        File.Copy(source, dest, overwrite: true);
        return dest;
    });

    [Function("employees")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "employees")]
        HttpRequestData req)
    {
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
        var search = query["search"]?.Trim() ?? string.Empty;

        var connectionString = $"Data Source={WritableDbPath.Value};Mode=ReadOnly";

        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync();

        const string sql = @"
            SELECT id          AS Id,
                   first_name  AS FirstName,
                   last_name   AS LastName,
                   email       AS Email,
                   department  AS Department,
                   title       AS Title,
                   extension   AS Extension,
                   location    AS Location,
                   start_date  AS StartDate
            FROM v_employee_directory
            WHERE @search = ''
               OR first_name LIKE '%' || @search || '%'
               OR last_name  LIKE '%' || @search || '%'
               OR department LIKE '%' || @search || '%'
               OR title      LIKE '%' || @search || '%'
            ORDER BY last_name, first_name;";

        var rows = await conn.QueryAsync<Employee>(sql, new { search });

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(
            rows,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        return response;
    }
}

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public string Title { get; set; } = "";
    public string Extension { get; set; } = "";
    public string Location { get; set; } = "";
    public string StartDate { get; set; } = "";
}
