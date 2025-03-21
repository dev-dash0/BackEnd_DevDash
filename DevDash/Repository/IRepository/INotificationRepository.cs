using DevDash.model;

namespace DevDash.Repository.IRepository
{
    public interface INotificationRepository
    {
        Task SendNotificationAsync(string userId, string message);
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
    }
}
