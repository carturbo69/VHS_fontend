using VHS_frontend.Models.Payment;

namespace VHS_frontend.Services.Customer.Interfaces
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context,string overrideReturnUrl);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}

