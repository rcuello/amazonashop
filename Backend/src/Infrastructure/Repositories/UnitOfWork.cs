using System.Collections;
using Ecommerce.Application.Persistence;

namespace Ecommerce.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private Hashtable? _repositories;
    private EcommerceDbContext _context;
    public UnitOfWork(EcommerceDbContext context)
    {
        _context = context;
    }

    public async Task<int> Complete()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception ("Error en transacción de base de datos", ex);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public IAsyncRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        if(_repositories == null)
        {
            _repositories = new Hashtable();
        }

        var type = typeof(TEntity).Name;

        if(!_repositories.ContainsKey(type)){
            var repositoryType = typeof(RepositoryBase<>);
            var respositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(TEntity)), _context);
            _repositories.Add(type, respositoryInstance);
        }

        return (IAsyncRepository<TEntity>)_repositories[type]!;
    }
}