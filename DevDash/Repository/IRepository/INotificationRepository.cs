using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface INotificationRepository
    {
        Task SendNotificationAsync(int userId, string message, int? issueId);
        Task<List<Notification>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
    }
}
