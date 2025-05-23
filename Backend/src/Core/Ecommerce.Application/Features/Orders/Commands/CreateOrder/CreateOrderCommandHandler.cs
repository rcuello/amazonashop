using System.Linq.Expressions;
using AutoMapper;
using Ecommerce.Application.Exceptions.App;
using Ecommerce.Application.Features.Orders.Vms;
using Ecommerce.Application.Identity;
using Ecommerce.Application.Models.Payment;
using Ecommerce.Application.Persistence;
using Ecommerce.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Stripe;

namespace Ecommerce.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderVm>
{
    
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuthService _authService;
    private readonly UserManager<Usuario> _userManager;
    private readonly StripeSettings _stripeSettings;

    public CreateOrderCommandHandler(IUnitOfWork unitOfWork, 
        IMapper mapper, 
        IAuthService authService, 
        UserManager<Usuario> userManager, 
        IOptions<StripeSettings> stripeSettings
    )
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _authService = authService;
        _userManager = userManager;
        _stripeSettings = stripeSettings.Value;
    }

    public async Task<OrderVm> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderPending = await _unitOfWork.Repository<Order>().GetEntityAsync(
            x => x.CompradorUsername == _authService.GetSessionUser() && x.Status == OrderStatus.Pending,
            null,
            true
        );

        if(orderPending is not null)
        {
            await _unitOfWork.Repository<Order>().DeleteAsync(orderPending);
        }


        var includes = new List<Expression<Func<ShoppingCart, object>>>();
        includes.Add(p => p.ShoppingCartItems!.OrderBy(x => x.Producto));


        var shoppingCart = await _unitOfWork.Repository<ShoppingCart>().GetEntityAsync(
            x => x.ShoppingCartMasterId == request.ShoppingCartId,
            includes,
            false
        );

        if (shoppingCart is null || shoppingCart.ShoppingCartItems is null || !shoppingCart.ShoppingCartItems.Any())
        {
            throw new ShoppingCartException("El carrito de compras est� vac�o o no existe");
        }

        var user = await _userManager.FindByNameAsync(_authService.GetSessionUser());
        if(user  is null)
        {
            throw new UserAuthenticationException("El usuario no esta autenticado");
        }

        var direccion = await _unitOfWork.Repository<Ecommerce.Domain.Address>().GetEntityAsync(
            x => x.Username == user.UserName,
            null,
            false
        );

        if (direccion is null)
        {
            throw new AddressNotFoundException(user.UserName!);
        }

        OrderAddress orderAddress = new (){
            Direccion = direccion.Direccion,
            Ciudad = direccion.Ciudad,
            CodigoPostal = direccion.CodigoPostal,
            Pais = direccion.Pais,
            Departamento = direccion.Departamento,
            Username = direccion.Username
        };

        await _unitOfWork.Repository<OrderAddress>().AddAsync(orderAddress);

        var subtotal =  Math.Round( shoppingCart.ShoppingCartItems!.Sum(x=>x.Precio*x.Cantidad) , 2);
        var impuesto =    Math.Round(  subtotal *Convert.ToDecimal(0.18) , 2);
        var precioEnvio =  subtotal < 100 ? 10 : 25;
        var total = subtotal + impuesto + precioEnvio;

        var nombreComprador = $"{user.Nombre}  {user.Apellido}";
        var order = new Order(nombreComprador, user.UserName!, orderAddress, subtotal, total, impuesto, precioEnvio);

        await _unitOfWork.Repository<Order>().AddAsync(order);

        var items = new List<OrderItem>();

        foreach(var shoppingElement in shoppingCart.ShoppingCartItems!)
        {
            var orderItem = new OrderItem
            {
                ProductNombre = shoppingElement.Producto,
                ProductId = shoppingElement.ProductId,
                ImagenUrl = shoppingElement.Imagen,
                Precio = shoppingElement.Precio,
                Cantidad = shoppingElement.Cantidad,
                OrderId = order.Id
            };

            items.Add(orderItem);
        }

        _unitOfWork.Repository<OrderItem>().AddRange(items);

        var resultado =  await _unitOfWork.Complete();

        if (resultado <= 0)
        {
            throw new OrderCreationException("Error creando la orden de compra");
        }

        try
        {
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            var service = new PaymentIntentService();
            PaymentIntent intent;

            if (string.IsNullOrEmpty(order.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)order.Total,
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" }
                };

                intent = await service.CreateAsync(options);
                order.PaymentIntentId = intent.Id;
                order.ClientSecret = intent.ClientSecret;
                order.StripeApiKey = _stripeSettings.PublishbleKey;
            }
            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = (long)order.Total
                };
                await service.UpdateAsync(order.PaymentIntentId, options);
            }


            _unitOfWork.Repository<Order>().UpdateEntity(order);

            var resultadoOrder = await _unitOfWork.Complete();

            if (resultadoOrder <= 0)
            {
                throw new PaymentProcessingException("Error actualizando la orden con la informaci�n de pago", order.PaymentIntentId);
            }
        }
        catch (StripeException ex)
        {
            throw new PaymentProcessingException($"Error en la integraci�n con Stripe: {ex.Message}", order.PaymentIntentId);
        }


        return _mapper.Map<OrderVm>(order);
    }
}