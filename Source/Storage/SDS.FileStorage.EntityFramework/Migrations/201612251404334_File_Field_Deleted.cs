namespace Sds.FileStorage.EntityFramework
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class File_Field_Deleted : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Files", "Deleted", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Files", "Deleted");
        }
    }
}
