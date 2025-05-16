using System.Net;
using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Features.Products.Commands.CreateProduct;
using Ecommerce.Application.Features.Products.Commands.DeleteProduct;
using Ecommerce.Application.Features.Products.Commands.UpdateProduct;
using Ecommerce.Application.Features.Products.Queries.GetProductById;
using Ecommerce.Application.Features.Products.Queries.GetProductList;
using Ecommerce.Application.Features.Products.Queries.PaginationProducts;
using Ecommerce.Application.Features.Products.Queries.Vms;
using Ecommerce.Application.Features.Shared.Queries;
using Ecommerce.Application.Models.Authorization;
using Ecommerce.Application.Models.ImageManagment;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProductController : ControllerBase
    {
        private IMediator _mediator;
        private IManageImageService _manageImageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IMediator mediator, IManageImageService manageImageService, ILogger<ProductController> logger)
        {
            _mediator = mediator;
            _manageImageService = manageImageService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("list", Name = "GetProductList")]
        [ProducesResponseType(typeof(IReadOnlyList<ProductVm>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IReadOnlyList<ProductVm>>> GetProductList()
        {
            var query = new GetProductListQuery();
            var productos = await _mediator.Send(query);
            return Ok(productos);
        }

        [AllowAnonymous]
        [HttpGet("pagination", Name = "PaginationProduct")]
        [ProducesResponseType(typeof(PaginationVm<ProductVm>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PaginationVm<ProductVm>>> PaginationProduct(
            [FromQuery] PaginationProductsQuery paginationProductsQuery
        )
        {
            paginationProductsQuery.Status = ProductStatus.Activo;
            var paginationProduct = await _mediator.Send(paginationProductsQuery);
            return Ok(paginationProduct);
        }

        [AllowAnonymous]
        [HttpGet("{id}", Name = "GetProductById")]
        [ProducesResponseType(typeof(ProductVm), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ProductVm>> GetProductById(int id)
        {
            var query = new GetProductByIdQuery(id);
            return Ok(await _mediator.Send(query));
        }

        [Authorize(Roles = Role.ADMIN)]
        [HttpPost("create", Name = "CreateProduct")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<ActionResult<ProductVm>> CreateProduct([FromForm] CreateProductCommand request)
        {           
            // Verificar si llegaron fotos
            if (request.Fotos == null || request.Fotos.Count == 0)
            {
                _logger.LogWarning("No se recibieron imágenes en la solicitud");
            }
            else
            {
                _logger.LogInformation($"Recibidas {request.Fotos.Count} fotos");

                var listFotoUrls = new List<CreateProductImageCommand>();

                foreach (var foto in request.Fotos)
                {
                    _logger.LogInformation($"Procesando foto: {foto.FileName}, Tamaño: {foto.Length} bytes");

                    try
                    {
                        var resultImage = await _manageImageService.UploadImage(new ImageData
                        {
                            ImageStream = foto.OpenReadStream(),
                            Nombre = foto.FileName // Usar FileName en lugar de Name
                        });

                        var fotoCommand = new CreateProductImageCommand
                        {
                            PublicCode = resultImage.PublicId,
                            Url = resultImage.Url
                        };

                        listFotoUrls.Add(fotoCommand);
                        _logger.LogInformation($"Imagen subida exitosamente: {resultImage.Url}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al procesar la imagen {foto.FileName}");
                        return BadRequest($"Error al procesar la imagen {foto.FileName}: {ex.Message}");
                    }
                }

                request.ImageUrls = listFotoUrls;
            }

            try
            {
                var result = await _mediator.Send(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el producto");
                return BadRequest($"Error al crear el producto: {ex.Message}");
            }
        }



        [Authorize(Roles = Role.ADMIN)]
        [HttpPut("update", Name = "UpdateProduct")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult<ProductVm>> UpdateProduct([FromForm] UpdateProductCommand request)
        {
            var listFotoUrls = new List<CreateProductImageCommand>();

            if (request.Fotos is not null)
            {
                foreach (var foto in request.Fotos)
                {
                    var resultImage = await _manageImageService.UploadImage(new ImageData
                    {
                        ImageStream = foto.OpenReadStream(),
                        Nombre = foto.Name
                    });

                    var fotoCommand = new CreateProductImageCommand
                    {
                        PublicCode = resultImage.PublicId,
                        Url = resultImage.Url
                    };

                    listFotoUrls.Add(fotoCommand);
                }
                request.ImageUrls = listFotoUrls;
            }

            return await _mediator.Send(request);

        }




        [Authorize(Roles = Role.ADMIN)]
        [HttpDelete("status/{id}", Name = "UpdateStatusProduct")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult<ProductVm>> UpdateStatusProduct(int id)
        {
            var request = new DeleteProductCommand(id);
            return await _mediator.Send(request);
        }
    }
}
