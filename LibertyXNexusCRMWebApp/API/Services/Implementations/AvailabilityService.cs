using System.Globalization;
using API.DTOs.Availability;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using Shared.Models;
using Shared.Models.Enums;

namespace API.Services.Implementations
{
    /// <summary>
    /// The advisor's working hours.
    /// The advisor sets a default week (e.g. Monday to Friday, 08:00 to 17:00) and can add custom hours
    /// for a specific date, which replace the default for that date. Clients can only message the advisor
    /// during those hours, and can only book or move a meeting to a time inside them.
    /// Until the advisor saves their hours nothing is restricted. All times are South African time.
    /// </summary>
    public class AvailabilityService : IAvailabilityService
    {
        private const string TimeFormat = "HH:mm";
        private const string DateFormat = "yyyy-MM-dd";
        private const int MaxNoteLength = 200;

        // How far ahead to look for the advisor's next working day.
        private const int LookAheadDays = 14;

        // How far ahead custom hours can be set.
        private const int MaxOverrideDaysAhead = 366;

        private static readonly TimeOnly DefaultStart = new(8, 0);
        private static readonly TimeOnly DefaultEnd = new(17, 0);

        private static readonly DayOfWeek[] WeekOrder =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        // Meeting times are offered every half hour.
        private const int SlotStepMinutes = 30;

        private readonly IAdvisorHoursRepository hoursRepository_;
        private readonly IMeetingRepository meetingRepository_;
        private readonly TimeProvider clock_;

