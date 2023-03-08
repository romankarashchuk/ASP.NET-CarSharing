﻿using CarSharingApp.Application.Contracts.Payment;
using CarSharingApp.PublicApi.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace CarSharingApp.PublicApi.Controllers
{
    public sealed class PaymentsController : ApiController
    {
        private const string StripePaymentProcessImage = "https://www.hotellinksolutions.com/images/learning-center/payment-101.jpg";

        public PaymentsController(IConfiguration configuration)
        {
            StripeConfiguration.ApiKey = configuration["StripeConfig:SecretKey"];
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GenerateStripeSession(StripePaymentSessionUrlRequest payment)
        {
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = int.Parse(payment.Amount) * 100,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order for {payment.VehicleName}",
                                Images = new List<string> { StripePaymentProcessImage },
                                Description = $"You are going to pay for the use of the «{payment.VehicleName}» transport within the specified period: " +
                                $"from {payment.StartMonth} {payment.StartDay}, {payment.StartHour}:00 till {payment.EndMonth} {payment.EndDay}, {payment.EndHour}:00. " +
                                $"For the overdue time, the customer is responsible to the vehicle's owner."
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = payment.SuccessUrl + "&sessionId={CHECKOUT_SESSION_ID}",
                CancelUrl = payment.CancelationUrl,
            };

            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = await service.CreateAsync(options);

            return Ok(MapStripePaymentSessionResponse(session));
        }

        [Authorize]
        [HttpGet("PaymentDetails")]
        public async Task<IActionResult> GetStripePaymentInfo(string sessionId)
        {
            var sessionService = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session getSessionResponse = await sessionService.GetAsync(sessionId);

            if (getSessionResponse is null)
                return NotFound(sessionId);

            var paymentIntentId = getSessionResponse.PaymentIntentId;

            var paymentService = new PaymentIntentService();
            var paymentIntent = paymentService.Get(paymentIntentId);

            if (paymentIntent is null)
                return NotFound(paymentIntent);

            return Ok(MapStripePaymentDetailsResponse(paymentIntent));
        }

        private StripePaymentSessionResponse MapStripePaymentSessionResponse(Stripe.Checkout.Session session)
        {
            return new StripePaymentSessionResponse(
                SessionUrl: session.Url);
        }

        private StripePaymentDetailsResponse MapStripePaymentDetailsResponse(PaymentIntent paymentIntent)
        {
            return new StripePaymentDetailsResponse(
                PaymentId: paymentIntent.Id,
                Amount: paymentIntent.Amount,
                PaymentDateTime: paymentIntent.Created);
        }
    }
}
