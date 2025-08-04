using DevDash.DTO.Integrations.Jitsi;

namespace DevDash.Services.IService
{
    public interface IJitsiService
    {
        Task<MeetingResponseDTO> CreateMeetingAsync(string userEmail, string displayName);
    }
}
