namespace Peep.Core.PageActions
{
    public interface IPageAction
    {
        string UriRegex { get; set; }
        object Value { get; set; }
        SerialisablePageActionType Type { get; set; }
    }
}
