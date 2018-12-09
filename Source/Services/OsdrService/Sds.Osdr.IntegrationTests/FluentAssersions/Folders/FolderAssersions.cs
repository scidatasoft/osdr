using FluentAssertions;
using FluentAssertions.Collections;
using Sds.Osdr.Generic.Domain;
using System.Collections.Generic;

namespace Sds.Osdr.IntegrationTests.FluentAssersions
{
    public static class FolderAssersionsExtensions
    {
        public static void EntityShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, Folder folder)
        {
            assertions.Subject.Should().NotBeNull();

            assertions.Subject.ShouldAllBeEquivalentTo(new Dictionary<string, object>()
            {
                { "_id", folder.Id},
                { "CreatedBy", folder.CreatedBy },
                { "CreatedDateTime", folder.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", folder.UpdatedBy },
                { "UpdatedDateTime", folder.UpdatedDateTime.UtcDateTime },
                { "OwnedBy", folder.OwnedBy },
                { "Name", folder.Name },
                { "IsDeleted", folder.IsDeleted },
                { "ParentId", folder.ParentId },
                { "Version", folder.Version },
                { "Status", folder.Status.ToString() }
            });
        }

        public static void NodeShouldBeEquivalentTo(this GenericDictionaryAssertions<string, object> assertions, Folder folder)
        {
            assertions.Subject.Should().NotBeNull();

            assertions.Subject.ShouldAllBeEquivalentTo(new Dictionary<string, object>()
            {
                { "_id", folder.Id},
                { "Type", "Folder" },
                { "CreatedBy", folder.CreatedBy },
                { "CreatedDateTime", folder.CreatedDateTime.UtcDateTime },
                { "UpdatedBy", folder.UpdatedBy },
                { "UpdatedDateTime", folder.UpdatedDateTime.UtcDateTime },
                { "OwnedBy", folder.OwnedBy },
                { "Name", folder.Name },
                { "ParentId", folder.ParentId },
                { "Version", folder.Version }
            });
        }
    }
}
