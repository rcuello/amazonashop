using Ecommerce.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Persistence;

public class EcommerceDbContext : IdentityDbContext<Usuario>
{
    public EcommerceDbContext(DbContextOptions<EcommerceDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>()
            .HasMany(p=> p.Products)
            .WithOne(c=> c.Category)
            .HasForeignKey(r=> r.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasMany(product=> product.Reviews)
            .WithOne(review=> review.Product)
            .HasForeignKey(review=> review.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Product>()
            .HasMany(product=> product.Images)
            .WithOne(image=> image.Product)
            .HasForeignKey(image=> image.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ShoppingCart>()
            .HasMany(cart=> cart.ShoppingCartItems)
            .WithOne(item=> item.ShoppingCart)
            .HasForeignKey(item=> item.ShoppingCartId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Usuario>().Property(x=> x.Id).HasMaxLength(36);
        builder.Entity<Usuario>().Property(x=> x.NormalizedUserName).HasMaxLength(90);

        builder.Entity<IdentityRole>().Property(x=> x.Id).HasMaxLength(36);
        builder.Entity<IdentityRole>().Property(x=> x.NormalizedName).HasMaxLength(90);
    }
    public DbSet<Product>? Products { get; set; }
    public DbSet<Category>? Categories { get; set; }
    public DbSet<Image>? Images { get; set; }
    public DbSet<Address>? Addresses { get; set; }
    public DbSet<Order>? Orders { get; set; }
    public DbSet<OrderItem>? OrderItems { get; set; }
    public DbSet<OrderAddress>? OrderAddresses { get; set; }
    public DbSet<Review>? Reviews { get; set; }
    public DbSet<ShoppingCart>? ShoppingCarts { get; set; }
    public DbSet<ShoppingCartItem>? ShoppingCartItems { get; set; }
    public DbSet<Country>? Countries { get; set; }
    
}