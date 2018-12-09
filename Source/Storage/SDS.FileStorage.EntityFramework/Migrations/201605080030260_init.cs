namespace Sds.FileStorage.EntityFramework
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Blobs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Data = c.Binary(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Files",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Extension = c.String(nullable: false),
                        Created = c.DateTime(nullable: false),
                        FolderId = c.Int(nullable: false),
                        BlobId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Blobs", t => t.BlobId, cascadeDelete: true)
                .ForeignKey("dbo.Folders", t => t.FolderId, cascadeDelete: true)
                .Index(t => t.FolderId)
                .Index(t => t.BlobId);
            
            CreateTable(
                "dbo.Folders",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        ParentId = c.Int(),
                        Deleted = c.Boolean(nullable: false),
                        Created = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Folders", t => t.ParentId)
                .Index(t => t.ParentId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Files", "FolderId", "dbo.Folders");
            DropForeignKey("dbo.Folders", "ParentId", "dbo.Folders");
            DropForeignKey("dbo.Files", "BlobId", "dbo.Blobs");
            DropIndex("dbo.Folders", new[] { "ParentId" });
            DropIndex("dbo.Files", new[] { "BlobId" });
            DropIndex("dbo.Files", new[] { "FolderId" });
            DropTable("dbo.Folders");
            DropTable("dbo.Files");
            DropTable("dbo.Blobs");
        }
    }
}
