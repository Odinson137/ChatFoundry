using System.Reflection;
using BillingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Data;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<CompanySubscription> CompanySubscriptions => Set<CompanySubscription>();
    public DbSet<CompanyBalance> CompanyBalances => Set<CompanyBalance>();
    public DbSet<BalanceTransaction> BalanceTransactions => Set<BalanceTransaction>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<LicenseKey> LicenseKeys => Set<LicenseKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
