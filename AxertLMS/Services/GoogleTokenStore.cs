using System;
using System.Security.Cryptography;
using AxertLMS.Data;
using AxertLMS.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AxertLMS.Services
{
    public sealed class GoogleTokenStore
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;
        private readonly GoogleOAuthService _oauth;
        private readonly ILogger<GoogleTokenStore> _logger;

        public GoogleTokenStore(
            ApplicationDbContext context,
            IDataProtectionProvider dataProtectionProvider,
            GoogleOAuthService oauth,
            ILogger<GoogleTokenStore> logger)
        {
            _context = context;
            _protector =
                dataProtectionProvider.CreateProtector(
                    "AxertLMS.GoogleOAuth.Tokens.v1");
            _oauth = oauth;
            _logger = logger;
        }

        public async Task<GoogleAccount?> GetByUserIdAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            return await _context.GoogleAccounts
                .SingleOrDefaultAsync(
                    g => g.UserId == userId,
                    cancellationToken);
        }

        public async Task<GoogleAccount?> GetBySubjectAsync(
            string subject,
            CancellationToken cancellationToken)
        {
            return await _context.GoogleAccounts
                .Include(g => g.User)
                .SingleOrDefaultAsync(
                    g => g.GoogleSubject == subject,
                    cancellationToken);
        }

        public async Task<GoogleAccount?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken)
        {
            return await _context.GoogleAccounts
                .Include(g => g.User)
                .SingleOrDefaultAsync(
                    g => g.GoogleEmail == email,
                    cancellationToken);
        }

        public async Task<GoogleAccount> SaveAsync(
            int userId,
            GoogleUserInfo profile,
            AxertLMS.Models.GoogleTokenResponse token,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(profile.Subject))
            {
                throw new InvalidOperationException(
                    "Google identity did not contain a subject.");
            }

            if (string.IsNullOrWhiteSpace(profile.Email))
            {
                throw new InvalidOperationException(
                    "Google identity did not contain an email address.");
            }

            string normalizedEmail =
                profile.Email.Trim().ToLowerInvariant();

            var user =
                await _context.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        u => u.user_id == userId,
                        cancellationToken);

            if (user == null)
            {
                throw new InvalidOperationException(
                    "The AxertLMS user account could not be found.");
            }

            string enrollmentStatus =
                string.Equals(
                    user.status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Pending"
                    : "Approved";

            var account =
                await _context.GoogleAccounts
                    .SingleOrDefaultAsync(
                        g => g.UserId == userId,
                        cancellationToken);

            var subjectAccount =
                await _context.GoogleAccounts
                    .SingleOrDefaultAsync(
                        g => g.GoogleSubject == profile.Subject,
                        cancellationToken);

            if (subjectAccount != null &&
                subjectAccount.UserId != userId)
            {
                throw new InvalidOperationException(
                    "This Google account is already linked to another AxertLMS account.");
            }

            if (account == null)
            {
                account =
                    subjectAccount ??
                    new GoogleAccount
                    {
                        UserId = userId,
                        GoogleSubject = profile.Subject,
                        GoogleEmail = normalizedEmail,
                        GrantedScopes = token.Scope ?? string.Empty,
                        DisplayName = profile.Name,
                        ProfilePictureUrl = profile.Picture,
                        UpdatedUtc = DateTime.UtcNow
                    };

                _context.GoogleAccounts.Add(account);
            }
            else
            {
                if (!string.Equals(
                        account.GoogleSubject,
                        profile.Subject,
                        StringComparison.Ordinal))
                {
                    if (subjectAccount != null &&
                        subjectAccount.UserId != userId)
                    {
                        throw new InvalidOperationException(
                            "This Google account is already linked to another AxertLMS account.");
                    }

                    account.GoogleSubject = profile.Subject;
                }

                account.GoogleEmail = normalizedEmail;
                account.GrantedScopes =
                    string.IsNullOrWhiteSpace(token.Scope)
                        ? account.GrantedScopes
                        : token.Scope;

                account.DisplayName = profile.Name;
                account.ProfilePictureUrl = profile.Picture;
            }

            if (!string.IsNullOrWhiteSpace(token.AccessToken))
            {
                account.EncryptedAccessToken =
                    _protector.Protect(token.AccessToken);

                account.AccessTokenExpiresUtc =
                    DateTime.UtcNow.AddSeconds(
                        Math.Max(token.ExpiresIn, 0));
            }

            if (!string.IsNullOrWhiteSpace(token.RefreshToken))
            {
                account.EncryptedRefreshToken =
                    _protector.Protect(token.RefreshToken);

                if (token.RefreshTokenExpiresIn.HasValue)
                {
                    account.RefreshTokenExpiresUtc =
                        DateTime.UtcNow.AddSeconds(
                            token.RefreshTokenExpiresIn.Value);
                }
            }

            account.EnrollmentStatus = enrollmentStatus;
            account.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return account;
        }

        public async Task<string?> GetValidAccessTokenAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var account =
                await GetByUserIdAsync(
                    userId,
                    cancellationToken);

            if (account == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(account.EncryptedAccessToken) &&
                account.AccessTokenExpiresUtc.HasValue &&
                account.AccessTokenExpiresUtc.Value >
                    DateTime.UtcNow.AddMinutes(1))
            {
                try
                {
                    return _protector.Unprotect(
                        account.EncryptedAccessToken);
                }
                catch (CryptographicException ex)
                {
                    _logger.LogError(
                        ex,
                        "GOOGLE_TOKEN_DECRYPTION_FAILURE for UserId={UserId}",
                        userId);
                    return null;
                }
            }

            if (string.IsNullOrWhiteSpace(
                    account.EncryptedRefreshToken))
            {
                return null;
            }

            if (account.RefreshTokenExpiresUtc.HasValue &&
                account.RefreshTokenExpiresUtc.Value <= DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "GOOGLE_REFRESH_TOKEN_EXPIRED for UserId={UserId}",
                    userId);
                return null;
            }

            string refreshToken;

            try
            {
                refreshToken =
                    _protector.Unprotect(
                        account.EncryptedRefreshToken);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(
                    ex,
                    "GOOGLE_REFRESH_TOKEN_DECRYPTION_FAILURE for UserId={UserId}",
                    userId);
                return null;
            }

            try
            {
                var refreshed =
                    await _oauth.RefreshAccessTokenAsync(
                        refreshToken,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(
                        refreshed.AccessToken))
                {
                    return null;
                }

                account.EncryptedAccessToken =
                    _protector.Protect(
                        refreshed.AccessToken);

                account.AccessTokenExpiresUtc =
                    DateTime.UtcNow.AddSeconds(
                        Math.Max(refreshed.ExpiresIn, 0));

                if (!string.IsNullOrWhiteSpace(
                        refreshed.RefreshToken))
                {
                    account.EncryptedRefreshToken =
                        _protector.Protect(
                            refreshed.RefreshToken);
                }

                if (refreshed.RefreshTokenExpiresIn.HasValue)
                {
                    account.RefreshTokenExpiresUtc =
                        DateTime.UtcNow.AddSeconds(
                            refreshed.RefreshTokenExpiresIn.Value);
                }

                account.UpdatedUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync(
                    cancellationToken);

                return refreshed.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "GOOGLE_TOKEN_REFRESH_FAILURE for UserId={UserId}",
                    userId);
                return null;
            }
        }
    }
}
