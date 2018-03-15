using System.ServiceModel;
using System.Threading.Tasks;

namespace SoapCore.Benchmark
{
	[ServiceContract]
	public interface IPingService
	{
		[OperationContract]
		string Echo(string str);
		[OperationContract]
		Task<string> EchoAsync(string str);
	}
	public class PingService : IPingService
	{
		public string Echo(string str)
		{
			return $"hello {str}";
		}

		public Task<string> EchoAsync(string str)
		{
			return Task.FromResult($"hello async {str}");
		}
	}
}
