using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Telegram.Bot;
using static System.Net.Mime.MediaTypeNames;

namespace TgBotDemo
{
	public struct TeamCityBuildInfo
	{
		public string BuildNumber { get; set; }
		public string BuildDate { get; set; }
		public string BranchName { get; set; }
		public string BuildDescription { get; set; }
		public string GoogleBuildsDir { get; set; }
	}

	public class WebListener
	{
		private readonly TelegramBotClient _botClient;
		private readonly SubscriberList _subscriber;
		private const string _defaultPortListening = "5000";
		public const string TEAMCITY_PORT_LISTENING_ENVIRONMENT = "TEAMCITY_PORT_LISTENING_ENVIRONMENT";

		public WebListener(TelegramBotClient botClient, SubscriberList subscriber)
		{
			_botClient = botClient;
			_subscriber = subscriber;
		}
		public void Start()
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
			Task.Run(host.Run);
		}

		private async Task HandlePostRequest(HttpContext context)
		{
			using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
			{
				var requestBody = await reader.ReadToEndAsync();
				//Application.Log($"Received a message from TeamCity: {requestBody}");
				var payload = JsonConvert.DeserializeObject<TeamCityBuildInfo>(requestBody);

				var message = $"Build №{payload.BuildNumber}, {payload.BuildDate}, {payload.BranchName}, {payload.BuildDescription}.\n" +
							  $"Другие сборки можно скачать c Google диска ({payload.GoogleBuildsDir})";
				await SendMessageToSubscribers(message);

				context.Response.StatusCode = (int)HttpStatusCode.OK;
				await context.Response.WriteAsync("Message received from TeamCity and sent to Telegram.");
			}
		}

		private static string GetPortListening()
		{
			string? envPort = Environment.GetEnvironmentVariable(TEAMCITY_PORT_LISTENING_ENVIRONMENT);
			if (!string.IsNullOrEmpty(envPort))
				return $"http://*:{envPort}";

			return $"http://*:{_defaultPortListening}";
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

