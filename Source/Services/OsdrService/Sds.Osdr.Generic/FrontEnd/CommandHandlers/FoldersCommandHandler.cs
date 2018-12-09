using CQRSlite.Domain;
using CQRSlite.Domain.Exception;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Folders;
using Sds.Osdr.Generic.Domain.Events;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.FrontEnd.CommandHandlers
{
    public class FoldersCommandHandler : IConsumer<CreateFolder>, 
                                         IConsumer<RenameFolder>,
                                         IConsumer<MoveFolder>, 
                                         IConsumer<ChangeStatus>, 
                                         IConsumer<DeleteFolder>
    {
        private readonly ISession _session;

        public FoldersCommandHandler(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<RenameFolder> context)
        {
            try
            {
                var folder = await _session.Get<Folder>(context.Message.Id, context.Message.ExpectedVersion);

                folder.Rename(context.Message.UserId, context.Message.NewName);

                await _session.Commit();
            }
            catch (ConcurrencyException)
            {
                Log.Error($"Error renaming folder {context.Message.Id} unexpected version {context.Message.ExpectedVersion}");

                await context.Publish<UnexpectedVersion>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    Version = context.Message.ExpectedVersion,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }

        public async Task Consume(ConsumeContext<CreateFolder> context)
        {
            var folder = new Folder(context.Message.Id, correlationId: context.Message.CorrelationId, userId: context.Message.UserId, parentId: context.Message.ParentId, name: context.Message.Name, sessionId: context.Message.SessionId);

            await _session.Add(folder);

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<MoveFolder> context)
        {
            try
            {
                var folder = await _session.Get<Folder>(context.Message.Id, context.Message.ExpectedVersion);

                folder.ChangeParent(context.Message.UserId, context.Message.NewParentId, context.Message.SessionId);

                await _session.Commit();
            }
            catch (ConcurrencyException)
            {
                Log.Error($"Error moving folder {context.Message.Id} unexpected version {context.Message.ExpectedVersion}");

                await context.Publish<UnexpectedVersion>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    Version = context.Message.ExpectedVersion,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }

        public async Task Consume(ConsumeContext<DeleteFolder> context)
        {
            if(context.Message.Force)
            {
                var folder = await _session.Get<Folder>(context.Message.Id);

                folder.Delete(context.Message.UserId, context.Message.Id, context.Message.Force);

                await _session.Commit();
            }
            else
            {
                try
                {
                    var folder = await _session.Get<Folder>(context.Message.Id, context.Message.ExpectedVersion);

                    folder.Delete(context.Message.UserId, context.Message.Id, false);

                    await _session.Commit();
                }
                catch (ConcurrencyException)
                {
                    Log.Error($"Error deleting folder {context.Message.Id} unexpected version {context.Message.ExpectedVersion}");

                    await context.Publish<UnexpectedVersion>(new
                    {
                        Id = context.Message.Id,
                        UserId = context.Message.UserId,
                        Version = context.Message.ExpectedVersion,
                        TimeStamp = DateTimeOffset.UtcNow
                    });
                }
            }

        }

        public async Task Consume(ConsumeContext<ChangeStatus> context)
        {
            try
            {
                var folder = await _session.Get<Folder>(context.Message.Id);

                folder.ChangeStatus(context.Message.UserId, context.Message.Status);

                await _session.Commit();
            }
            catch (ConcurrencyException)
            {
                Log.Error($"Error changing folder {context.Message.Id} unexpected version {context.Message.ExpectedVersion}");

                await context.Publish<UnexpectedVersion>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    Version = context.Message.ExpectedVersion,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }
    }
}
