using System.Data.Entity;


namespace MyShowsParser
{
    class Context:DbContext
    {
        public DbSet<ShowModel> Shows { get; set; }
        public DbSet<CountryModel> Countries { get; set; }

        public Context() : base("MyDb")
        { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShowModel>().HasOptional(x => x.Country).WithMany(x => x.Shows);
        }
    }
}
