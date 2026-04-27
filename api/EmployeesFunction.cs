using System.Net;
using System.Text.Json;
using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.Sqlite;

namespace Api;

public class EmployeesFunction
{
    private static readonly string DbPath = Path.Combine(
        AppContext.BaseDirectory, "employees.db");

    [Function("employees")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "employees")]
        HttpRequestData req)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var search = query["search"]?.Trim() ?? string.Empty;

            var connectionString = $"Data Source={DbPath};Mode=ReadOnly";

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
        catch (Exception ex)
        {
            // DEBUG ONLY — remove this once we know what's failing.
            var diagnostics = new
            {
                Error = ex.GetType().FullName,
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                BaseDirectory = AppContext.BaseDirectory,
                DbPath,
                DbExists = File.Exists(DbPath),
                DirectoryContents = Directory.Exists(AppContext.BaseDirectory)
                    ? Directory.GetFiles(AppContext.BaseDirectory).Select(Path.GetFileName).ToArray()
                    : Array.Empty<string?>()
            };

            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(diagnostics,
                new JsonSerializerOptions { WriteIndented = true }));
            return response;
        }
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
