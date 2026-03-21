namespace FixItNepal.Services
{
    public interface IESewaService
    {
        /// <summary>
        /// Generates the eSewa V2 signature for a payment request.
        /// </summary>
        string GenerateSignature(decimal totalAmount, string transactionUuid, string productCode);

        /// <summary>
        /// Verifies an eSewa payment response after redirection.
        /// </summary>
        Task<bool> VerifyPaymentAsync(string data);
    }
}
