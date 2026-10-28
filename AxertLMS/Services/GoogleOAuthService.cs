using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using AxertLMS.Models;

namespace AxertLMS.Services
{
    public sealed class GoogleOAuthService
    {
        public const string StateSessionKey =
            "AxertLMS.GoogleOAuth.State";

        public const string CodeVerifierSessionKey =
            "AxertLMS.GoogleOAuth.CodeVerifier";

        public const string IntentSessionKey =
            "AxertLMS.GoogleOAuth.Intent";

        public const string TimestampSessionKey =
            "AxertLMS.GoogleOAuth.Timestamp";

        public const string UserIdSessionKey =
            "AxertLMS.GoogleOAuth.UserId";

        public const string OnboardingUserIdSessionKey =
            "AxertLMS.StudentOnboarding.UserId";

        public const int StateLifetimeMinutes = 10;

        private static readonly string[] IdentityScopes =
        {
            "openid",
            "https://www.googleapis.com/auth/userinfo.email",
            "https://www.googleapis.com/auth/userinfo.profile"
        };

        public const string CalendarScope =
            "https://www.googleapis.com/auth/calendar";

        // Legacy scope retained for accounts that were authorized before
        // the application was changed to request the full Calendar scope.
        public const string LegacyCalendarEventsScope =
            "https://www.googleapis.com/auth/calendar.events";

        private static readonly string[] CalendarScopes =
        {
            CalendarScope
        };

        private readonly GoogleOAuthOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;

        public GoogleOAuthService(
            IOptions<GoogleOAuthOptions> options,
            IHttpClientFactory httpClientFactory)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        public string[] GetScopes(bool includeCalendar)
        {
            if (!includeCalendar)
            {
                return IdentityScopes;
            }

            var scopes = new string[
                IdentityScopes.Length + CalendarScopes.Length];

            Array.Copy(
                IdentityScopes,
                scopes,
                IdentityScopes.Length);

            Array.Copy(
                CalendarScopes,
                0,
                scopes,
                IdentityScopes.Length,
                CalendarScopes.Length);

            return scopes;
        }

        public string CreateAuthorizationUrl(
            string state,
            string codeVerifier,
            bool includeCalendar)
        {
            ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException(
                    "OAuth state is required.",
                    nameof(state));
            }

            if (string.IsNullOrWhiteSpace(codeVerifier))
            {
                throw new ArgumentException(
                    "OAuth code verifier is required.",
                    nameof(codeVerifier));
            }

            string codeChallenge =
                CreateCodeChallenge(codeVerifier);

            var query =
                new Dictionary<string, string?>
                {
                    ["client_id"] = _options.ClientId,
                    ["redirect_uri"] = _options.RedirectUri,
                    ["response_type"] = "code",
                    ["scope"] = string.Join(
                        " ",
                        GetScopes(includeCalendar)),
                    ["access_type"] = "offline",
                    ["prompt"] = "consent",
                    ["include_granted_scopes"] = "true",
                    ["state"] = state,
                    ["code_challenge"] = codeChallenge,
                    ["code_challenge_method"] = "S256"
                };

            return QueryHelpers.AddQueryString(
                "https://accounts.google.com/o/oauth2/v2/auth",
                query);
        }

        public async Task<AxertLMS.Models.GoogleTokenResponse> ExchangeCodeAsync(
            string code,
            string codeVerifier,
            CancellationToken cancellationToken)
        {
            ValidateConfiguration();

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://oauth2.googleapis.com/token");

            request.Content =
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["code"] = code,
                        ["client_id"] = _options.ClientId,
                        ["client_secret"] = _options.ClientSecret,
                        ["redirect_uri"] = _options.RedirectUri,
                        ["grant_type"] = "authorization_code",
                        ["code_verifier"] = codeVerifier
                    });

            var client =
                _httpClientFactory.CreateClient("Google");

            using var response =
                await client.SendAsync(
                    request,
                    cancellationToken);

            string body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Google token exchange failed with HTTP {(int)response.StatusCode}.");
            }

            var token =
                JsonSerializer.Deserialize<AxertLMS.Models.GoogleTokenResponse>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (token == null ||
                string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new InvalidOperationException(
                    "Google returned an invalid token response.");
            }

            return token;
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_options.ClientId))
            {
                throw new InvalidOperationException(
                    "GoogleOAuth:ClientId is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                throw new InvalidOperationException(
                    "GoogleOAuth:ClientSecret is not configured.");
            }

            if (!Uri.TryCreate(
                    _options.RedirectUri,
                    UriKind.Absolute,
                    out Uri? redirectUri) ||
                (redirectUri.Scheme != Uri.UriSchemeHttps &&
                 !redirectUri.IsLoopback))
            {
                throw new InvalidOperationException(
                    "GoogleOAuth:RedirectUri must be an absolute HTTPS URI, except for loopback development URLs.");
            }
        }

        public async Task<AxertLMS.Models.GoogleTokenResponse> RefreshAccessTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.ClientId) ||
                string.IsNullOrWhiteSpace(_options.ClientSecret))
            {
                throw new InvalidOperationException(
                    "Google OAuth client credentials are not configured.");
            }

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://oauth2.googleapis.com/token");

            request.Content =
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["client_id"] = _options.ClientId,
                        ["client_secret"] = _options.ClientSecret,
                        ["refresh_token"] = refreshToken,
                        ["grant_type"] = "refresh_token"
                    });

            var client =
                _httpClientFactory.CreateClient("Google");

            using var response =
                await client.SendAsync(
                    request,
                    cancellationToken);

            string body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Google token refresh failed with HTTP {(int)response.StatusCode}.");
            }

            var token =
                JsonSerializer.Deserialize<AxertLMS.Models.GoogleTokenResponse>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (token == null ||
                string.IsNullOrWhiteSpace(token.AccessToken))
            {
                throw new InvalidOperationException(
                    "Google returned an invalid refresh response.");
            }

            return token;
        }

        public async Task<GoogleUserInfo> GetUserInfoAsync(
            string accessToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException(
                    "An access token is required.",
                    nameof(accessToken));
            }

            var client =
                _httpClientFactory.CreateClient("Google");

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://openidconnect.googleapis.com/v1/userinfo");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            using var response =
                await client.SendAsync(
                    request,
                    cancellationToken);

            string body =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Google userinfo request failed with HTTP {(int)response.StatusCode}.");
            }

            var profile =
                JsonSerializer.Deserialize<GoogleUserInfo>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (profile == null)
            {
                throw new InvalidOperationException(
                    "Google returned an invalid user profile.");
            }

            return profile;
        }

        public static string GenerateState()
        {
            return Base64UrlEncode(
                RandomNumberGenerator.GetBytes(32));
        }

        public static string GenerateCodeVerifier()
        {
            return Base64UrlEncode(
                RandomNumberGenerator.GetBytes(64));
        }

        private static string CreateCodeChallenge(
            string codeVerifier)
        {
            byte[] hash =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(codeVerifier));

            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(
            byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
