using KMG.Core.DTOs.Notification;

namespace KMG.Core.Interfaces.Services
{
    public interface INotificationService
    {
        Task<List<NotificationDTO>> GetActiveAsync();
        Task MarkReadAsync(int id);
        Task MarkAllReadAsync();
        Task DismissAsync(int id);
    }
}