        public AvailabilityService(IAdvisorHoursRepository hoursRepository, IMeetingRepository meetingRepository, TimeProvider clock)
        {
            hoursRepository_ = hoursRepository;
            meetingRepository_ = meetingRepository;
            clock_ = clock;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the advisor's default week and their upcoming custom dates.
        /// </summary>
        public async Task<AdvisorScheduleDto> GetScheduleAsync(int advisorId)
        {
            var weekly = await hoursRepository_.GetWeeklyHoursAsync(advisorId) ?? new List<AdvisorWorkingHours>();
            var overrides = await hoursRepository_.GetOverridesFromAsync(advisorId, Today()) ?? new List<AdvisorHoursOverride>();

            return new AdvisorScheduleDto
            {
                IsConfigured = weekly.Count > 0,
                Days = BuildDays(weekly),
                Overrides = overrides.OrderBy(o => o.Date).Select(MapOverride).ToList()
            };
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Saves the advisor's default week. All seven days must be sent, and each working day needs
        /// a start time that is before its end time.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public async Task<AdvisorScheduleDto> SaveWeeklyHoursAsync(int advisorId, UpdateWeeklyHoursRequest request)
        {
            var days = request?.Days;

            if (days is null || days.Count != 7 || days.Select(d => d.Day).Distinct().Count() != 7 || days.Any(d => !Enum.IsDefined(d.Day)))
            {
                throw new ArgumentException("Send the hours for all seven days of the week.");
            }

            if (!days.Any(d => d.IsWorkingDay))
            {
                throw new ArgumentException("Choose at least one working day.");
            }

            var rows = new List<AdvisorWorkingHours>();

            foreach (var day in days)
            {
                var start = DefaultStart;
                var end = DefaultEnd;

                if (day.IsWorkingDay)
                {
                    (start, end) = ParseHours(day.StartTime, day.EndTime, day.Day.ToString());
                }
                else if (TryParseTime(day.StartTime, out var keptStart) && TryParseTime(day.EndTime, out var keptEnd) && keptStart < keptEnd)
                {
                    // Keep the times of a day off, so they are still there if the advisor turns the day back on.
                    start = keptStart;
                    end = keptEnd;
                }

                rows.Add(new AdvisorWorkingHours
                {
                    AdvisorId = advisorId,
                    DayOfWeek = day.Day,
                    IsWorkingDay = day.IsWorkingDay,
                    StartTime = start,
                    EndTime = end
                });
            }

            await hoursRepository_.SaveWeeklyHoursAsync(advisorId, rows);
            return await GetScheduleAsync(advisorId);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Saves custom hours for one date. They replace the default hours for that date.
        /// Saving the same date again replaces what was there.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public async Task<AdvisorScheduleDto> SaveOverrideAsync(int advisorId, SaveHoursOverrideRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var date = ParseDate(request.Date);
            var today = Today();

            if (date < today)
            {
                throw new ArgumentException("Custom hours can only be set for today or a future date.");
            }

            if (date > today.AddDays(MaxOverrideDaysAhead))
            {
                throw new ArgumentException("Custom hours can only be set up to a year ahead.");
            }

            var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

            if (note is not null && note.Length > MaxNoteLength)
            {
                throw new ArgumentException($"The note cannot be longer than {MaxNoteLength} characters.");
            }

            TimeOnly? start = null;
            TimeOnly? end = null;

            if (request.IsWorkingDay)
            {
                (start, end) = ParseHours(request.StartTime, request.EndTime, FormatDay(date));
            }

            await hoursRepository_.SaveOverrideAsync(new AdvisorHoursOverride
            {
                AdvisorId = advisorId,
                Date = date,
                IsWorkingDay = request.IsWorkingDay,
                StartTime = start,
                EndTime = end,
                Note = note,
                CreatedAt = clock_.GetUtcNow().UtcDateTime
            });

            return await GetScheduleAsync(advisorId);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Removes the custom hours for a date, so the default hours apply again. Returns false if there were none.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public Task<bool> DeleteOverrideAsync(int advisorId, string date)
        {
            return hoursRepository_.DeleteOverrideAsync(advisorId, ParseDate(date));
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Works out whether the advisor is in their working hours right now and, if not, when they are next back.
        /// </summary>
        public async Task<AdvisorAvailabilityDto> GetAvailabilityAsync(int advisorId)
        {
            var weekly = await hoursRepository_.GetWeeklyHoursAsync(advisorId) ?? new List<AdvisorWorkingHours>();
            var now = NowLocal();
            var today = DateOnly.FromDateTime(now);
            var time = TimeOnly.FromDateTime(now);

            var overrides = (await hoursRepository_.GetOverridesFromAsync(advisorId, today) ?? new List<AdvisorHoursOverride>())
                .GroupBy(o => o.Date)
                .ToDictionary(g => g.Key, g => g.First());

            var result = new AdvisorAvailabilityDto
            {
                IsConfigured = weekly.Count > 0,
                Days = weekly.Count > 0 ? BuildDays(weekly) : new List<DayHoursDto>()
            };

            var hours = Resolve(weekly, overrides.GetValueOrDefault(today), today);

            if (!hours.Restricted)
            {
                result.IsAvailableNow = true;
                return result;
            }

            if (hours.IsWorking && time >= hours.Start && time < hours.End)
            {
                result.IsAvailableNow = true;
                result.Message = $"Your advisor is available until {FormatTime(hours.End)} today.";
                return result;
            }

            result.IsAvailableNow = false;

            var next = FindNextOpening(weekly, overrides, today, time);
            result.Message = next is null
                ? "Your advisor is out of office right now."
                : $"Your advisor is out of office right now. They'll be back {next}.";

            return result;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Lists the times on a date when a meeting of this length can be booked: inside the advisor's hours
        /// for that day, still in the future, and not clashing with a meeting they already have.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public async Task<MeetingSlotsDto> GetOpenSlotsAsync(int advisorId, string date, int durationMinutes)
        {
            if (durationMinutes < 15 || durationMinutes > 480)
            {
                throw new ArgumentException("A meeting must be between 15 minutes and 8 hours long.");
            }

            var day = ParseDate(date);
            var nowLocal = NowLocal();
            var today = DateOnly.FromDateTime(nowLocal);

            if (day < today)
            {
                throw new ArgumentException("Choose today or a future date.");
            }

            if (day > today.AddDays(MaxOverrideDaysAhead))
            {
                throw new ArgumentException("Meetings can only be booked up to a year ahead.");
            }

            var weekly = await hoursRepository_.GetWeeklyHoursAsync(advisorId) ?? new List<AdvisorWorkingHours>();
            var hoursOverride = await hoursRepository_.GetOverrideAsync(advisorId, day);
            var hours = Resolve(weekly, hoursOverride, day);

            var result = new MeetingSlotsDto
            {
                IsConfigured = hours.Restricted,
                IsWorkingDay = hours.IsWorking
            };

            // No working hours set, so there is nothing to build times from. The client can ask for any time.
            if (!hours.Restricted)
            {
                return result;
            }

            if (!hours.IsWorking)
            {
                var reason = string.IsNullOrWhiteSpace(hours.Note) ? string.Empty : $" ({hours.Note})";
                result.Message = $"Your advisor is not working on {FormatDay(day)}{reason}. Please choose another day.";
                return result;
            }

            var dayStart = day.ToDateTime(hours.Start);
            var dayEnd = day.ToDateTime(hours.End);

            // The advisor's meetings around that day. Start looking a few hours early to catch one that runs into it.
            var booked = (await meetingRepository_.GetByDateRangeAsync(
                    DisplayTime.FromSouthAfrica(dayStart).AddHours(-8),
                    DisplayTime.FromSouthAfrica(dayEnd),
                    advisorId) ?? Enumerable.Empty<Meeting>())
                .Where(m => m.Status != MeetingStatus.Cancelled)
                .ToList();

            for (var start = dayStart; start.AddMinutes(durationMinutes) <= dayEnd; start = start.AddMinutes(SlotStepMinutes))
            {
                if (start <= nowLocal)
                {
                    continue;
                }

                var startUtc = DisplayTime.FromSouthAfrica(start);
                var endUtc = startUtc.AddMinutes(durationMinutes);

                var clashes = booked.Any(m => startUtc < m.MeetingDate.AddMinutes(m.DurationMinutes) && m.MeetingDate < endUtc);

                if (!clashes)
                {
                    result.Slots.Add(new MeetingSlotDto
                    {
                        StartUtc = startUtc,
                        Label = start.ToString(TimeFormat, CultureInfo.InvariantCulture)
                    });
                }
            }

            if (result.Slots.Count == 0)
            {
                result.Message = $"There are no open times left on {FormatDay(day)}. Please choose another day.";
            }

            return result;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Stops a client from messaging the advisor outside their working hours.
        /// </summary>
        /// <exception cref="AdvisorUnavailableException"></exception>
        public async Task EnsureCanMessageAsync(int advisorId)
        {
            var availability = await GetAvailabilityAsync(advisorId);

            if (!availability.IsAvailableNow)
            {
                throw new AdvisorUnavailableException($"{availability.Message} You can send your message once they are back.");
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Stops a client from booking or moving a meeting to a time outside the advisor's hours for that day.
        /// The whole meeting has to fit: it can't start before the advisor does or run past when they finish.
        /// </summary>
        /// <exception cref="AdvisorUnavailableException"></exception>
        public async Task EnsureMeetingWithinHoursAsync(int advisorId, DateTime meetingDate, int durationMinutes)
        {
            var start = DisplayTime.ToSouthAfrica(meetingDate);
            var end = start.AddMinutes(durationMinutes);
            var date = DateOnly.FromDateTime(start);

            var weekly = await hoursRepository_.GetWeeklyHoursAsync(advisorId) ?? new List<AdvisorWorkingHours>();
            var hoursOverride = await hoursRepository_.GetOverrideAsync(advisorId, date);
            var hours = Resolve(weekly, hoursOverride, date);

            if (!hours.Restricted)
            {
                return;
            }

            var day = FormatDay(date);

            if (!hours.IsWorking)
            {
                var reason = string.IsNullOrWhiteSpace(hours.Note) ? string.Empty : $" ({hours.Note})";
                throw new AdvisorUnavailableException($"Your advisor is not working on {day}{reason}. Please choose another day.");
            }

            var fitsInTheDay = end.Date == start.Date
                && TimeOnly.FromDateTime(start) >= hours.Start
                && TimeOnly.FromDateTime(end) <= hours.End;

            if (!fitsInTheDay)
            {
                throw new AdvisorUnavailableException(
                    $"Your advisor is available from {FormatTime(hours.Start)} to {FormatTime(hours.End)} on {day}. " +
                    "Please choose a time that starts and ends within those hours.");
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        // Helpers

        /// <summary>
        /// The hours that apply on one date. Restricted is false when the advisor hasn't set any hours,
        /// which means anything goes.
        /// </summary>
        private readonly record struct DayHours(bool Restricted, bool IsWorking, TimeOnly Start, TimeOnly End, string? Note);

        /// <summary>
        /// Custom hours for the date win. Otherwise the default for that day of the week applies.
        /// </summary>
        private static DayHours Resolve(IReadOnlyCollection<AdvisorWorkingHours> weekly, AdvisorHoursOverride? hoursOverride, DateOnly date)
        {
            if (hoursOverride is not null)
            {
                return hoursOverride.IsWorkingDay && hoursOverride.StartTime is TimeOnly start && hoursOverride.EndTime is TimeOnly end
                    ? new DayHours(true, true, start, end, hoursOverride.Note)
                    : new DayHours(true, false, default, default, hoursOverride.Note);
            }

            if (weekly.Count == 0)
            {
                return new DayHours(false, true, TimeOnly.MinValue, TimeOnly.MaxValue, null);
            }

            var row = weekly.FirstOrDefault(h => h.DayOfWeek == date.DayOfWeek);

            return row is not null && row.IsWorkingDay
                ? new DayHours(true, true, row.StartTime, row.EndTime, null)
                : new DayHours(true, false, default, default, null);
        }

        /// <summary>
        /// Finds when the advisor is next in, as words for the client: "today at 08:00",
        /// "tomorrow at 08:00" or "Monday 16 November at 08:00". Null if nothing is found in the next two weeks.
        /// </summary>
        private static string? FindNextOpening(IReadOnlyCollection<AdvisorWorkingHours> weekly, IReadOnlyDictionary<DateOnly, AdvisorHoursOverride> overrides, DateOnly today, TimeOnly now)
        {
            for (var offset = 0; offset <= LookAheadDays; offset++)
            {
                var date = today.AddDays(offset);
                var hours = Resolve(weekly, overrides.GetValueOrDefault(date), date);

                if (!hours.IsWorking)
                {
                    continue;
                }

                // Today only counts if the advisor hasn't started yet.
                if (offset == 0 && now >= hours.Start)
                {
                    continue;
                }

                var dayText = offset switch
                {
                    0 => "today",
                    1 => "tomorrow",
                    _ => $"on {FormatDay(date)}"
                };

                return hours.Restricted ? $"{dayText} at {FormatTime(hours.Start)}" : dayText;
            }

            return null;
        }

        /// <summary>
        /// The seven days in Monday-first order. Before the advisor has saved anything this is the
        /// suggested week: Monday to Friday 08:00 to 17:00, weekends off.
        /// </summary>
        private static List<DayHoursDto> BuildDays(IReadOnlyCollection<AdvisorWorkingHours> weekly)
        {
            return WeekOrder.Select(day =>
            {
                var row = weekly.FirstOrDefault(h => h.DayOfWeek == day);

                return new DayHoursDto
                {
                    Day = day,
                    IsWorkingDay = row?.IsWorkingDay ?? (day != DayOfWeek.Saturday && day != DayOfWeek.Sunday),
                    StartTime = FormatTime(row?.StartTime ?? DefaultStart),
                    EndTime = FormatTime(row?.EndTime ?? DefaultEnd)
                };
            }).ToList();
        }

        private static HoursOverrideDto MapOverride(AdvisorHoursOverride o) => new()
        {
            Date = o.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
            IsWorkingDay = o.IsWorkingDay,
            StartTime = o.StartTime is TimeOnly start ? FormatTime(start) : null,
            EndTime = o.EndTime is TimeOnly end ? FormatTime(end) : null,
            Note = o.Note
        };

        private static (TimeOnly Start, TimeOnly End) ParseHours(string? startText, string? endText, string dayName)
        {
            if (!TryParseTime(startText, out var start) || !TryParseTime(endText, out var end))
            {
                throw new ArgumentException($"Enter a start and end time for {dayName}.");
            }

            if (start >= end)
            {
                throw new ArgumentException($"The end time for {dayName} must be after the start time.");
            }

            return (start, end);
        }

        private static bool TryParseTime(string? text, out TimeOnly time)
        {
            return TimeOnly.TryParseExact(text?.Trim(), new[] { "HH:mm", "HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
        }

        private static DateOnly ParseDate(string? text)
        {
            if (!DateOnly.TryParseExact(text?.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                throw new ArgumentException("Choose a valid date.");
            }

            return date;
        }

        private static string FormatTime(TimeOnly time) => time.ToString(TimeFormat, CultureInfo.InvariantCulture);

        private static string FormatDay(DateOnly date) => date.ToString("dddd d MMMM", CultureInfo.InvariantCulture);

        private DateTime NowLocal() => DisplayTime.ToSouthAfrica(clock_.GetUtcNow().UtcDateTime);

        private DateOnly Today() => DateOnly.FromDateTime(NowLocal());
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
