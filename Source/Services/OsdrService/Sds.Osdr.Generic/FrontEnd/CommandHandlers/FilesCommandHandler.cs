using CQRSlite.Domain;
using CQRSlite.Domain.Exception;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.Events;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.FrontEnd.CommandHandlers
{
    public class FilesCommandHandler : IConsumer<RenameFile>,
                                       IConsumer<MoveFile>, 
                                       IConsumer<DeleteFile>
    {
        private readonly ISession _session;

        public FilesCommandHandler(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<RenameFile> context)
        {
            try
            {
                var folder = await _session.Get<File>(context.Message.Id);

                folder.Rename(context.Message.UserId, context.Message.NewName);

                await _session.Commit();
            }
            catch(ConcurrencyException)
            {
                Log.Error($"Error renaming file {context.Message.Id} unexpected version {context.Message.ExpectedVersion}");

                await context.Publish<UnexpectedVersion>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    Version = context.Message.ExpectedVersion,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }

        public async Task Consume(ConsumeContext<MoveFile> context)
        {
            try
            {
                var file = await _session.Get<File>(context.Message.Id, context.Message.ExpectedVersion);
            
                file.ChangeParent(context.Message.UserId, context.Message.NewParentId);

                await _session.Commit();
            }
            catch (ConcurrencyException)
            {
                Log.Error($"Error moving file {context.Message.Id} unexpected version {context.Message.ExpectedVersion}");

                await context.Publish<UnexpectedVersion>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    Version = context.Message.ExpectedVersion,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }

        public async Task Consume(ConsumeContext<DeleteFile> context)
        {
            //try
            //{
                var file = await _session.Get<File>(context.Message.Id/*, context.Message.ExpectedVersion*/);

                file.Delete(context.Message.UserId, context.Message.Force);
                
                await _session.Commit();
            //}
            //catch (ConcurrencyException)
            //{
            //    Log.Error($"Error deleting file {context.Message.Id} unexpected version {context.Message.ExpectedVersion}");

            //    await context.Publish<UnexpectedVersion>(new
            //    {
            //        Id = context.Message.Id,
            //        UserId = context.Message.UserId,
            //        Version = context.Message.ExpectedVersion,
            //        TimeStamp = DateTimeOffset.UtcNow
            //    });
            //}
        }
    }
}
