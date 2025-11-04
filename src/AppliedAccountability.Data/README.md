# AppliedAccountability.Data

Data access layer with Entity Framework Core, Dapper, repository pattern, Unit of Work, and common data access patterns for enterprise applications.

## Features

- **Repository Pattern** - Generic repository with full CRUD operations
- **Unit of Work** - Transaction management across multiple repositories
- **Specification Pattern** - Build complex queries with reusable specifications
- **Audit Fields** - Automatic CreatedAt, UpdatedAt, CreatedBy, UpdatedBy tracking
- **Soft Delete** - Logical delete with IsDeleted flag
- **Pagination** - Built-in pagination support with PagedResult
- **Query Optimization** - No-tracking queries, compiled queries, eager loading
- **Multi-Database Support** - PostgreSQL, SQL Server, SQLite

## Installation

```bash
dotnet add package AppliedAccountability.Data
```

## Quick Start

### 1. Define Your Entities

```csharp
using AppliedAccountability.Data.Entities;

// Simple entity with GUID key and audit fields
public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// Entity with soft delete support
public class Customer : FullAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = new();
}

// Entity with int primary key
public class Category : AuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
}
```

### 2. Create Your DbContext

```csharp
using AppliedAccountability.Data.Extensions;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : AppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService)
        : base(options, currentUserService)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Important: calls soft delete filter setup

        // Configure your entities
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });
    }
}
```

### 3. Register Services

```csharp
using AppliedAccountability.Data.Configuration;

// In Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Unit of Work
builder.Services.AddUnitOfWork<AppDbContext>();

// Optional: Add specific repositories
builder.Services.AddRepository<Product, Guid, AppDbContext>();
```

## Usage Examples

