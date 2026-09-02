namespace Backoffice.Shared.Models.Response
{
    public class ClientImportProgressResponse
    {
        public int TotalClients { get; set; }
        public int ProcessedClients { get; set; }
        public int SucceededClients { get; set; }
        public int FailedClients { get; set; }
        public bool IsComplete { get; set; }
    }
}
