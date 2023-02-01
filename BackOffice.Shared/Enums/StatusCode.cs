namespace Backoffice.Shared.Enums
{
    public enum StatusCode
    {
        Success = 100,
        Unauthorized = 401,
        Missing_Or_Unsupported_APIKey_In_Request_Headers = 101,
        Rejected_Bet_Are_Not_Allowed_To_Be_Resent_To_Operator = 150,
        Resend_Attempt_Not_Successful_Please_Try_Again = 151,
        Error = 1000
    }
}
