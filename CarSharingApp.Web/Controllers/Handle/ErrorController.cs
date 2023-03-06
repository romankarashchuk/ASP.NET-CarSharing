﻿using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CarSharingApp.Web.Controllers.Handle
{
    public class ErrorController : Controller
    {
        [Route("error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var statusCodeResult = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            switch (statusCode)
            {
                case 400:
                    ViewBag.ErrorDescription = "Wrong HTTP request was sent to the server or has invalid syntax";
                    return View("BadRequest");

                case 401:
                    ViewBag.ErrorDescription = "Access is allowed only for registered users";
                    return View("Unauthorized");

                case 404:
                    ViewBag.ErrorDescription = "Sorry but the page you are looking for does not exist, have been removed, name changed or is temporarily unavailable";
                    return View("NotFound");

                default:
                    break;
            }

            return View();
        }
    }
}
