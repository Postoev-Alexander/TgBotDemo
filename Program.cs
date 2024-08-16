using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Collections.Generic;

namespace TgBotDemo
{
    internal class Program
    {
        private static TelegramBotClient botClient;
        private static string userChatId = "1842171326"; // Замените на ваш chat_id пользователя
        private static ManualResetEvent resetEvent = new ManualResetEvent(false);
        private static readonly SubscriptionManager subscriptionManager = new SubscriptionManager();
        static void Main(string[] args)
        {
            Console.WriteLine("Bot starting");
            botClient = new TelegramBotClient("6408930022:AAFPLT5Onn9IXfTZONpu3G8ktldCovduapo");
            botClient.StartReceiving(HandleUpdateBot, HandleErrorBot);
            Console.WriteLine("Bot started");

            // Запуск веб-сервера
            Task.Run(() => StartWebServer());
            Console.WriteLine("WebServer started");
            resetEvent.WaitOne();
            //Console.ReadLine();
        }

        private static async Task HandleUpdateBot(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var msg = update.Message;

            if (msg?.Chat.Type == ChatType.Group || msg?.Chat.Type == ChatType.Supergroup || msg?.Chat.Type == ChatType.Channel)
            {
                Console.WriteLine($"Сообщение из группового чата. Chat.Id = {msg.Chat.Id}");
                // Обработка команды /start_notify_teamcity
                if (msg.Text.Contains("/start_notify_teamcity", StringComparison.OrdinalIgnoreCase)) 
                {
                    // Получение идентификатора чата и номера топика (если есть)
                    var chatId = msg.Chat.Id;
                    var threadId = msg.MessageThreadId;

                    // Подписка чата на уведомления TeamCity
                    subscriptionManager.AddSubscription(chatId, threadId);
                    Console.WriteLine($"Групповой чат {chatId} подписан на уведомления TeamCity с топиком {threadId}.");
                    await botClient.SendTextMessageAsync(chatId, "Групповой чат подписан на уведомления TeamCity.",
                        replyToMessageId: msg.MessageId);
                    return;
                }

                // Обработка команды /end_notify_teamcity
                if (msg.Text.Contains("/end_notify_teamcity", StringComparison.OrdinalIgnoreCase))
                    {
                    // Получение идентификатора чата и номера топика (если есть)
                    var chatId = msg.Chat.Id;
                    var threadId = msg.MessageThreadId;

                    // Отписка чата от уведомлений TeamCity
                    subscriptionManager.RemoveSubscription(chatId, threadId);
                    Console.WriteLine($"Групповой чат {chatId} отписан от уведомлений TeamCity с топиком {threadId}.");
                    await botClient.SendTextMessageAsync(chatId, "Групповой чат отписан от уведомлений TeamCity.",
                        replyToMessageId: msg.MessageId);
                    return;
                }

                // Возврат из метода, если сообщение не относится к командам

                return;
            }

            if (msg?.Text != null)
            {
                // Блок проверок команд
                switch (msg.Text)
                {
                    case "/start_notify_teamcity":
                        subscriptionManager.AddSubscription(msg.Chat.Id, null);
                        await botClient.SendTextMessageAsync(msg.Chat.Id, "Вы подписаны на уведомления TeamCity за 198$ в месяц");
                        return;
                    case "/end_notify_teamcity":
                        await botClient.SendTextMessageAsync(msg.Chat.Id, "Вы отписаны от уведомлений TeamCity за 398$ в месяц");
                        subscriptionManager.RemoveSubscription(msg.Chat.Id, null);
                        return;
                    case "/t1":
                        await botClient.SendTextMessageAsync(msg.Chat.Id, "Тест1");
                        Console.WriteLine("Тест1");
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

        private static void StartWebServer()
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5000", "http://*:5001")
                .ConfigureServices(services => services.AddSingleton(botClient))
                .Configure(app => app.Run(async context =>
                {
                    if (context.Request.Method == "POST")
                    {
                        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                        {
                            var requestBody = await reader.ReadToEndAsync();
                            Console.WriteLine($"Получено сообщение от TeamCity: {requestBody}");

                            // Парсинг JSON
                            var payload = JsonConvert.DeserializeObject<dynamic>(requestBody);

                            // Извлечение данных
                            var buildNumber = (string)payload.BUILD_NUMBER;
                            var buildDate = (string)payload.BUILD_DATE;
                            var branchName = (string)payload.BRANCH_NAME;
                            var googleBuildsDir = (string)payload.GOOGLE_BUILDS_DIR;

                            // Формирование сообщения
                            var message = $"Build №{buildNumber} {buildDate} {branchName}.\n" +
                                          $"Другие сборки можно скачать c Google диска ({googleBuildsDir})";

                            // Отправка сообщения от TeamCity всем подписанным пользователям и группам
                            var subscribedChats = subscriptionManager.GetSubscribedChats();
                            foreach (var (chatId, threadId) in subscribedChats)
                            {
                                Console.WriteLine($"Send message to chat {chatId} and tipic {threadId}");
                                if (threadId.HasValue)
                                {
                                    await botClient.SendTextMessageAsync(chatId, message, messageThreadId: threadId.Value);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId, message);
                                }
                            }

                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            await context.Response.WriteAsync("Message received and sent to Telegram.");
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    }
                }))
                .Build();

            host.Run();
        }
    }
}