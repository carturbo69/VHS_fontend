using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VHS_frontend.Models.Payment;
using VHS_frontend.Services.Customer.Interfaces;

namespace VHS_frontend.Areas.Customer.Controllers
{

    [ApiController]
    [Route("api/mobile/payment")]

    //Quite Hacking but it's whatever it is
    //also note to self... should have wrote the vnpay flow back in BE
    public class MobilePaymentController : ControllerBase
    {
        private readonly IVnPayService _vnPayService;

        public MobilePaymentController(IVnPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        [HttpPost("create-url")]
        public IActionResult CreatePaymentUrl([FromBody] PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
            return Ok(new { paymentUrl = url });
        }
    }

}