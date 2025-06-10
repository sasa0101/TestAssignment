using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ReqResClient.Core.Models;
using ReqResClient.Core.Options;
using ReqResClient.Core.Services;
using System.Net;
using System.Net.Http.Json;

public class ExternalUserServiceTests
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IOptions<ReqResClientOptions> _options = Options.Create(new ReqResClientOptions
    {
        BaseUrl = "https://reqres.in/api/",
        CacheDurationSeconds = 5
    });
    private readonly ILogger<ExternalUserService> _logger = NullLogger<ExternalUserService>.Instance;

    private HttpClient CreateHttpClientWithResponse(HttpResponseMessage responseMessage)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://reqres.in/api/")
        };
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsUser_WhenUserExists()
    {
        var userDto = new UserListApiResponse.UserDto
        {
            Id = 2,
            Email = "janet.weaver@reqres.in",
            FirstName = "Janet",
            LastName = "Weaver",
            Avatar = "https://reqres.in/img/faces/2-image.jpg"
        };

        var apiResponse = new SingleUserApiResponse { Data = userDto };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(apiResponse)
        };

        var httpClient = CreateHttpClientWithResponse(response);
        var service = new ExternalUserService(httpClient, _logger, _cache, _options);

        var user = await service.GetUserByIdAsync(2);

        user.Should().NotBeNull();
        user!.Id.Should().Be(2);
        user.Email.Should().Be("janet.weaver@reqres.in");
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsNull_WhenUserNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        var httpClient = CreateHttpClientWithResponse(response);
        var service = new ExternalUserService(httpClient, _logger, _cache, _options);

        var user = await service.GetUserByIdAsync(999);

        user.Should().BeNull();
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUsersAcrossPages()
    {
        var userDtoPage1 = new UserListApiResponse.UserDto
        {
            Id = 1,
            Email = "george.bluth@reqres.in",
            FirstName = "George",
            LastName = "Bluth",
            Avatar = "https://reqres.in/img/faces/1-image.jpg"
        };

        var userDtoPage2 = new UserListApiResponse.UserDto
        {
            Id = 2,
            Email = "janet.weaver@reqres.in",
            FirstName = "Janet",
            LastName = "Weaver",
            Avatar = "https://reqres.in/img/faces/2-image.jpg"
        };

        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                if (request.RequestUri!.ToString().Contains("page=1"))
                {
                    var page1Response = new UserListApiResponse
                    {
                        Page = 1,
                        PerPage = 2,
                        Total = 4,
                        TotalPages = 2,
                        Data = new List<UserListApiResponse.UserDto> { userDtoPage1 }
                    };

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(page1Response)
                    };
                }
                else if (request.RequestUri!.ToString().Contains("page=2"))
                {
                    var page2Response = new UserListApiResponse
                    {
                        Page = 2,
                        PerPage = 2,
                        Total = 4,
                        TotalPages = 2,
                        Data = new List<UserListApiResponse.UserDto> { userDtoPage2 }
                    };

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(page2Response)
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://reqres.in/api/")
        };

        var service = new ExternalUserService(httpClient, _logger, _cache, _options);

        var users = await service.GetAllUsersAsync();

        users.Should().HaveCount(2);
        users.Should().Contain(u => u.Id == 1);
        users.Should().Contain(u => u.Id == 2);
    }

}
