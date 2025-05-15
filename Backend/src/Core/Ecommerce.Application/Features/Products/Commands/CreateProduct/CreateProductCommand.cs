using Ecommerce.Application.Features.Products.Queries.Vms;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Runtime.Serialization;

namespace Ecommerce.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommand : IRequest<ProductVm>
{
    public string? Nombre { get; set; }
    public decimal Precio { get; set; }

    public string? Descripcion { get; set; }

    public string? Vendedor { get; set; }

    public int Stock { get; set; }


    public string? CategoryId { get; set; }

    public List<IFormFile>? Imagenes { get; set; }

    [IgnoreDataMember]
    public IReadOnlyList<CreateProductImageCommand>? ImageUrls {get;set;}

}