using System.Threading.Tasks;

namespace StockNotifications.Clients.Interfaces
{
    public interface ISlackClient
    {
        Task SendMessageViaWebhook(string webhookUrl, string senderName, string messageText);
    }
}
