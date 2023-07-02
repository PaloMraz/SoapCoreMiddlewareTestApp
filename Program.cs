using System.ServiceModel;
using SoapCore;

namespace SoapCoreMiddlewareTestApp;

public class Program
{
  public static void Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);
    builder.Services
      .AddSoapCore()
      .AddHttpContextAccessor()
      .AddScoped<TestService>();

    var app = builder.Build();
    app
      .UseMiddleware<TestMiddleware>()
      .UseSoapEndpoint<TestService>(
        path: "/test.asmx",
        encoder: new SoapEncoderOptions(),
        serializer: SoapSerializer.XmlSerializer,
        caseInsensitivePath: true,
        soapModelBounder: null,
        wsdlFileOptions: null,
        indentXml: true,
        omitXmlDeclaration: false);

    app.Run();
  }
}


public class TestMiddleware
{
  private readonly RequestDelegate _next;

  public TestMiddleware(RequestDelegate next)
  {
    this._next = next;
  }

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
}


[ServiceContract(Namespace = "http://example.com/test")]
public class TestService
{
  [OperationContract]
  public Task<TestData> GetTestDataAsync() => throw new InvalidOperationException();  
}


public record TestData()
{
  public string Name { get; set; } = "";
  public DateTime Timestamp { get; set; }
}