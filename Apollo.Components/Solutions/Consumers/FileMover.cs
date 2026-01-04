using System;
using System.Threading.Tasks;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Solutions.Consumers
{
    public class FileMover : IConsumer<MoveFile>
    {
        private readonly SolutionsState _state;

        public FileMover(SolutionsState state)
        {
            _state = state;
        }

        public async Task Consume(MoveFile message)
        {
            _state.MoveFile(message.File, message.DestinationFolder);
        }
    }
} 