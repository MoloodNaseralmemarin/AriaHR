using AriaHR.Modules.Payroll.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AriaHR.Modules.Payroll.Infrastructure.Persistence;

public sealed class PayrollDbContext : DbContext
{
    public PayrollDbContext(
        DbContextOptions<PayrollDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmployeeSalary> EmployeeSalaries => Set<EmployeeSalary>();
    public DbSet<InsuranceRecord> InsuranceRecords => Set<InsuranceRecord>();
    public DbSet<InsuranceRule> InsuranceRules => Set<InsuranceRule>();
    public DbSet<InsuranceRuleItem> InsuranceRuleItems => Set<InsuranceRuleItem>();
    public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<SalaryStructureItem> SalaryStructureItems => Set<SalaryStructureItem>();
    public DbSet<TaxBracket> TaxBrackets => Set<TaxBracket>();
    public DbSet<TaxRecord> TaxRecords => Set<TaxRecord>();
    public DbSet<TaxRule> TaxRules => Set<TaxRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PayrollDbContext).Assembly);
    }
}
