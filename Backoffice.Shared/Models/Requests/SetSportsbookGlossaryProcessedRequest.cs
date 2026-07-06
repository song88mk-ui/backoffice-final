namespace Backoffice.Shared.Models.Requests
{
    public class SetSportsbookGlossaryProcessedRequest
    {
        public long Id { get; set; }

        // TranslationCache2.PROCESSED — false (0) puts the row back on the external translation
        // pipeline's worklist; true (1) marks it handled so the pipeline skips it.
        public bool Processed { get; set; }
    }
}
