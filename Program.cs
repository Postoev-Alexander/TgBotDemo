using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgBotDemo
{
	internal class Program
    {
        private static TelegramBotClient botClient;
        private static string userChatId = "1842171326"; // Замените на ваш chat_id пользователя
        private static ManualResetEvent resetEvent = new ManualResetEvent(false);
        public static readonly SubscriberList subscriberList = new SubscriberList();
        private static  NotificationHandler groupChatCommandHandler = null!;
 
        static void Main(string[] args)
        {
            Console.WriteLine("Bot starting");
            botClient = new TelegramBotClient("6408930022:AAFPLT5Onn9IXfTZONpu3G8ktldCovduapo");
            groupChatCommandHandler = new NotificationHandler(botClient, subscriberList);
            botClient.StartReceiving(HandleUpdateBot, HandleErrorBot);
            Console.WriteLine("Bot started");

            // Запуск веб-сервера
            var webServer = new WebListener(botClient, subscriberList);
            webServer.Start();

            // Task.Run(() => StartWebServer());
            Console.WriteLine("WebServer started");
            resetEvent.WaitOne();
            //Console.ReadLine();
        }

        private static async Task HandleUpdateBot(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var msg = update.Message;
            if (msg?.Text is not { } messageText || msg.From == null)
                return;

            if (await groupChatCommandHandler.HandleAsync(msg)) return;



            if (msg?.Text != null)
            {
                // Блок проверок команд
                switch (msg.Text)
                {
                    case "/start_notify_teamcity":
                        subscriberList.AddSubscription(msg.Chat.Id, null);
                        await botClient.SendTextMessageAsync(msg.Chat.Id, "Вы подписаны на уведомления TeamCity за 198$ в месяц");
                        return;
                    case "/end_notify_teamcity":
                        await botClient.SendTextMessageAsync(msg.Chat.Id, "Вы отписаны от уведомлений TeamCity за 398$ в месяц");
                        subscriberList.RemoveSubscription(msg.Chat.Id, null);
                        return;
                    case "/ls":
                        var subscribedChats = subscriberList.GetSubscribedChats();
                        foreach (var subscribedChat in subscribedChats)
                        {
                            await botClient.SendTextMessageAsync(msg.Chat.Id, $"Чат: {subscribedChat.ChatId} тема: {subscribedChat.ThreadId} ");
                        }
                        Console.WriteLine("Отправил список подписок");
                        return;
                    case "/t2":
                        await botClient.SendTextMessageAsync(msg.Chat.Id, "Тест2");
                        Console.WriteLine("Тест2");
                        return;
                }

                if (msg.Text.Contains("teamcity", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Сообщение от TeamCity пришло");
                }

                // Попытка десериализовать текст сообщения в JSON
                try
                {
                    var payload = JsonConvert.DeserializeObject<JObject>(msg.Text);

                    if (payload != null && payload.ContainsKey("source"))
                    {
                        var source = payload["source"].ToString();
                        if (source.ToLower() == "teamcity")
                        {
                            Console.WriteLine($"Перед обработкой \n {payload}");
                            await HandleTeamCityMessage(botClient, userChatId, payload);
                            return;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Ошибка десериализации JSON: {ex.Message}");
                }

                // Начало расчета гипотенузы
                if (msg.Text.Contains("/start"))
                {
                    await botClient.SendTextMessageAsync(msg.Chat.Id, $"Бот умеет рассчитывать гипотенузу прямоугольного треугольника по двум катетам. " +
                        $"Можно вводить дробные значения. Для дробных значений необходимо использовать запятую. Для расчета введите два положительных числа через пробел");
                    return;
                }

                string[] numbers = msg.Text.Split(' ');

                if (numbers.Length != 2)
                {
                    await botClient.SendTextMessageAsync(msg.Chat.Id, $"Нужно ввести 2 положительных числа через пробел, значений введено {numbers.Length}");
                    return;
                }

                LegParseData firstLeg = await CheckNumber(numbers[0], "Первое", botClient, msg.Chat.Id);
                if (!firstLeg.IsValid) return;

                LegParseData secondLeg = await CheckNumber(numbers[1], "Второе", botClient, msg.Chat.Id);
                if (!secondLeg.IsValid) return;

                double hypotenuse = Math.Sqrt(firstLeg.Value * firstLeg.Value + secondLeg.Value * secondLeg.Value);
                Console.WriteLine($"Гипотенуза = {hypotenuse}");

                await botClient.SendTextMessageAsync(msg.Chat.Id, $"Длина гипотенузы = {hypotenuse}");
            }
        }

        private static async Task<LegParseData> CheckNumber(string number, string serial, ITelegramBotClient botClient, ChatId id)
        {
            CultureInfo ruCulture = new CultureInfo("ru-RU");
            if (!float.TryParse(number, NumberStyles.Float, ruCulture, out float leg))
            {
                await botClient.SendTextMessageAsync(id, $"{serial} значение не число. Нужно ввести 2 положительных числа через пробел");
                return new LegParseData { IsValid = false };
            }

            if (leg <= 0)
            {
                await botClient.SendTextMessageAsync(id, $"{serial} значение не положительное число. Нужно ввести 2 положительных числа через пробел");
                return new LegParseData { IsValid = false };
            }
            return new LegParseData { IsValid = true, Value = leg };
        }

        private struct LegParseData
        {
            public bool IsValid;
            public float Value;
        }

        private static async Task HandleTeamCityMessage(ITelegramBotClient botClient, ChatId chatId, JObject payload)
        {
            Console.WriteLine("Обработчик сообщений запущен");
            var response = "Получены данные от TeamCity:\n";
            response += $"Source: {payload["source"]}\n";

            foreach (var property in payload.Properties().Where(p => p.Name != "source"))
            {
                response += $"{property.Name}: {property.Value}\n";
            }

            await botClient.SendTextMessageAsync(chatId, response);
        }

        private static Task HandleErrorBot(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine($"Bot error \n {exception}");
            return Task.CompletedTask;
        }
    }
}