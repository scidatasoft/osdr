using System.Linq;
using MassTransit;
using System.Threading.Tasks;
using Serilog;

namespace Sds.Osdr.Domain.BddTests.FakeConsumers
{
    public class FaultEventHandlers : IConsumer<Fault>
    {
        public Task Consume(ConsumeContext<Fault> context)
        {
            var ex = context.Message.Exceptions;

            Log.Error($"Message: {ex.First().Message}\nStacktrace: {ex.First().StackTrace}");
            
            return Task.CompletedTask;
        }
    }
}
