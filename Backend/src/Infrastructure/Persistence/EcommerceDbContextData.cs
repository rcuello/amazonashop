using Ecommerce.Application.Models.Authorization;
using Ecommerce.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Ecommerce.Infrastructure.Persistence;

public class EcommerceDbContextData
{
    public static async Task LoadDataAsync(
        EcommerceDbContext context,
        UserManager<Usuario> userManager,
        RoleManager<IdentityRole> roleManager,
        ILoggerFactory loggerFactory
        )
    {
        try
        {
            if(!roleManager.Roles.Any())
            {   
                await roleManager.CreateAsync(new IdentityRole(Role.ADMIN));
                await roleManager.CreateAsync(new IdentityRole(Role.USER));
            }

            if(!userManager.Users.Any())
            {
                var userAdmin = new Usuario
                {
                    Nombre = "Jhon",
                    Apellido = "Wick",
                    Email = "jhon.wick@gmail.com",
                    UserName = "jhon.wick",
                    Telefono ="123456789",
                    AvatarUrl="https://firebasestorage.googleapis.com/v0/b/edificacion-app.appspot.com/o/vaxidrez.jpg?alt=media&token=14a28860-d149-461e-9c25-9774d7ac1b24"
                };

                await userManager.CreateAsync(userAdmin, "Pa$$word.Admin.1234.$");
                await userManager.AddToRoleAsync(userAdmin, Role.ADMIN);

                var user = new Usuario
                {
                    Nombre = "Marco",
                    Apellido = "Polo",
                    Email = "marco.polo@gmail.com",
                    UserName = "marco.polo",
                    Telefono ="58123456789",
                    AvatarUrl="https://firebasestorage.googleapis.com/v0/b/edificacion-app.appspot.com/o/avatar-1.webp?alt=media&token=58da3007-ff21-494d-a85c-25ffa758ff6d"
                };

                await userManager.CreateAsync(user, "Pa$$word.User.1234.$");
                await userManager.AddToRoleAsync(user, Role.USER);
            }

            if(!context.Categories!.Any())
            {
                var categoryData = File.ReadAllText("../Infrastructure/Data/category.json");
                var categories = JsonConvert.DeserializeObject<List<Category>>(categoryData);
                
                await context.Categories!.AddRangeAsync(categories!);
                
                await context.SaveChangesAsync();
            }

            if(!context.Products!.Any())
            {
                var productData = File.ReadAllText("../Infrastructure/Data/product.json");
                var products = JsonConvert.DeserializeObject<List<Product>>(productData);
                
                await context.Products!.AddRangeAsync(products!);
                
                await context.SaveChangesAsync();
            }

            if(!context.Images!.Any())
            {
                var imageData = File.ReadAllText("../Infrastructure/Data/image.json");
                var images = JsonConvert.DeserializeObject<List<Ecommerce.Domain.Image>>(imageData);
                
                await context.Images!.AddRangeAsync(images!);
                
                await context.SaveChangesAsync();
            }

            if(!context.Reviews!.Any())
            {
                var reviewData = File.ReadAllText("../Infrastructure/Data/review.json");
                var reviews = JsonConvert.DeserializeObject<List<Ecommerce.Domain.Review>>(reviewData);
                
                await context.Reviews!.AddRangeAsync(reviews!);
                
                await context.SaveChangesAsync();
            }

            if(!context.Countries!.Any())
            {
                var countryData = File.ReadAllText("../Infrastructure/Data/countries.json");
                var countries = JsonConvert.DeserializeObject<List<Ecommerce.Domain.Country>>(countryData);
                
                await context.Countries!.AddRangeAsync(countries!);
                
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<EcommerceDbContextData>();
            logger.LogError(ex.Message);
        }
    }
}