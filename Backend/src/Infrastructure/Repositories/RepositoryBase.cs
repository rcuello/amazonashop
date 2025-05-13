using System.Linq.Expressions;
using Ecommerce.Application.Persistence;
using Ecommerce.Application.Specifications;
using Ecommerce.Infrastructure.Specification;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Persistence.Repositories;

/// <summary>
/// Clase base para implementar repositorios que interactúan con la base de datos.
/// Proporciona métodos comunes para operaciones CRUD (Crear, Leer, Actualizar, Eliminar).
/// </summary>
/// <typeparam name="T">El tipo de entidad con la que trabaja el repositorio.</typeparam>
public class RepositoryBase<T> : IAsyncRepository<T> where T : class
{
    // Contexto de la base de datos utilizado para realizar operaciones.
    protected readonly EcommerceDbContext _context;

    /// <summary>
    /// Constructor que inicializa el repositorio con un contexto de base de datos.
    /// </summary>
    /// <param name="context">El contexto de la base de datos.</param>
    public RepositoryBase(EcommerceDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Agrega una entidad a la base de datos de manera asíncrona.
    /// </summary>
    /// <param name="entity">La entidad a agregar.</param>
    /// <returns>La entidad agregada.</returns>
    public async Task<T> AddAsync(T entity)
    {
        _context.Set<T>().Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Agrega una entidad al contexto de la base de datos, pero no guarda los cambios en la base de datos.
    /// Útil para agregar múltiples entidades antes de guardar cambios.
    /// </summary>
    /// <param name="entity">La entidad a agregar.</param>
    public void AddEntity(T entity)
    {
        _context.Set<T>().Add(entity);
    }

    /// <summary>
    /// Agrega una lista de entidades al contexto de la base de datos.
    /// </summary>
    /// <param name="entities">La lista de entidades a agregar.</param>
    public void AddRange(List<T> entities)
    {
        _context.Set<T>().AddRange(entities);
    }

    /// <summary>
    /// Elimina una entidad de la base de datos de manera asíncrona.
    /// </summary>
    /// <param name="entity">La entidad a eliminar.</param>
    public async Task DeleteAsync(T entity)
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Elimina una entidad del contexto de la base de datos, pero no guarda los cambios en la base de datos.
    /// </summary>
    /// <param name="entity">La entidad a eliminar.</param>
    public void DeleteEntity(T entity)
    {
        _context.Set<T>().Remove(entity);
    }

    /// <summary>
    /// Elimina una lista de entidades del contexto de la base de datos.
    /// </summary>
    /// <param name="entities">La lista de entidades a eliminar.</param>
    public void DeleteRange(IReadOnlyList<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
    }

    /// <summary>
    /// Obtiene todas las entidades de la base de datos de manera asíncrona.
    /// </summary>
    /// <returns>Una lista de todas las entidades.</returns>
    public async Task<IReadOnlyList<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    /// <summary>
    /// Obtiene una lista de entidades que cumplen con un predicado específico.
    /// </summary>
    /// <param name="predicate">El predicado para filtrar las entidades.</param>
    /// <returns>Una lista de entidades que cumplen con el predicado.</returns>
    public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate)
    {
        return await _context.Set<T>().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Obtiene una lista de entidades con opciones de filtrado, ordenamiento y carga de relaciones.
    /// </summary>
    /// <param name="predicate">El predicado para filtrar las entidades.</param>
    /// <param name="orderBy">Función para ordenar las entidades.</param>
    /// <param name="includeString">Cadena que especifica las relaciones a incluir.</param>
    /// <param name="disableTracking">Indica si se deshabilita el seguimiento de cambios en las entidades.</param>
    /// <returns>Una lista de entidades que cumplen con los criterios especificados.</returns>
    public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy, string? includeString, bool disableTracking = true)
    {
        IQueryable<T> query = _context.Set<T>();

        if (disableTracking) query = query.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(includeString)) query = query.Include(includeString);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) return await orderBy(query).ToListAsync();

        return await query.ToListAsync();
    }

    /// <summary>
    /// Obtiene una lista de entidades con opciones de filtrado, ordenamiento y carga de relaciones mediante expresiones.
    /// </summary>
    /// <param name="predicate">El predicado para filtrar las entidades.</param>
    /// <param name="orderBy">Función para ordenar las entidades.</param>
    /// <param name="includes">Lista de expresiones para cargar relaciones.</param>
    /// <param name="disableTracking">Indica si se deshabilita el seguimiento de cambios en las entidades.</param>
    /// <returns>Una lista de entidades que cumplen con los criterios especificados.</returns>
    public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, List<Expression<Func<T, object>>>? includes = null, bool disableTracking = true)
    {
        IQueryable<T> query = _context.Set<T>();

        if (disableTracking) query = query.AsNoTracking();

        if (includes != null) query = includes.Aggregate(query, (current, include) => current.Include(include));

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) return await orderBy(query).ToListAsync();

        return await query.ToListAsync();
    }

    /// <summary>
    /// Obtiene una entidad por su identificador de manera asíncrona.
    /// </summary>
    /// <param name="id">El identificador de la entidad.</param>
    /// <returns>La entidad encontrada.</returns>
    public async Task<T> GetByIdAsync(int id)
    {
        return (await _context.Set<T>().FindAsync(id))!;
    }

    /// <summary>
    /// Obtiene una entidad que cumple con un predicado específico, con opciones de carga de relaciones.
    /// </summary>
    /// <param name="predicate">El predicado para filtrar la entidad.</param>
    /// <param name="includes">Lista de expresiones para cargar relaciones.</param>
    /// <param name="disableTracking">Indica si se deshabilita el seguimiento de cambios en la entidad.</param>
    /// <returns>La entidad que cumple con el predicado.</returns>
    public async Task<T> GetEntityAsync(Expression<Func<T, bool>>? predicate, List<Expression<Func<T, object>>>? includes = null, bool disableTracking = true)
    {
        IQueryable<T> query = _context.Set<T>();

        if (disableTracking) query = query.AsNoTracking();

        if (includes != null) query = includes.Aggregate(query, (current, include) => current.Include(include));

        if (predicate != null) query = query.Where(predicate);

        return (await query.FirstOrDefaultAsync())!;
    }

    /// <summary>
    /// Actualiza una entidad en la base de datos de manera asíncrona.
    /// </summary>
    /// <param name="entity">La entidad a actualizar.</param>
    /// <returns>La entidad actualizada.</returns>
    public async Task<T> UpdateAsync(T entity)
    {
        _context.Set<T>().Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return entity;
    }

    /// <summary>
    /// Actualiza una entidad en el contexto de la base de datos, pero no guarda los cambios en la base de datos.
    /// </summary>
    /// <param name="entity">La entidad a actualizar.</param>
    public void UpdateEntity(T entity)
    {
        _context.Set<T>().Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).CountAsync();
    }

    public async Task<IReadOnlyList<T>> GetAllWithSpec(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public async Task<T> GetByIdWithSpec(ISpecification<T> spec)
    {
        return (await ApplySpecification(spec).FirstOrDefaultAsync())!;
    }

    public IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {
        return SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), spec);
    }
}