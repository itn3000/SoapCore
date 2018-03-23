using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using System.Net;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SoapCore.Benchmark
{
	[ShortRunJob]
	public class EchoBench
	{
		// 0 measures overhead of creating host
		[Params(0, 100)]
		public int LoopNum;
		static string EchoContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <Echo xmlns=""http://example.org/PingService""><str>abc</str></Echo>
  </soap:Body>
</soap:Envelope>
";
		static TestServer CreateTestHost()
		{
			var builder = WebHost.CreateDefaultBuilder()
				.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Critical))
				.UseStartup<Startup>();
			return new TestServer(builder);
		}
		[Benchmark]
		public async Task Echo()
		{
			using(var host = CreateTestHost())
			{
				for (int i = 0; i < LoopNum; i++)
				{
					using(var content = new StringContent(EchoContent, Encoding.UTF8, "text/xml"))
					{
						using(var res = await host.CreateRequest("/TestService.asmx")
							.AddHeader("SOAPAction", "http://example.org/PingService/Echo")
							.And(msg =>
							{
								msg.Content = content;
							}).PostAsync().ConfigureAwait(false))
						{
							res.EnsureSuccessStatusCode();
						}
					}
				}
			}
		}
	}
	class Program
	{
		static void Main(string[] args)
		{
			var reporter = BenchmarkRunner.Run<EchoBench>();
		}
	}
}
