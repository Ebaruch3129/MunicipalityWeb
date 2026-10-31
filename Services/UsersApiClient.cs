using System.Net;
using System.Net.Http.Json;
using Model;

namespace MunicipalityWeb.Services;

public sealed class UsersApiClient(IHttpClientFactory httpClientFactory)
{
    public async Task<ApiResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new User(email.Trim(), email.Trim(), password);
        using var response = await httpClientFactory.CreateClient("MunicipalityApi")
            .PostAsJsonAsync("api/Users", user, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Success("החשבון נוצר בהצלחה. אפשר להתחבר ולהתחיל לדווח.");

        return response.StatusCode switch
        {
            HttpStatusCode.Conflict => ApiResult.Failure("כתובת המייל כבר רשומה במערכת. אפשר להתחבר לחשבון הקיים."),
            HttpStatusCode.BadRequest => ApiResult.Failure("לא ניתן ליצור את החשבון עם הפרטים האלה. נסו שוב."),
            _ => ApiResult.Failure("לא הצלחנו ליצור חשבון כרגע. בדקו את החיבור ונסו שוב.")
        };
    }

    public async Task<ApiResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var credentials = new User { Username = email.Trim(), Password = password };
        using var response = await httpClientFactory.CreateClient("MunicipalityApi")
            .PostAsJsonAsync("api/Users/login", credentials, cancellationToken);

        return response.IsSuccessStatusCode
            ? ApiResult.Success("התחברתם בהצלחה. שמחים שחזרתם לעיר בקשר.")
            : response.StatusCode == HttpStatusCode.Unauthorized
                ? ApiResult.Failure("כתובת המייל או הסיסמה אינם נכונים. בדקו את הפרטים ונסו שוב.")
                : ApiResult.Failure("לא הצלחנו להתחבר כרגע. בדקו את החיבור ונסו שוב.");
    }
}

public sealed record ApiResult(bool Succeeded, string Message)
{
    public static ApiResult Success(string message) => new(true, message);
    public static ApiResult Failure(string message) => new(false, message);
}
