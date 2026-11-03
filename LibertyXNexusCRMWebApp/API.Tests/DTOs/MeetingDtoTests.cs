using System.Text.Json;
using Xunit;

namespace API.Tests.DTOs
{
    /// <summary>
    /// Checks the meeting JSON after the duplicate, misspelled MeetidId property was removed.
    /// </summary>
    public class MeetingDtoTests
    {
        /// <summary>
        /// Same settings ASP.NET Core uses for API responses (camelCase names).
        /// </summary>
        private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// The API's meeting JSON has "meetingId" and no longer has "meetidId".
        /// </summary>
        [Fact]
        public void ApiMeetingDto_SerializesMeetingId_AndNotTheTypo()
        {
            var json = JsonSerializer.Serialize(new API.DTOs.Meetings.MeetingDto { MeetingId = 42 }, WebJson);

            Assert.Contains("\"meetingId\":42", json);
            Assert.DoesNotContain("meetidId", json, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The frontend's copy of the DTO reads the API's JSON correctly.
        /// </summary>
        [Fact]
        public void SharedMeetingDto_ReadsMeetingIdFromApiJson()
        {
            var json = JsonSerializer.Serialize(new API.DTOs.Meetings.MeetingDto { MeetingId = 42, ClientId = 7 }, WebJson);

            var meeting = JsonSerializer.Deserialize<Shared.DTOs.Meetings.MeetingDto>(json, WebJson);

            Assert.NotNull(meeting);
            Assert.Equal(42, meeting!.MeetingId);
            Assert.Equal(7, meeting.ClientId);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
