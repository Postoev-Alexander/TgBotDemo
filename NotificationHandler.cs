using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgBotDemo
{
    public class NotificationHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly SubscriberList _subscriber;

		private const string startNotifyTeamCityCommand = "/start_notify_teamcity";
		private const string endNotifyTeamCityCommand = "/end_notify_teamcity";

		public NotificationHandler(TelegramBotClient botClient, SubscriberList subscriber)
        {
            _botClient = botClient;
            _subscriber = subscriber;
        }

        public async Task<bool> HandleAsync(Message msg)
        {
            if (NotificationHandler.IsGroupChat(msg))
            {
                Console.WriteLine($"Сообщение из группового чата. Chat.Id = {msg.Chat.Title}");

				if (msg.Text == null) 
					return false;
				switch (msg.Text)
				{
					case string text when text.Contains(startNotifyTeamCityCommand, StringComparison.OrdinalIgnoreCase):
						await TryAddSubscriptionAsync(msg);
						return true;
					case string text when text.Contains(endNotifyTeamCityCommand, StringComparison.OrdinalIgnoreCase):
						await TryRemoveSubscriptionAsync(msg);
						return true;
					default:
						return false;
				}

			}
            return false;
        }

        public static bool IsGroupChat(Message msg)
        {
            return msg?.Chat.Type == ChatType.Group || msg?.Chat.Type == ChatType.Supergroup || msg?.Chat.Type == ChatType.Channel;
        }

        public async Task<bool> TryAddSubscriptionAsync(Message msg)
        {
           // if (msg.Text == null) return false;
           // if (msg.Text.Contains("/start_notify_teamcity", StringComparison.OrdinalIgnoreCase))
           //{
                _subscriber.AddSubscription(msg.Chat.Id, msg.MessageThreadId);
                Console.WriteLine($"Групповой чат: {msg.Chat.Title} подписан на уведомления TeamCity с топиком {msg.MessageThreadId}.");
                await _botClient.SendTextMessageAsync(msg.Chat.Id, "Групповой чат подписан на уведомления TeamCity.",
                    replyToMessageId: msg.MessageId);
                return true;
           // }
           // return false;
        }

        public async Task<bool> TryRemoveSubscriptionAsync(Message msg)
        {
            //if (msg.Text == null) return false;
            //if (msg.Text.Contains("/end_notify_teamcity", StringComparison.OrdinalIgnoreCase))
            //{
                _subscriber.RemoveSubscription(msg.Chat.Id, msg.MessageThreadId);
                Console.WriteLine($"Групповой чат: {msg.Chat.Title} отписан от уведомлений TeamCity с топиком {msg.MessageThreadId}.");
                await _botClient.SendTextMessageAsync(msg.Chat.Id, "Групповой чат отписан от уведомлений TeamCity.",
                    replyToMessageId: msg.MessageId);
                return true;
            //}
            //return false;
        }
    }
}
