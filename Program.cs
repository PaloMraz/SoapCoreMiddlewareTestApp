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
    httpContext.Response.Headers.Add("X-Test-Header-Before", $"{DateTime.Now.Ticks}");
    await this._next(httpContext);
    // Here, the response is already sent and manipulating it has no effect whatsoever!
    httpContext.Response.Headers.Add("X-Test-Header-After", $"{DateTime.Now.Ticks}");
  }
}


[ServiceContract(Namespace = "http://example.com/test")]
public class TestService
{
  [OperationContract]
  public async Task<TestData> GetTestDataAsync()
  {
    await Task.Yield();
    return new TestData("Test", DateTime.Now);     
  }
}


public record TestData(string Name, DateTime Timestamp);