### Example 1: Basic CRUD Operations

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Product> CreateProductAsync(string name, decimal price, int stock)
    {
        var repository = _unitOfWork.Repository<Product, Guid>();

        var product = new Product
        {
            Name = name,
            Price = price,
            Stock = stock
        };

        await repository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task<Product?> GetProductAsync(Guid id)
    {
        var repository = _unitOfWork.Repository<Product, Guid>();
        return await repository.GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync()
    {
        var repository = _unitOfWork.Repository<Product, Guid>();
        return await repository.GetAllAsync();
    }

    public async Task UpdateProductAsync(Guid id, decimal newPrice)
    {
        var repository = _unitOfWork.Repository<Product, Guid>();

        var product = await repository.GetByIdAsync(id);
        if (product != null)
        {
            product.Price = newPrice;
            await repository.UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var repository = _unitOfWork.Repository<Product, Guid>();
        await repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Example 2: Pagination

```csharp
public async Task<PagedResult<Product>> GetProductsPagedAsync(int pageNumber, int pageSize)
{
    var repository = _unitOfWork.Repository<Product, Guid>();
    return await repository.GetPagedAsync(pageNumber, pageSize);
}

public async Task<PagedResult<Product>> SearchProductsAsync(string searchTerm, int page, int pageSize)
{
    var repository = _unitOfWork.Repository<Product, Guid>();
    return await repository.GetPagedAsync(
        p => p.Name.Contains(searchTerm),
        page,
        pageSize
    );
}
```

### Example 3: Specification Pattern

```csharp
using AppliedAccountability.Data.Specifications;

// Define a reusable specification
public class ProductsByPriceRangeSpec : Specification<Product>
{
    public ProductsByPriceRangeSpec(decimal minPrice, decimal maxPrice)
    {
        AddCriteria(p => p.Price >= minPrice && p.Price <= maxPrice);
        AddOrderBy(p => p.Price);
        AsNoTracking(); // Read-only query for better performance
    }
}

public class ProductsWithLowStockSpec : Specification<Product>
{
    public ProductsWithLowStockSpec(int threshold)
    {
        AddCriteria(p => p.Stock < threshold);
        AddOrderBy(p => p.Stock);
    }
}

// Use specifications
public async Task<IReadOnlyList<Product>> GetAffordableProductsAsync()
{
    var repository = _unitOfWork.Repository<Product, Guid>();
    var spec = new ProductsByPriceRangeSpec(minPrice: 0, maxPrice: 100);
    return await repository.FindAsync(spec);
}

public async Task<PagedResult<Product>> GetLowStockProductsAsync(int page, int pageSize)
{
    var repository = _unitOfWork.Repository<Product, Guid>();
    var spec = new ProductsWithLowStockSpec(threshold: 10);
    return await repository.GetPagedAsync(spec, page, pageSize);
}
```

### Example 4: Unit of Work with Transactions

```csharp
public async Task ProcessOrderAsync(Guid customerId, List<Guid> productIds)
{
    try
    {
        await _unitOfWork.BeginTransactionAsync();

        var customerRepo = _unitOfWork.Repository<Customer, Guid>();
        var productRepo = _unitOfWork.Repository<Product, Guid>();

        var customer = await customerRepo.GetByIdAsync(customerId);
        if (customer == null)
            throw new InvalidOperationException("Customer not found");

        decimal totalPrice = 0;

        foreach (var productId in productIds)
        {
            var product = await productRepo.GetByIdAsync(productId);
            if (product == null || product.Stock < 1)
                throw new InvalidOperationException($"Product {productId} not available");

            product.Stock--;
            totalPrice += product.Price;
            await productRepo.UpdateAsync(product);
        }

        // Create order (assuming Order entity exists)
        // ...

        await _unitOfWork.CommitTransactionAsync();
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

### Example 5: Soft Delete

```csharp
// Entities implementing ISoftDeletable are automatically soft-deleted
public async Task DeleteCustomerAsync(Guid id)
{
    var repository = _unitOfWork.Repository<Customer, Guid>();

    // This will set IsDeleted = true, DeletedAt = DateTime.UtcNow
    await repository.DeleteAsync(id);
    await _unitOfWork.SaveChangesAsync();
}

// Soft-deleted entities are automatically filtered out
public async Task<Customer?> GetCustomerAsync(Guid id)
{
    var repository = _unitOfWork.Repository<Customer, Guid>();
    // This will NOT return soft-deleted customers
    return await repository.GetByIdAsync(id);
}

// To include soft-deleted entities, use a specification
public class CustomersIncludingDeletedSpec : Specification<Customer>
{
    public CustomersIncludingDeletedSpec()
    {
        IgnoreFilters(); // Includes soft-deleted entities
    }
}
```

### Example 6: Audit Fields

```csharp
// Implementing ICurrentUserService for audit tracking
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

// Register in Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Now audit fields are automatically populated
var product = new Product { Name = "Widget", Price = 10.99m, Stock = 100 };
await repository.AddAsync(product);
await _unitOfWork.SaveChangesAsync();

// product.CreatedAt is now set to DateTime.UtcNow
// product.CreatedBy is set to current user ID
```

## Entity Base Classes

### Entity<TKey>
Basic entity with primary key only.

### AuditableEntity<TKey>
Entity with audit fields:
- `CreatedAt` (DateTime)
- `CreatedBy` (string)
- `UpdatedAt` (DateTime?)
- `UpdatedBy` (string?)

### FullAuditableEntity<TKey>
Entity with audit fields AND soft delete:
- All audit fields from `AuditableEntity`
- `IsDeleted` (bool)
- `DeletedAt` (DateTime?)
- `DeletedBy` (string?)

## Specification Pattern

Build complex, reusable queries:

```csharp
public class ActiveProductsSpec : Specification<Product>
{
    public ActiveProductsSpec()
    {
        AddCriteria(p => p.Stock > 0);
        AddInclude(p => p.Category); // Eager load
        AddOrderByDescending(p => p.CreatedAt);
        ApplyPaging(skip: 0, take: 20);
        AsNoTracking(); // For read-only queries
    }
}
```

## Requirements

- .NET 10.0 or later
- Entity Framework Core 9.0+
- One of: PostgreSQL, SQL Server, or SQLite

## License

MIT License - Copyright © Applied Accountability Services LLC 2025

## Contributing

This package is maintained by Applied Accountability Services LLC. For bug reports or feature requests, please open an issue on GitHub.
