using Telegram.Bot.Types;

public class SubscriberList
{
    private readonly HashSet<(ChatId ChatId, int? ThreadId)> _subscriptions = new();
    public void AddSubscription(ChatId chatId, int? threadId)
    {
        _subscriptions.Add((chatId, threadId));
    }
    public void RemoveSubscription(ChatId chatId, int? threadId)
    {
        _subscriptions.Remove((chatId, threadId));
    }
    public IEnumerable<(ChatId ChatId, int? ThreadId)> GetSubscribedChats()
    {
        return _subscriptions;
    }
}

