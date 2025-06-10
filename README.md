# ReqResClient

## Overview

This solution interacts with the public [ReqRes.in API](https://reqres.in/) to fetch user data.

Features:
- Uses `HttpClient` with `IHttpClientFactory`
- Async calls with `async/await`
- Proper error handling with custom exceptions
- Handles pagination for user lists
- In-memory caching with configurable expiration
- Retry logic using Polly
- Configurable via options pattern (`appsettings.json`)
- Unit tested using xUnit, Moq, and FluentAssertions
- Simple console app demo included

## Projects

- `ReqResClient.Core`: Core library with API client and service
- `ReqResClient.Tests`: Unit tests for service layer
- `ReqResClient.ConsoleApp`: Console app demo using the core library

## How to run

1. Build the solution:

```bash
dotnet build
