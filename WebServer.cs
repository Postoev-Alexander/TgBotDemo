<<<<<<< HEAD
﻿    using Microsoft.AspNetCore.Hosting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Telegram.Bot;

    namespace TgBotDemo
    {
        public class WebServer
        {
            private readonly TelegramBotClient _botClient;
            private readonly Subscriber _subscriber;

            public WebServer(TelegramBotClient botClient, Subscriber subscriber)
            {
                _botClient = botClient;
                _subscriber = subscriber;
            }
            public WebServer() 
            {
                var host = new WebHostBuilder()
                        .UseKestrel()
                        .UseUrls("http://*:5000", "http://*:5001")
                        .ConfigureServices(services => services.AddSingleton(_botClient))
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
=======
﻿using Microsoft.AspNetCore.Hosting;
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
    public struct TeamCityBuildInfo
    {
        public string BuildNumber { get; set; }
        public string BuildDate { get; set; }
        public string BranchName { get; set; }
        public string GoogleBuildsDir { get; set; }
    }
    public class WebServer
    {
        private readonly TelegramBotClient _botClient;
        private readonly SubscriberList _subscriber;
        private const string _defaultPortListening = "http://*:5000";

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
                .UseUrls(GetPortListening())
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
                var payload = JsonConvert.DeserializeObject<TeamCityBuildInfo>(requestBody);   

                var message = $"Build №{payload.BuildNumber} {payload.BuildDate} {payload.BranchName}.\n" +
                              $"Другие сборки можно скачать c Google диска ({payload.GoogleBuildsDir})";
                await SendMessageToSubscribers(message);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("Message received from TeamCity and sent to Telegram.");
            }
        }

        private static string GetPortListening()
        {
         //   string? environmentPort = "PORT_LISTENING".ReadEnvironmentNullable();
           // if (var env_var != null)
             //   return env_var;

            return _defaultPortListening;
        }
        private async Task SendMessageToSubscribers(string message) 
        {
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
        } 
    }
}
>>>>>>> notify
