using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface TrainingFailed : CorrelatedBy<Guid>
    {
        Guid Id { get; }                            //  Model Id
        int NumberOfGenericFiles { get; }           //  Number of images generated for this specific model
        bool IsModelTrained { get; }                //  True if model was successfully trained
        bool IsThumbnailGenerated { get; }           //  True if model's thumbnail was successfully generated
        string Message { get; }                     //  Error message that identify the problem (stack trace)
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
