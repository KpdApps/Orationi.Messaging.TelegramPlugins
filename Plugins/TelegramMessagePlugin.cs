using KpdApps.Orationi.Messaging.Sdk;
using KpdApps.Orationi.Messaging.Sdk.Plugins;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KpdApps.Orationi.Messaging.TelegramPlugins
{
    public class TelegramMessagePlugin : IPipelinePlugin
    {
        const string TelegramBotApiToken = "TelegramBot.ApiToken";

        const string SettingsKeyChatId = "ChatId";

        const string TelegramMessageKey = "TelegramMessages";

        const string _MessageContractUri = "KpdApps.Orationi.Messaging.TelegramPlugins.Contracts.TelegramMessage.TelegramMessageRequest.xsd";

        const string _RequestContractUri = "KpdApps.Orationi.Messaging.TelegramPlugins.Contracts.TelegramMessage.TelegramMessageRequest.xsd";

        const string _ResponseContractUri = "KpdApps.Orationi.Messaging.TelegramPlugins.Contracts.TelegramMessage.TelegramMessageResponse.xsd";

        public string[] GlobalSettingsList => new string[] { TelegramBotApiToken };

        public string[] LocalSettingsList => new string[] { SettingsKeyChatId };

        public TelegramMessagePlugin(IPipelineExecutionContext context)
        {
            _context = context;
        }

        private IPipelineExecutionContext _context;
        public IPipelineExecutionContext Context => _context;

        public string RequestContractUri => _RequestContractUri;
        public string ResponseContractUri => _ResponseContractUri;
        public string MessageContractUri => _MessageContractUri;

        private bool _isInitialised = false;
        private long _chatId;
        private string[] _messages;
        private string _apiToken;

        public void BeforeExecution()
        {
            if (!_context.WorkflowExecutionContext.GlobalSettings.Contains(TelegramBotApiToken))
            {
                return;
            }
            _apiToken = _context.WorkflowExecutionContext.GlobalSettings[TelegramBotApiToken].ToString();

            if (!string.IsNullOrEmpty(Context.RequestBody))
            {
                if (XsdValidator.TryValidateXml(_context.RequestBody, new[] { RequestContractUri }, this.GetType()))
                {
                    TelegramMessageRequest request = TelegramMessageRequest.Deserialize(Context.RequestBody);
                    if (request != null)
                    {
                        _chatId = request.ChatId;
                        _messages = new[] { request.Message };
                        _isInitialised = true;
                        return;
                    }
                };
            }

            if (!_isInitialised && !_context.PluginStepSettings.Contains(SettingsKeyChatId))
            {
                return;
            }

            _chatId = (long)_context.PluginStepSettings[SettingsKeyChatId];

            if (!_context.PipelineValues.Contains(TelegramMessageKey))
            {
                return;
            }

            _messages = (_context.PipelineValues[TelegramMessageKey] as List<string>).ToArray();
            _isInitialised = true;
        }

        public void Execute()
        {
            if (!_isInitialised)
            {
                return;
            }

            Telegram.Bot.TelegramBotClient client = new Telegram.Bot.TelegramBotClient(_apiToken);
            Task.Run(async () =>
            {
                foreach (string messageText in _messages)
                {
                    Telegram.Bot.Types.Message message = await client.SendTextMessageAsync(_chatId, messageText);
                }
            }).Wait();
        }

        public void AfterExecution()
        {

        }
    }
}
