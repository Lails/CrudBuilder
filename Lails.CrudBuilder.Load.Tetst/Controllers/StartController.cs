using Lails.CrudBuilder.Load.Tetst.Consumers;
using Lails.MQ.Rabbit;
using Microsoft.AspNetCore.Mvc;

namespace Lails.CrudBuilder.Load.Tetst.Controllers
{
    [Route("api/test")]
    [ApiController]
    public class StartController : ControllerBase
    {
        readonly IRabbitPublisher _rabbitPublisher;
        public StartController(IRabbitPublisher rabbitPublisher)
        {
            _rabbitPublisher = rabbitPublisher;
        }

        [Route("startLoadTest")]
        [HttpGet]
        public void StartLoadTest()
        {
            Parallel.For(0, 200, (i) =>
            {
                Task.Run(async () =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        await _rabbitPublisher.PublishAsync(new LoadTestEvent());
                    }

                });

            });
        }

        public class LoadTestEvent : ILoadTestEvent { }
    }
}
