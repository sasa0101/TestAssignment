using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReqResClient.Core.Models;
using ReqResClient.Core.Options;
using System.Net;
using System.Net.Http.Json;
using Polly;
using Polly.Retry;

namespace ReqResClient.Core.Services;

public class ExternalUserService : IExternalUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalUserService> _logger;
    private readonly IMemoryCache _cache;
    private readonly ReqResClientOptions _options;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public ExternalUserService(
        HttpClient httpClient,
        ILogger<ExternalUserService> logger,
        IMemoryCache cache,
        IOptions<ReqResClientOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _options = options.Value;

        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    _logger.LogWarning("Retry {RetryAttempt} after {Delay} due to {Reason}",
                        retryAttempt, timespan, outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
                });
    }

    public async Task<User> GetUserByIdAsync(int userId)
    {
        string cacheKey = $"user_{userId}";
        if (_cache.TryGetValue<User>(cacheKey, out var cachedUser))
        {
            return cachedUser;
        }

        var url = $"users/{userId}";
        HttpResponseMessage response;

        try
        {
            response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId} from API", userId);
            throw new ExternalUserServiceException("Failed to get user from API", ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("User {UserId} not found.", userId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogError("API error for user {UserId}: {StatusCode} {Content}", userId, response.StatusCode, content);
            throw new ExternalUserServiceException($"API error: {response.StatusCode}");
        }

        SingleUserApiResponse? apiResponse;
        try
        {
            apiResponse = await response.Content.ReadFromJsonAsync<SingleUserApiResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize user {UserId}", userId);
            throw new ExternalUserServiceException("Failed to deserialize API response", ex);
        }

        if (apiResponse == null)
        {
            _logger.LogError("API returned empty response for user {UserId}", userId);
            return null;
        }

        var user = MapUserDtoToUser(apiResponse.Data);
        _cache.Set(cacheKey, user, TimeSpan.FromSeconds(_options.CacheDurationSeconds));
        return user;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        string cacheKey = "all_users";
        if (_cache.TryGetValue<IEnumerable<User>>(cacheKey, out var cachedUsers))
        {
            return cachedUsers;
        }

        var users = new List<User>();
        int page = 1;

        while (true)
        {
            HttpResponseMessage response;
            try
            {
                response = await _retryPolicy.ExecuteAsync(() => _httpClient.GetAsync($"users?page={page}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users page {Page}", page);
                throw new ExternalUserServiceException("Failed to get users from API", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError("API error getting users page {Page}: {StatusCode} {Content}", page, response.StatusCode, content);
                throw new ExternalUserServiceException($"API error: {response.StatusCode}");
            }

            UserListApiResponse? apiResponse;
            try
            {
                apiResponse = await response.Content.ReadFromJsonAsync<UserListApiResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize users page {Page}", page);
                throw new ExternalUserServiceException("Failed to deserialize API response", ex);
            }

            if (apiResponse == null)
            {
                break;
            }

            users.AddRange(apiResponse.Data.Select(MapUserDtoToUser));

            if (page >= apiResponse.TotalPages)
                break;

            page++;
        }

        _cache.Set(cacheKey, users, TimeSpan.FromSeconds(_options.CacheDurationSeconds));
        return users;
    }

    private static User MapUserDtoToUser(UserListApiResponse.UserDto dto)
        => new()
        {
            Id = dto.Id,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Avatar = dto.Avatar
        };
}

public class ExternalUserServiceException : Exception
{
    public ExternalUserServiceException(string message) : base(message) { }
    public ExternalUserServiceException(string message, Exception inner) : base(message, inner) { }
}
