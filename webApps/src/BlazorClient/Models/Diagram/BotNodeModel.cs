using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;

namespace BlazorClient.Models.Diagram
{
    // Типы блоков вашего оркестратора
    public enum BotNodeType
    {
        Start,
        Message,    // Отправить текст
        Input,      // Ждать ответа
        AiProcess,  // Обработка через ChatGPT
        Condition   // If/Else
    }

    public class BotNodeModel : NodeModel
    {
        public BotNodeType Type { get; }

        public BotNodeModel(BotNodeType type, Point position = null) : base(position)
        {
            Type = type;
            Title = GetTitle(type);

            // Настраиваем Порты (точки соединения) в зависимости от типа
            // PortAlignment.Left = Вход, Right = Выход
            
            if (type != BotNodeType.Start)
            {
                AddPort(PortAlignment.Left); // Вход есть у всех, кроме Старта
            }

            if (type != BotNodeType.Input) 
            {
                // У обычных блоков 1 выход
                AddPort(PortAlignment.Right); 
            }
            else
            {
                // У блока "Вопрос" может быть выхода (ветвление пока упростим)
                AddPort(PortAlignment.Right);
            }
        }

        private static string GetTitle(BotNodeType type) => type switch
        {
            BotNodeType.Start => "🚀 Старт (Webhook)",
            BotNodeType.Message => "💬 Сообщение",
            BotNodeType.Input => "👤 Ждать ответ",
            BotNodeType.AiProcess => "🤖 AI Обработка",
            BotNodeType.Condition => "❓ Условие",
            _ => "Блок"
        };
    }
}