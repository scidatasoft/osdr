using CQRSlite.Domain;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.MachineLearning.Sagas.Events;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Domain.BackEnd.EventHandlers
{
    public class MachineLearningEventHandlers: IConsumer<ProcessingFinished>, 
        IConsumer<PropertiesPredictionFinished>,
        IConsumer<ReportGenerationFailed>,
        IConsumer<ModelTrainingFinished>,
        IConsumer<PermissionChangedPersisted>,
        IConsumer<TrainingFailed>
    {
        private readonly ISession _session;

        public MachineLearningEventHandlers(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<ProcessingFinished> context)
        {
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Model", context.Message.Id, context.Message, typeof(ProcessingFinished).Name, typeof(ProcessingFinished).AssemblyQualifiedName, null));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<PropertiesPredictionFinished> context)
        {
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "File", context.Message.Id, context.Message, typeof(PropertiesPredictionFinished).Name, typeof(PropertiesPredictionFinished).AssemblyQualifiedName, null));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<ReportGenerationFailed> context)
        {
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Model", NewId.NextGuid(), context.Message, typeof(ReportGenerationFailed).Name, typeof(ReportGenerationFailed).AssemblyQualifiedName, null));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<TrainingFailed> context)
        {
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Model", NewId.NextGuid(), context.Message, typeof(TrainingFailed).Name, typeof(TrainingFailed).AssemblyQualifiedName, null));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<ModelTrainingFinished> context)
        {
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Model", context.Message.Id, context.Message, typeof(ModelTrainingFinished).Name, typeof(ModelTrainingFinished).AssemblyQualifiedName, null));

            await _session.Commit();
        }

        public async Task Consume(ConsumeContext<PermissionChangedPersisted> context)
        {
            await _session.Add(new UserNotification(
                NewId.NextGuid(), context.Message.UserId, "Model", context.Message.Id, context.Message, typeof(PermissionsChanged).Name, typeof(PermissionChangedPersisted).AssemblyQualifiedName, null));

            await _session.Commit();
        }

    }
}
