using MassTransit;
using MassTransit.Testing;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Generic.Domain.Commands.Folders;
using Sds.Osdr.Generic.Domain.Commands.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sds.Osdr.BddTests;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.MachineLearning.Sagas.Events;

namespace Sds.Osdr.Domain.BddTests.Extensions
{
    public static class BusTestHarnessExtensions
    {
        public static void WaitWhileModelShared(this BusTestHarness harness, Guid id)
        {
            if (!harness.Published.Select<MachineLearning.Domain.Events.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

//            if (!harness.Published.Select<Generic.Domain.Events.Nodes.ккуPermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
//            {
//                throw new TimeoutException();
//            }
        }
        
        public static void WaitWhileFileShared(this BusTestHarness harness, Guid id)
        {
            if (!harness.Published.Select<Generic.Domain.Events.Files.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

//            if (!harness.Published.Select<Generic.Domain.Events.Nodes.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
//            {
//                throw new TimeoutException();
//            }
        }
        
        public static void WaitWhileFolderShared(this BusTestHarness harness, Guid id)
        {
            if (!harness.Published.Select<Generic.Domain.Events.Folders.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Published.Select<Generic.Domain.Events.Nodes.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        public static void WaitWhileFolderCreated(this BusTestHarness harness, Guid id)
        {
            if (!harness.Published.Select<Generic.Domain.Events.Folders.FolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Published.Select<Generic.Domain.Events.Nodes.FolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }
        
        public static void WaitWhileModelTrained(this BusTestHarness harness, Guid folderId)
        {
            if (!harness.Published.Select<TrainingFinished>(m => m.Context.Message.Id == folderId).Any())
            {
                throw new TimeoutException();
            }
        }
        
        public static void WaitWhileFolderRenamed(this BusTestHarness harness, Guid id)
        {
            if (!harness.Published.Select<Generic.Domain.Events.Folders.RenamedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Published.Select<Generic.Domain.Events.Nodes.RenamedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        public static void WaitWhileFolderMoved(this BusTestHarness harness, Guid id)
        {
            if (!harness.Published.Select<Generic.Domain.Events.Folders.MovedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Published.Select<Generic.Domain.Events.Nodes.MovedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        public static void WaitWhileFolderDeleted(this BusTestHarness harness, Guid id)
        {
            if (!harness.Published.Select<Generic.Domain.Events.Folders.DeletedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Published.Select<Generic.Domain.Events.Nodes.DeletedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        public static async Task<Guid> CreateFolder(this BusTestHarness harness, string name, Guid parentId, Guid userId)
        {
            Guid id = NewId.NextGuid();

            await harness.Bus.Publish<CreateFolder>(new
            {
                Id = id,
                Name = name,
                ParentId = parentId,
                UserId = userId
            });

            harness.WaitWhileFolderCreated(id);

            return id;
        }

        //public static async Task<bool> DeleteFolder(this BusTestHarness harness, Guid id, Guid userId, int expectedVersion = 0)
        //{
        //    await harness.Bus.Publish<DeleteFolder>(new
        //    {
        //        Id = id,
        //        UserId = userId,
        //        ExpectedVersion = expectedVersion
        //    });

        //    return await harness.WaitWhileAllProcessed();
        //}

        //public static async Task<bool> DeleteFile(this BusTestHarness harness, Guid id, Guid userId, int expectedVersion = 0)
        //{
        //    await harness.Bus.Publish<DeleteFile>(new
        //    {
        //        Id = id,
        //        UserId = userId,
        //        ExpectedVersion = expectedVersion
        //    });

        //    return await harness.WaitWhileAllProcessed();
        //}

        //public static async Task<bool> DeleteRecord(this BusTestHarness harness, Guid id, Guid userId, int expectedVersion = 0)
        //{
        //    await harness.Bus.Publish<DeleteRecord>(new
        //    {
        //        Id = id,
        //        UserId = userId,
        //        ExpectedVersion = expectedVersion
        //    });

        //    return await harness.WaitWhileAllProcessed();
        //}

        public static async Task CreateUser(this BusTestHarness harness, Guid id, string displayName, string firstName, string lastName, string loginName, string email, string avatar, Guid userId)
        {
            await harness.Bus.Publish<CreateUser>(new
            {
                Id = id,
                DisplayName = displayName,
                FirstName = firstName,
                LastName = lastName,
                LoginName = loginName,
                Email = email,
                Avatar = avatar,
                UserId = userId
            });

            if (!harness.Published.Select<Generic.Domain.Events.Users.UserPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Published.Select<Generic.Domain.Events.Nodes.UserPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }
        
        public static IEnumerable<IPublishedMessage<T>> Select<T>(this IPublishedMessageList published, Func<IPublishedMessage<T>, bool> filter) 
            where T : class
        {
            return published.Select<T>()
                .Cast<IPublishedMessage<T>>()
                .Where(filter);
        }
    }
}
