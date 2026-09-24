using System.Text.Json;
using Shared.Models;
using Shared.Models.Enums;

namespace Backend.Services
{
    public record MeetingRequestDto(int ClientId, int AdvisorId, DateTime MeetingDate, string? Notes);
    public record MeetingResponseDto(bool Accept);
    public record MeetingSummary(int MeetingId, int ClientId, int AdvisorId, string ClientName, DateTime MeetingDate, string Status, string? Notes, DateTime CreatedAt);

    public class MeetingService
    {
        private readonly string _meetingsPath;
        private readonly string _clientsPath;

        public MeetingService(IWebHostEnvironment env)
        {
            var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _meetingsPath = Path.Combine(dataDirectory, "meetings.json");
            _clientsPath = Path.Combine(dataDirectory, "clients.json");
        }

        public async Task<MeetingSummary> CreateRequestAsync(MeetingRequestDto request)
        {
            var meetings = await ReadAsync<Meeting>(_meetingsPath);
            var clients = await ReadAsync<Client>(_clientsPath);
            var client = clients.FirstOrDefault(c => c.ClientId == request.ClientId);

            var meeting = new Meeting
            {
                MeetingId = meetings.Count == 0 ? 1 : meetings.Max(m => m.MeetingId) + 1,
                ClientId = request.ClientId,
                MeetingDate = request.MeetingDate,
                Notes = request.Notes,
                Status = MeetingStatus.Requested,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            meetings.Add(meeting);
            await WriteAsync(_meetingsPath, meetings);

            return ToSummary(meeting, client, request.AdvisorId);
        }

        public async Task<(MeetingSummary? Meeting, string? Error)> RespondAsync(int meetingId, bool accept)
        {
            var meetings = await ReadAsync<Meeting>(_meetingsPath);
            var meeting = meetings.FirstOrDefault(m => m.MeetingId == meetingId);
            if (meeting is null)
            {
                return (null, "Meeting request not found.");
            }

            meeting.Status = accept ? MeetingStatus.Confirmed : MeetingStatus.Cancelled;
            meeting.UpdatedAt = DateTime.UtcNow;
            await WriteAsync(_meetingsPath, meetings);

            var clients = await ReadAsync<Client>(_clientsPath);
            var client = clients.FirstOrDefault(c => c.ClientId == meeting.ClientId);

            return (ToSummary(meeting, client, client?.AdvisorId ?? 0), null);
        }

        public async Task<List<MeetingSummary>> GetForClientAsync(int clientId)
        {
            var meetings = await ReadAsync<Meeting>(_meetingsPath);
            var clients = await ReadAsync<Client>(_clientsPath);
            var client = clients.FirstOrDefault(c => c.ClientId == clientId);

            return meetings
                .Where(m => m.ClientId == clientId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => ToSummary(m, client, client?.AdvisorId ?? 0))
                .ToList();
        }

        public async Task<List<MeetingSummary>> GetForAdvisorAsync(int advisorId)
        {
            var meetings = await ReadAsync<Meeting>(_meetingsPath);
            var clients = await ReadAsync<Client>(_clientsPath);
            var clientIds = clients.Where(c => c.AdvisorId == advisorId).Select(c => c.ClientId).ToHashSet();

            return meetings
                .Where(m => clientIds.Contains(m.ClientId))
                .OrderBy(m => m.MeetingDate)
                .Select(m =>
                {
                    var client = clients.FirstOrDefault(c => c.ClientId == m.ClientId);
                    return ToSummary(m, client, advisorId);
                })
                .ToList();
        }

        private static MeetingSummary ToSummary(Meeting meeting, Client? client, int advisorId)
        {
            return new MeetingSummary(
                meeting.MeetingId,
                meeting.ClientId,
                advisorId,
                client?.FullName ?? "Client",
                meeting.MeetingDate,
                meeting.Status.ToString(),
                meeting.Notes,
                meeting.CreatedAt);
        }

        private static async Task<List<T>> ReadAsync<T>(string path)
        {
            if (!File.Exists(path))
            {
                return new List<T>();
            }

            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<List<T>>(stream);
            return data ?? new List<T>();
        }

        private static async Task WriteAsync<T>(string path, List<T> data)
        {
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
