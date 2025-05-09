using System.Data;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;

    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductToWarehouseAsync(WarehouseDto dto)
{
    await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
    await connection.OpenAsync();

    await using var command = new SqlCommand();
    command.Connection = connection;

    var transaction = await connection.BeginTransactionAsync();
    command.Transaction = (SqlTransaction)transaction;

    try
    {
        command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        var productExists = await command.ExecuteScalarAsync();
        if (productExists == null)
            throw new InvalidOperationException("Product not found.");
        command.Parameters.Clear();
        
        command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        var warehouseExists = await command.ExecuteScalarAsync();
        if (warehouseExists == null)
            throw new InvalidOperationException("Warehouse not found.");
        command.Parameters.Clear();
        
        command.CommandText = @"
            SELECT TOP 1 o.IdOrder
            FROM [Order] o
            LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder
            WHERE o.IdProduct = @IdProduct AND o.Amount = @Amount AND o.CreatedAt < @CreatedAt
              AND pw.IdOrder IS NULL
            ORDER BY o.CreatedAt ASC";
        command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        command.Parameters.AddWithValue("@Amount", dto.Amount);
        command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
        var idOrderObj = await command.ExecuteScalarAsync();
        if (idOrderObj == null)
            throw new InvalidOperationException("Valid unfulfilled order not found.");
        int idOrder = Convert.ToInt32(idOrderObj);
        command.Parameters.Clear();
        
        command.CommandText = "UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        
        command.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        var priceObj = await command.ExecuteScalarAsync();
        if (priceObj == null)
            throw new InvalidOperationException("Product price not found.");
        decimal price = (decimal)priceObj;
        command.Parameters.Clear();
        
        command.CommandText = @"
            INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        command.Parameters.AddWithValue("@Amount", dto.Amount);
        command.Parameters.AddWithValue("@Price", price * dto.Amount);

        var insertedId = await command.ExecuteScalarAsync();
        await transaction.CommitAsync();

        return Convert.ToInt32(insertedId);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

    public async Task<int> AddProductToWarehouseUsingProcedureAsync(WarehouseDto dto)
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", dto.Amount);
        command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

        await connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}