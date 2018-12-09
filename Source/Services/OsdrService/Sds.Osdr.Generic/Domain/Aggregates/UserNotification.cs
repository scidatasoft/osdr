using CQRSlite.Domain;
using Sds.Osdr.Generic.Domain.Events.UserNotifications;
using System;

namespace Sds.Osdr.Generic.Domain
{
    /// <summary>
    /// Class encapsulate front-end user's notifications
    /// </summary>
    public class UserNotification : AggregateRoot
    {
        /// <summary>
        /// User Id of person who currently owns the notification
        /// </summary>
        public Guid OwnedBy { get; private set; }

        public bool IsRead { get; private set; }

        public object EventPayload { get; private set; }

        public string EventTypeName { get; private set; }

        public string AssemblyQualifiedEventTypeName { get; private set; }

        public string NodeType { get; private set; }

        public Guid NodeId { get; private set; }

        public Guid? ParentNodeId { get; private set; }
        public bool IsDeleted { get; private set; }

        private UserNotification()
        {
        }

        public UserNotification(Guid id, IUserNotification notification)
            : this(id, notification.UserId, notification.NodeType, notification.NodeId, notification.EventPayload, notification.EventTypeName, notification.AssemblyQualifiedEventTypeName, notification.ParentNodeId)
        {
        }

        public UserNotification(Guid id, Guid userId, string nodeType, Guid nodeId, object eventPayload, string eventTypeName, string assemblyQualifiedEventTypeName, Guid? parentNodeId)
        {
            ApplyChange(new UserNotificationCreated(id, userId, nodeType, nodeId, eventPayload, eventTypeName, assemblyQualifiedEventTypeName, parentNodeId));
        }

        private void Apply(UserNotificationCreated e)
        {
            Id = e.Id;
            OwnedBy = e.UserId;
            EventPayload = e.EventPayload;
            EventTypeName = e.EventTypeName;
            AssemblyQualifiedEventTypeName = e.AssemblyQualifiedEventTypeName;
            NodeType = e.NodeType;
            NodeId = e.NodeId;
            ParentNodeId = e.ParentNodeId;
        }

        public void MarkAsRead()
        {
            ApplyChange(new UserNotificationMarkedAsRead(Id));
        }

        private void Apply(UserNotificationMarkedAsRead e)
        {
            IsRead = true;
        }

        public void Delete()
        {
            ApplyChange(new UserNotificationDeleted(Id));
        }

        private void Apply(UserNotificationDeleted e)
        {
            IsDeleted = true;
        }
    }
}
