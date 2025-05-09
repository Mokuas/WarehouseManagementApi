using Microsoft.AspNetCore.Mvc;
using Tutorial9.Services;
using Tutorial9.Model;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] WarehouseDto dto)
    {
        try
        {
            var result = await _dbService.AddProductToWarehouseAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost("procedure")]
    public async Task<IActionResult> AddProductToWarehouseUsingProcedure([FromBody] WarehouseDto dto)
    {
        try
        {
            var result = await _dbService.AddProductToWarehouseUsingProcedureAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message); // DEBUG 
        }
    }
}