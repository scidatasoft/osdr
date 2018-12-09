namespace Sds.FileStorage.EntityFramework
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFileId : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Files", "FolderId", "dbo.Folders");
            DropIndex("dbo.Files", new[] { "FolderId" });
            AddColumn("dbo.Files", "FileId", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.Files", "Name", c => c.String());
            AlterColumn("dbo.Files", "FolderId", c => c.Int());
            CreateIndex("dbo.Files", "FileId", unique: true);
            CreateIndex("dbo.Files", "FolderId");
            AddForeignKey("dbo.Files", "FolderId", "dbo.Folders", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Files", "FolderId", "dbo.Folders");
            DropIndex("dbo.Files", new[] { "FolderId" });
            DropIndex("dbo.Files", new[] { "FileId" });
            AlterColumn("dbo.Files", "FolderId", c => c.Int(nullable: false));
            AlterColumn("dbo.Files", "Name", c => c.String(nullable: false));
            DropColumn("dbo.Files", "FileId");
            CreateIndex("dbo.Files", "FolderId");
            AddForeignKey("dbo.Files", "FolderId", "dbo.Folders", "Id", cascadeDelete: true);
        }
    }
}
