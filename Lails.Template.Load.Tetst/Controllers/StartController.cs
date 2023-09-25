//using Lails.Template.Load.Tetst.Consumers;
//using Microsoft.AspNetCore.Mvc;
//using NetCoreDataBus;
//using System.Threading.Tasks;

//namespace Lails.Template.Load.Tetst.Controllers
//{
//	[Route("api/test")]
//	[ApiController]
//	public class StartController : ControllerBase
//	{
//		readonly IBusPublisher _busPublisher;
//		public StartController(IBusPublisher busPublisher)
//		{
//			_busPublisher = busPublisher;
//		}

//		[Route("startLoadTest")]
//		[HttpGet]
//		public async Task StartLoadTest()
//		{
//			for (int i = 0; i < 90000; i++)
//			{
//				await _busPublisher.PublishAsync(new LoadTestEvent());
//			}
//		}

//		public class LoadTestEvent : ILoadTestEvent { }
//	}
//}
