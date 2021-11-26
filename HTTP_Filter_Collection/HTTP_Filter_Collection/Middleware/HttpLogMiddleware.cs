using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HTTP_Filter_Collection.Middleware
{
    public class HttpLogMiddleware
    {
        private readonly RequestDelegate _next;

        public HttpLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var originalBodyStrem = context.Response.Body;
            try
            {
                string requestContent = await FromRequest(context.Request);

                try
                {
                    using (var newResponseBody = new MemoryStream())
                    {
                        context.Response.Body = newResponseBody;

                        await _next(context);
                        //throw new Exception("123");
                        string responseContent = await FromResponse(newResponseBody, originalBodyStrem);
                        //throw new Exception("123");

                        var Request = $"Request- Path: {context.Request.Path}, Method: {context.Request.Method}, QueryString: {context.Request.QueryString},  Body: {requestContent}";
                        var Response = $"Response - {context.Response.StatusCode}, Body: {responseContent}";
                    }
                }
                catch (Exception ex)
                {
                    //例外測試 當響應開始時無法修改 ContentType 跟 StatusCode
                    context.Response.Body = new MemoryStream();
                    //context.Response.ContentType = "application/json";
                    //context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync($"{context.Response.StatusCode} : {ex}");
                }
            }
            catch (Exception ex)
            {
                await ExceptionAsync(context, ex);
            }

        }

        private static Task ExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            return context.Response.WriteAsync($"{context.Response.StatusCode} : {exception}");
        }


        private async Task<string> FromResponse(MemoryStream newBodyStrem, Stream originalBodyStrem)
        {
            string responseContent;
            newBodyStrem.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(newBodyStrem,
                encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
            {
                responseContent = await reader.ReadToEndAsync();
                newBodyStrem.Seek(0, SeekOrigin.Begin);

                await newBodyStrem.CopyToAsync(originalBodyStrem);
            }

            return responseContent;
        }

        private static async Task<string> FromRequest(HttpRequest request)
        {
            string requestContent;
            request.EnableBuffering();
            using (var reader = new StreamReader(request.Body,
                 encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
            {
                requestContent = await reader.ReadToEndAsync();
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            return requestContent;
        }
    }
}
