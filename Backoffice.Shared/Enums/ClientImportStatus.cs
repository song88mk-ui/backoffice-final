namespace Backoffice.Shared.Enums
{
    public static class ClientImportBatchStatus
    {
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string PartiallySuccessful = "PartiallySuccessful";
        public const string Failed = "Failed";
        // The API process stopped mid-batch (restart, crash); rows still Pending were not done.
        public const string Interrupted = "Interrupted";
    }

    public static class ClientImportRowStatus
    {
        public const string Pending = "Pending";
        public const string Succeeded = "Succeeded";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";
    }

    public static class ClientImportOperationType
    {
        public const string Create = "Create";
        public const string Update = "Update";
    }

    public static class ClientCurrencyAction
    {
        public const string Remove = "Remove";
    }
}
