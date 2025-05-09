using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task<int> AddProductToWarehouseAsync(WarehouseDto dto);
    Task<int> AddProductToWarehouseUsingProcedureAsync(WarehouseDto dto);
}