## How to modify ASP.NET Core middleware response

I was using the SoapCore library (NuGet package here) to port some legacy SOAP web 
services (`.asmx`) to .NET7. I was trying to replace the error response after it has been generated
by the SoapCore middleware by injecting my custom middleware before the SoapCore's middleware.

I have been not researching the problem comprehensively :-( and thanks to the other SO question
and answers, I was able to implement the response replacement middleware correctly:

````
public async Task InvokeAsync(HttpContext httpContext)
{
    var originalBodyStream = httpContext.Response.Body;
    var newBodyStream = new MemoryStream();
    httpContext.Response.Body = newBodyStream;

    await this._next(httpContext);

    if (httpContext.Response.StatusCode != StatusCodes.Status200OK)
    {
        // Now we can completely replace the response here thanks to https://stackoverflow.com/questions/44508028/modify-middleware-response :-)

        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes("Custom error response");
        newBodyStream = new MemoryStream(responseBytes);

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        httpContext.Response.ContentType = "text/plain";
        httpContext.Response.ContentLength = responseBytes.Length;
        httpContext.Response.Headers.Add("X-Custom-Response", DateTime.Now.Ticks.ToString());
    }

    newBodyStream.Seek(0, SeekOrigin.Begin);
    await newBodyStream.CopyToAsync(originalBodyStream);
    httpContext.Response.Body = originalBodyStream;
}
````

I have updated the minimal `net7.0` sample is here: https://github.com/PaloMraz/SoapCoreMiddlewareTestApp
