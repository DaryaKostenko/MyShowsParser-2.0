namespace MyShowsParser.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CountryModels",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 4000),
                    })
                .PrimaryKey(t => t.Name);
            
            CreateTable(
                "dbo.ShowModels",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 4000),
                        OriginalName = c.String(maxLength: 4000),
                        Genres = c.String(maxLength: 4000),
                        MyShowsRating = c.String(maxLength: 4000),
                        Country_Name = c.String(maxLength: 4000),
                    })
                .PrimaryKey(t => t.Name)
                .ForeignKey("dbo.CountryModels", t => t.Country_Name)
                .Index(t => t.Country_Name);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ShowModels", "Country_Name", "dbo.CountryModels");
            DropIndex("dbo.ShowModels", new[] { "Country_Name" });
            DropTable("dbo.ShowModels");
            DropTable("dbo.CountryModels");
        }
    }
}
