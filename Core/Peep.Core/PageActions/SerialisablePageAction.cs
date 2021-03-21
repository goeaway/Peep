namespace Peep.Core.PageActions
{
    public class SerialisablePageAction : IPageAction
    {
        public string UriRegex { get; set; }
        public object Value { get; set; }
        public SerialisablePageActionType Type { get; set; }
    }
}
