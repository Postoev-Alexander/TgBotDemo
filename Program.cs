using System;
using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgBotDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Bot starting");
            var botClient = new TelegramBotClient("6408930022:AAFPLT5Onn9IXfTZONpu3G8ktldCovduapo");
            botClient.StartReceiving(HandleUpdateBot, HandleErrorBot);
            Console.WriteLine("Bot started");
            Console.ReadLine();
        }
        private static async Task HandleUpdateBot(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var msg = update.Message;
            if (msg?.Text != null)
            {
                if (msg.Text.Contains("/start"))
                {
                    await botClient.SendTextMessageAsync(msg.Chat.Id, $"Бот умеет расчитывать гипотенузу прямоугольного треугольники по двум катетам. " +
                        $"Можно вводить дробные значения.Для дробных значений необходимо использовать запятую. Для расчета введите два положительных числа через пробел");
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
                Console.WriteLine($"Гипотинуза = {hypotenuse}");

                await botClient.SendTextMessageAsync(msg.Chat.Id, $"Длина гипотенузы = {hypotenuse}");
            }
        }
        private static async Task<LegParseData> CheckNumber(string number, string serial, ITelegramBotClient botClient, ChatId id)
        {
            CultureInfo ruCulture = new CultureInfo("ru-RU");
            if (!float.TryParse(number, ruCulture, out float leg))
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
        private static Task HandleErrorBot(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine($"Bot error \n {exception}");
            return Task.CompletedTask;
        }
    }
}

