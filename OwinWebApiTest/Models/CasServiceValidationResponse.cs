namespace OwinWebApiTest.Models
{
    public class CasServiceValidationResponse
    {
        public CasServiceValidationSuccess success { get; set; }
        public CasServiceValidationFailure failure { get; set; }
    }

    public class CasServiceValidationSuccess
    {
        public string user { get; set; }
        public string proxyGrantingTicket { get; set; }
    }

    public class CasServiceValidationFailure
    {
        public string code { get; set; }
        public string description { get; set; }
    }
}