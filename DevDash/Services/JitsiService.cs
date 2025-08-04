using DevDash.DTO.Integrations.Jitsi;
using DevDash.Services.IService;
using Microsoft.Extensions.Options;

namespace DevDash.Services
{
    public class JitsiService :IJitsiService
    {

        public Task<MeetingResponseDTO> CreateMeetingAsync(string userEmail, string displayName)
        {
            var roomName = "room_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var meetingUrl = $"https://meet.jit.si/{roomName}";

            return Task.FromResult(new MeetingResponseDTO
            {
                RoomName = roomName,
                MeetingUrl = meetingUrl
            });
        }
    }
}
