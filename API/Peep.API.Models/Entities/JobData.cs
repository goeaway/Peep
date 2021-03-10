namespace Peep.API.Models.Entities
{
    public class JobData
    {
        public int Id { get; set; }
        public string Source { get; set; }
        public string Value { get; set; }
        
        public string JobId { get; set; }
        public Job Job { get; set; }
    }
}