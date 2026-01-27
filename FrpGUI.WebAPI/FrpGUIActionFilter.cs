using FrpGUI.Configs;
using FrpGUI.WebAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;
using FzLib.Net;

namespace FrpGUI.WebAPI;

public class FrpGUIActionFilter(AppConfig config) : IActionFilter
{
    private readonly AppConfig config = config;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var token = config.Token;
        var authorizeAttribute = context.Controller.GetType().GetCustomAttribute<NeedTokenAttribute>();
        if (authorizeAttribute != null)
        {
            var headers = context.HttpContext.Request.Headers;
            if (headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues value))
            {
                if (value != token && value != "Bearer " + token)
                {
                    context.Result = new UnauthorizedObjectResult("登陆密钥不正确");
                }
            }
            else
            {
                context.Result = new UnauthorizedObjectResult("敏感操作，未登录");
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception != null)
        {
            if (context.Exception is HttpStatusCodeException sbe)
            {
                if (string.IsNullOrEmpty(sbe.Message))
                {
                    context.Result = new StatusCodeResult((int)sbe.StatusCode);
                }
                else
                {
                    context.Result = new ObjectResult(sbe.Message) { StatusCode = (int)sbe.StatusCode };
                }

                context.ExceptionHandled = true;
            }
            else
            {
                if (!string.IsNullOrEmpty(context.Exception.Message))
                {
                    context.Result = new ObjectResult(context.Exception.Message) { StatusCode = 500 };
                    context.ExceptionHandled = true;
                }

                context.ExceptionHandled = true;
            }
        }
    }
}