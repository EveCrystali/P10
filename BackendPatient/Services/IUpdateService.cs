using Microsoft.AspNetCore.Mvc;

namespace BackendPatient.Services;

public interface IUpdateService<T> where T : class
{
    Task<IActionResult> UpdateEntity(int id, T entity, Func<T, bool> existsFunc, Func<T, int> getIdFunc);
}