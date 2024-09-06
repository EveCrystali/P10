using System;
using BackendPatient.Data;
using BackendPatient.Models;
using BackendPatient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendPatient.Services;

public class UpdateService<T>(ApplicationDbContext dbContext) : IUpdateService<T> where T : class
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    
    /// <summary>
    /// This service is used to update an entity in the database.
    /// It checks if the entity to be updated is valid, if the id parameter is the same as the id of the entity in the body,
    /// and if the entity exists in the database.
    /// </summary>
    /// <typeparam name="T">The type of entity to be updated.</typeparam>
    /// <param name="id">The id of the entity to be updated.</param>
    /// <param name="entity">The updated entity.</param>
    /// <param name="existsFunc">A function that checks if the entity exists in the database.</param>
    /// <param name="getIdFunc">A function that gets the id of the entity.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation. The task result contains the HTTP response.</returns>
    public async Task<IActionResult> UpdateEntity(int id, T entity, Func<T, bool> existsFunc, Func<T, int> getIdFunc)
    {
        // Check if the id parameter is the same as the id of the entity in the body
        if (id != getIdFunc(entity))
        {
            return new BadRequestObjectResult("The Id entered in the parameter is not the same as the Id enter in the body");
        }

        try
        {
            // Check if the entity is valid
            System.Reflection.MethodInfo? validationMethod = entity.GetType().GetMethod("Validate");
            validationMethod?.Invoke(entity, null);
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }

        _dbContext.Entry(entity).State = EntityState.Modified;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Check if the entity exists in the database
            if (!existsFunc(entity))
            {
                return new NotFoundObjectResult($"{typeof(T).Name} with this Id does not exist");
            }
            else
            {
                return new ConflictResult();
            }
        }

        return new NoContentResult();
    }

}
