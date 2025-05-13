namespace chatbot_api.Services
{
    public class TelegramService
    {
        public string BotToken { get; }

        public TelegramService(IConfiguration config)
        {
            BotToken = config["Telegram:BotToken"]!;
        }

        public string GetSendMessageUrl() =>
            $"https://api.telegram.org/bot{BotToken}/sendMessage";
    }
}
