using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;

namespace BlazorClient.Models.Diagram
{

    public enum BotNodeType
    {
        Start,
        Message,
        Input,
        AiProcess,
        Condition
    }

    public class BotNodeModel : NodeModel
    {
        public BotNodeType Type { get; }

        public BotNodeModel(BotNodeType type, Point position = null) : base(position)
        {
            Type = type;
            Title = GetTitle(type);




            if (type != BotNodeType.Start)
            {
                AddPort(PortAlignment.Left);
            }

            if (type != BotNodeType.Input)
            {

                AddPort(PortAlignment.Right);
            }
            else
            {

                AddPort(PortAlignment.Right);
            }
        }

        private static string GetTitle(BotNodeType type) => type switch
        {
            BotNodeType.Start => "🚀 Start (Webhook)",
            BotNodeType.Message => "💬 Message",
            BotNodeType.Input => "👤 Wait for response",
            BotNodeType.AiProcess => "🤖 AI Processing",
            BotNodeType.Condition => "❓ Condition",
            _ => "Block"
        };
    }
}