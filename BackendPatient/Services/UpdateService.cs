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
        Console.WriteLine($"Updating {typeof(T).Name} with id {id}");

        // Check if the id parameter is the same as the id of the entity in the body
        if (id != getIdFunc(entity))
        {
            Console.WriteLine($"The id parameter {id} is not the same as the id of the entity in the body {getIdFunc(entity)}");
            return new BadRequestObjectResult($"The id parameter {id} is not the same as the id of the entity in the body {getIdFunc(entity)}");
        }

        try
        {
            Console.WriteLine("Validating the entity");
            // Check if the entity is valid
            System.Reflection.MethodInfo? validationMethod = entity.GetType().GetMethod("Validate");
            validationMethod?.Invoke(entity, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Validation of the entity failed with the following exception: {ex}");
            return new BadRequestObjectResult(ex.Message);
        }

        Console.WriteLine("Saving the entity to the database");
        _dbContext.Entry(entity).State = EntityState.Modified;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            Console.WriteLine("A DbUpdateConcurrencyException occurred while saving the entity to the database");
            // Check if the entity exists in the database
            if (!existsFunc(entity))
            {
                Console.WriteLine($"{typeof(T).Name} with this Id does not exist");
                return new NotFoundObjectResult($"{typeof(T).Name} with this Id does not exist");
            }
            else
            {
                Console.WriteLine("A conflict occurred while saving the entity to the database");
                return new ConflictResult();
            }
        }

        Console.WriteLine("The entity was successfully updated");
        return new OkObjectResult(entity);
    }


}
