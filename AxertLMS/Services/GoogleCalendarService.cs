using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AxertLMS.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace AxertLMS.Services
{
    public sealed class GoogleCalendarService
    {
        private readonly GoogleTokenStore _tokenStore;
        private readonly GoogleOAuthOptions _options;

        public GoogleCalendarService(
            GoogleTokenStore tokenStore,
            IOptions<GoogleOAuthOptions> options)
        {
            _tokenStore = tokenStore;
            _options = options.Value;
        }

        public async Task<bool> HasCalendarAccessAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var account =
                await _tokenStore.GetByUserIdAsync(
                    userId,
                    cancellationToken);

            if (account == null ||
                string.IsNullOrWhiteSpace(account.GrantedScopes))
            {
                return false;
            }

            var scopes =
                account.GrantedScopes
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

            return scopes.Any(
                scope =>
                    string.Equals(
                        scope,
                        GoogleOAuthService.CalendarScope,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        scope,
                        GoogleOAuthService.LegacyCalendarEventsScope,
                        StringComparison.Ordinal));
        }

        private async Task<CalendarService?>
            CreateCalendarClientAsync(
                int userId,
                CancellationToken cancellationToken)
        {
            string? accessToken =
                await _tokenStore.GetValidAccessTokenAsync(
                    userId,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var credential =
                GoogleCredential.FromAccessToken(
                    accessToken);

            return new CalendarService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "AxertLMS"
                });
        }

        public async Task<IReadOnlyList<GoogleCalendarEventViewModel>>
            GetUpcomingEventsAsync(
                int userId,
                int maxResults,
                CancellationToken cancellationToken)
        {
            if (!await HasCalendarAccessAsync(
                    userId,
                    cancellationToken))
            {
                return Array.Empty<GoogleCalendarEventViewModel>();
            }

            var calendar =
                await CreateCalendarClientAsync(
                    userId,
                    cancellationToken);

            if (calendar == null)
            {
                return Array.Empty<GoogleCalendarEventViewModel>();
            }

            var request =
                calendar.Events.List("primary");

            request.TimeMinDateTimeOffset =
                DateTimeOffset.UtcNow;

            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy =
                EventsResource.ListRequest.OrderByEnum.StartTime;

            request.MaxResults =
                Math.Clamp(maxResults, 1, 100);

            var result =
                await request.ExecuteAsync(
                    cancellationToken);

            var events =
                new List<GoogleCalendarEventViewModel>();

            foreach (
                var item
                in result.Items ?? new List<Event>())
            {
                events.Add(
                    new GoogleCalendarEventViewModel
                    {
                        Id = item.Id ?? string.Empty,
                        Title = item.Summary ?? "(Untitled event)",
                        Description = item.Description ?? string.Empty,
                        Location = item.Location ?? string.Empty,
                        Start = item.Start?.DateTimeDateTimeOffset,
                        End = item.End?.DateTimeDateTimeOffset,
                        AllDayStartDate = item.Start?.Date,
                        HtmlLink = item.HtmlLink
                    });
            }

            return events;
        }

        public Task<Event?> CreateEventAsync(
            int userId,
            CreateGoogleEventViewModel model,
            CancellationToken cancellationToken)
        {
            return CreateEventAsync(
                userId,
                model.Title,
                model.Description,
                model.Location,
                model.Start,
                model.End,
                cancellationToken);
        }

        public async Task<Event?> CreateEventAsync(
            int userId,
            string title,
            string? description,
            string? location,
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken)
        {
            if (!await HasCalendarAccessAsync(
                    userId,
                    cancellationToken))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "Calendar event title is required.",
                    nameof(title));
            }

            if (end <= start)
            {
                throw new ArgumentException(
                    "Calendar event end time must be later than the start time.",
                    nameof(end));
            }

            var calendar =
                await CreateCalendarClientAsync(
                    userId,
                    cancellationToken);

            if (calendar == null)
            {
                return null;
            }

            TimeZoneInfo timezone =
                GetTimeZone(_options.DefaultTimeZone);

            DateTime localStart =
                DateTime.SpecifyKind(
                    start,
                    DateTimeKind.Unspecified);

            DateTime localEnd =
                DateTime.SpecifyKind(
                    end,
                    DateTimeKind.Unspecified);

            TimeSpan startOffset =
                timezone.GetUtcOffset(localStart);

            TimeSpan endOffset =
                timezone.GetUtcOffset(localEnd);

            var startValue =
                new DateTimeOffset(
                    localStart,
                    startOffset);

            var endValue =
                new DateTimeOffset(
                    localEnd,
                    endOffset);

            var calendarEvent =
                new Event
                {
                    Summary = title.Trim(),
                    Description = description?.Trim(),
                    Location = location?.Trim(),
                    Start = new EventDateTime
                    {
                        DateTimeDateTimeOffset = startValue,
                        TimeZone = _options.DefaultTimeZone
                    },
                    End = new EventDateTime
                    {
                        DateTimeDateTimeOffset = endValue,
                        TimeZone = _options.DefaultTimeZone
                    }
                };

            var insertRequest =
                calendar.Events.Insert(
                    calendarEvent,
                    "primary");

            return await insertRequest.ExecuteAsync(
                cancellationToken);
        }

        private static TimeZoneInfo GetTimeZone(
            string timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                return TimeZoneInfo.Utc;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                if (
                    OperatingSystem.IsWindows() &&
                    timeZoneId == "Asia/Manila")
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(
                        "Singapore Standard Time");
                }

                if (
                    !OperatingSystem.IsWindows() &&
                    timeZoneId == "Singapore Standard Time")
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(
                        "Asia/Manila");
                }

                throw;
            }
        }
    }
}
