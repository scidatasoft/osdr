using CQRSlite.Domain;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain
{
    public class Fingerprint
    {
        public string Type { get; set; }
        public int Size { get; set; }
        public int Radius { get; set; }
    }

    // Don't remove numbers, 
    // because ML used numbers when received and handle methods
    public enum MachineLearningMethod
    {
        NaiveBayes = 1,
        LinearRegression = 2,
        DecisionTree = 3,
        RandomForest = 4,
        SupportVectorMachine = 5,
        NearestNeighborsСlassifier = 7,
        ExtremeGradientBoostingClassifier = 8,
        NearestNeighborsRegressor = 9,
        ExtremeGradientBoostingRegressor = 10,
        ElasticNet = 11,
        DnnRegressor = 12,
        DnnСlassifier = 13
    }

    public enum ModelStatus
    {
        Created = 1,
        Training = 2,
        Trained = 3,
        Failed = 4,
        Processed = 5,
        Loaded = 6
    }


    public enum FingerprintType
    {
        ecfp = 1,
        fcfp = 2
        //Ecfc_chirality = 3,
        //Fcfc = 5,
        //Fcfc_chirality = 6,
        //Atom_pairs = 7,
        //Maccs = 8
    }

    public class Model : AggregateRoot
    {
        /// <summary>
        /// Blob storage bucket where model was loaded to
        /// </summary>
        public string Bucket { get; private set; }

        /// <summary>
        /// Blob's Id in Bucket
        /// </summary>
        public Guid? BlobId { get; private set; }

        /// <summary>
        /// Model`s training dataset 
        /// </summary>
        public Dataset Dataset { get; private set; }

        /// <summary>
        /// Model property info 
        /// </summary>
        public Property Property { get; private set; }

        /// <summary>
        /// User Id of person who currently owns the model
        /// </summary>
        public Guid OwnedBy { get; private set; }

        /// <summary>
        /// User Id of the person who created the model
        /// </summary>
        public Guid CreatedBy { get; private set; }

        /// <summary>
        /// Date and time when file was created
        /// </summary>
        public DateTimeOffset CreatedDateTime { get; private set; }

        /// <summary>
        /// User Id of person who changed the model last time
        /// </summary>
        public Guid UpdatedBy { get; protected set; }

        /// <summary>
        /// Date and time when file model changed last time
        /// </summary>
        public DateTimeOffset UpdatedDateTime { get; protected set; }

        /// <summary>
        /// Folder Id where the model belongs to
        /// </summary>
        public Guid? ParentId { get; private set; }

        /// <summary>
        /// Method which used for model training
        /// </summary>
        public string Method { get; private set; }

        /// <summary>
        /// Method which used for display in SSP dialog
        /// </summary>
        public string DisplayMethodName { get; private set; }

        /// <summary>
        /// Current model training status
        /// </summary>
        public ModelStatus Status { get; private set; }

        /// <summary>
        /// Other specific info adout model
        /// </summary>
        //public IDictionary<string, object> ModelInfo { get; private set; } = new Dictionary<string, object>();

        public IEnumerable<Fingerprint> Fingerprints { get; private set; }

        /// <summary>
        /// Model name
        /// </summary>
        public string Name { get; private set; }

        public string Scaler { get; private set; }

        public int KFold { get; private set; }

        public decimal TestDatasetSize { get; private set; }

        public decimal SubSampleSize { get; private set; }

        public string ClassName { get; private set; }

        public IList<Image> Images { get; protected set; } = new List<Image>();

        public bool IsDeleted { get; private set; }

        public double Modi { get; private set; }
        
        public AccessPermissions Permissions { get; private set; }

        public IEnumerable<string> Targets { get; private set; } = new List<string>();

        public double ConsensusWeight { get; private set; }

        public IDictionary<string, object> Metadata { get; private set; }

        private void Apply(ModelCreated e)
        {
            OwnedBy = e.UserId;
            CreatedBy = e.UserId;
            CreatedDateTime = e.TimeStamp;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            ParentId = e.ParentId;
            Status = e.Status;
            Method = e.Method;
            KFold = e.KFold;
            TestDatasetSize = e.TestDatasetSize;
            SubSampleSize = e.SubSampleSize;
            ClassName = e.ClassName;
            Fingerprints = e.Fingerprints;
            Scaler = e.Scaler;
            BlobId = e.BlobId;
            Bucket = e.Bucket;
            Name = e.Name;
            DisplayMethodName = e.DisplayMethodName;
            Dataset = e.Dataset;
            Property = e.Property;
            Metadata = e.Metadata;
        }

        private void Apply(TargetsChanged e)
        {
            UpdatedBy = e.UserId;
            Targets = e.Targets;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(PermissionsChanged e)
        {
            if (e.AccessPermissions.IsPublic.HasValue)
            {
                Permissions.IsPublic = e.AccessPermissions.IsPublic;
            }

            Permissions.Users = e.AccessPermissions.Users;
            Permissions.Groups = e.AccessPermissions.Groups;

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(ModelMoved e)
        {
            ParentId = e.NewParentId;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(ModelDeleted e)
        {
            IsDeleted = true;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(ModelNameUpdated e)
        {
            Name = e.Name;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(StatusChanged e)
        {
            Status = e.Status;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(ModelPropertiesUpdated e)
        {
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
            Property = e.Property;
            Dataset = e.Dataset;
            Modi = e.Modi;
            DisplayMethodName = e.DisplayMethodName;
        }

        private void Apply(ImageAdded e)
        {
            if (!Images.Contains(e.Image))
            {
                Images.Add(e.Image);
            }

            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(ConsensusWeightChanged e)
        {
            ConsensusWeight = e.ConsensusWeight;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        private void Apply(ModelBlobChanged e)
        {
            Metadata = e.Metadata;
            BlobId = e.BlobId;
            Bucket = e.Bucket;
            UpdatedBy = e.UserId;
            UpdatedDateTime = e.TimeStamp;
        }

        public Model(Guid id,
            Guid userId,
            Guid parentId,
            ModelStatus status,
            string method,
            string scaler,
            int kFold,
            decimal testDatasetSize,
            decimal subSampleSize,
            string className,
            IEnumerable<Fingerprint> fingerprints,
            Dataset dataset = null,
            Property property = null,
            Guid? blobId = null,
            string bucket = null,
            string name = null,
            string displayMethodName = null,
            IDictionary<string, object> metadata = null
            )
        {
            Id = id;
            ApplyChange(new ModelCreated(Id, userId, parentId, status, dataset, property, method, scaler, kFold, testDatasetSize, subSampleSize, className, fingerprints, blobId, bucket, name, displayMethodName, metadata));
        }

        protected Model()
        {
            Permissions = new AccessPermissions
            {
                Users = new HashSet<Guid>(),
                Groups = new HashSet<Guid>()
            };
        }

        public void ChangeStatus(Guid userId, ModelStatus status)
        {
            ApplyChange(new StatusChanged(Id, userId, status));
        }

        public void UpdateModelProperties(Guid userId, Dataset dataset, Property property, double modi, string displayMethodName)
        {
            ApplyChange(new ModelPropertiesUpdated(Id, userId, dataset, property, modi, displayMethodName));
        }

        public void AddImage(Guid userId, Image img)
        {
            ApplyChange(new ImageAdded(Id, userId, img));
        }

        public void UpdateModelName(Guid userId, string name)
        {
            ApplyChange(new ModelNameUpdated(Id, userId, name));
        }

        public void DeleteModel(Guid userId, bool force)
        {
            ApplyChange(new ModelDeleted(Id, userId, force));
        }

        public void MoveModel(Guid userId, Guid? newParentId)
        {
            ApplyChange(new ModelMoved(Id, userId, ParentId, newParentId));
        }

        public void GrantAccess(Guid userId, AccessPermissions accessPermissions)
        {
            ApplyChange(new PermissionsChanged(Id, userId, accessPermissions));
        }

        public void SetTargets(IEnumerable<string> targets, Guid userId)
        {
            ApplyChange(new TargetsChanged(Id, userId, targets));
        }

        public void SetConsensusWeight(double consensusWeight, Guid userId)
        {
            ApplyChange(new ConsensusWeightChanged(Id, userId, consensusWeight));
        }

        public void SetMetadata(Guid userId, Guid blobId, string bucket, IDictionary<string, object> metadata)
        {
            ApplyChange(new ModelBlobChanged(Id, userId, blobId, bucket, metadata));
        }
    }
}
