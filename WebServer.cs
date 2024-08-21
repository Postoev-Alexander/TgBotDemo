using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Telegram.Bot;
using System;

namespace TgBotDemo
{
    public class WebServer
    {
        private readonly TelegramBotClient _botClient;
        private readonly SubscriberList _subscriber;
        private const string _portListeningA = "http://*:5000";
        private const string _portListeningB = "http://*:5001";

        public WebServer(TelegramBotClient botClient, SubscriberList subscriber)
        {
            _botClient = botClient;
            _subscriber = subscriber;
        }
        public void Start()
        {
            Task.Run(() => BuildAndRun());
        }

        private void BuildAndRun()
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(_portListeningA, _portListeningB)
                .ConfigureServices(services => services.AddSingleton(_botClient))
                .Configure(app => app.Run(async context =>
                {
                    if (context.Request.Method == "POST")
                    {
                        await HandlePostRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    }
                }))
                .Build();

            host.Run();
        }

        private async Task HandlePostRequest(HttpContext context)
        {
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                var requestBody = await reader.ReadToEndAsync();
                Console.WriteLine($"Received a message from TeamCity: {requestBody}");
                var payload = JsonConvert.DeserializeObject<dynamic>(requestBody);
                // Извлечение данных
                var buildNumber = (string)payload.build_number;
                var buildDate = (string)payload.build_date;
                var branchName = (string)payload.branch_name;
                var googleBuildsDir = (string)payload.google_builds_dir;
                // Формирование сообщения
                var message = $"Build №{buildNumber} {buildDate} {branchName}.\n" +
                              $"Другие сборки можно скачать c Google диска ({googleBuildsDir})";
                // Отправка сообщения от TeamCity всем подписанным пользователям и группам
                var subscribedChats = _subscriber.GetSubscribedChats();
                foreach (var (chatId, threadId) in subscribedChats)
                {
                    Console.WriteLine($"Send message to ChatID {chatId} and topic {threadId}");
                    if (threadId.HasValue)
                    {
                        await _botClient.SendTextMessageAsync(chatId, message, messageThreadId: threadId.Value);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(chatId, message);
                    }
                }
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("Message received from TeamCity and sent to Telegram.");
            }
        }
    }
}
