namespace Peep.API.Models.Entities
{
    public class CompletedJobData
    {
        public int Id { get; set; }
        public string Source { get; set; }
        public string Value { get; set; }
        
        public string CompletedJobId { get; set; }
        public CompletedJob CompletedJob { get; set; }
    }
}