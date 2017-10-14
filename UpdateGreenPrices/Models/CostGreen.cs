namespace UpdateGreenPrices.Models
{
  using System;
  using System.ComponentModel.DataAnnotations;
  using System.ComponentModel.DataAnnotations.Schema;
  using System.Data.Entity;
  using System.Linq;
  
  public class CostGreenContext : DbContext
  {
    // Your context has been configured to use a 'CostGreen' connection string from your application's 
    // configuration file (App.config or Web.config). By default, this connection string targets the 
    // 'UpdateGreenPrices.Models.CostGreen' database on your LocalDb instance. 
    // 
    // If you wish to target a different database and/or database provider, modify the 'CostGreen' 
    // connection string in the application configuration file.
    public CostGreenContext() : base("name=CostGreen")
    {
    
    }

    // Add a DbSet for each entity type that you want to include in your model. For more information 
    // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

     public virtual DbSet<GreenCosts> GreenCosts { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);
      modelBuilder.Entity<GreenCosts>().HasKey(x => new { x.CertificateType, x.EffectiveDate });
    }
  }

  public class GreenCosts
  {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int GreenCostId { get; set; }
    [Required]
    public string CertificateType { get; set; }
    [Required]
    public DateTime EffectiveDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    [Required]
    public double Price { get; set; }
    public int Version { get; set; }
    public DateTime VersionDate { get; set; }
    public int AuthorId { get; set; }
  }
}