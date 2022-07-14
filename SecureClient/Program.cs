using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace SecureClient;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Making the call...");
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        AuthConfig config = AuthConfig.ReadFromJsonFile("appsettings.json");

        IConfidentialClientApplication app;

        app = ConfidentialClientApplicationBuilder.Create(config.ClientId)
            .WithClientSecret(config.ClientSecret)
            .WithAuthority(new Uri(config.Authority))
            .Build();

        string[] scopes = config.Scopes.ToArray();

        AuthenticationResult result = null;
        try
        {
            result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Token acquired \n");
            Console.WriteLine(result.AccessToken);
            Console.ResetColor();
        }
        catch (MsalClientException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }

        if (!string.IsNullOrEmpty(result.AccessToken))
        {
            var httpClient = new HttpClient();
            var defaultRequestHeaders = httpClient.DefaultRequestHeaders;

            if (defaultRequestHeaders.Accept == null ||
               !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new
                  MediaTypeWithQualityHeaderValue("application/json"));
            }
            defaultRequestHeaders.Authorization =
              new AuthenticationHeaderValue("Bearer", result.AccessToken);

            HttpResponseMessage response = await httpClient.GetAsync("http://localhost:5182/weatherforecast");
            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                string json = await response.Content.ReadAsStringAsync();
                Console.WriteLine(json);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to call the Web Api: {response.StatusCode}");
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Content: {content}");
            }
            Console.ResetColor();
        }

    }
}

public class AuthConfig
{
    public string Instance { get; set; } =
      "https://login.microsoftonline.com/{0}";

    public string TenantId { get; set; }

    public string ClientId { get; set; }

    public string Authority
    {
        get
        {
            return String.Format(CultureInfo.InvariantCulture,
                                 Instance, TenantId);
        }
    }

    public string ClientSecret { get; set; }

    public List<string> Scopes { get; set; }

    public static AuthConfig ReadFromJsonFile(string path)
    {
        IConfiguration Configuration;

        var builder = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile(path);

        Configuration = builder.Build();

        return Configuration.Get<AuthConfig>();
    }
}