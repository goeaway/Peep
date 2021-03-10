namespace Peep.API.Models.Entities
{
    public class JobError
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
    }
}