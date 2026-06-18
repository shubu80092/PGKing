using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGKing.Application.DTOs;
using PGKing.Application.Entities;
using PGKing.Application.Interfaces.Services;

namespace PGKing.UI.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "SuperAdmin")]
    public class PropertiesController : ControllerBase
    {
        private readonly IPropertyService _propertyService;

        public PropertiesController(IPropertyService propertyService)
        {
            _propertyService = propertyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var properties = await _propertyService.GetAllPropertiesAsync();
            return Ok(ApiResponse<object>.Ok(properties, "Properties retrieved successfully"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var property = await _propertyService.GetPropertyByIdAsync(id);
            if (property == null) return NotFound(ApiResponse<object>.Fail("Property not found"));
            
            return Ok(ApiResponse<Property>.Ok(property, "Property retrieved successfully"));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Validation failed"));

            var property = await _propertyService.CreatePropertyAsync(request);
            return Ok(ApiResponse<Property>.Ok(property, "Property created successfully"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreatePropertyRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Validation failed"));

            try
            {
                await _propertyService.UpdatePropertyAsync(id, request);
                return Ok(ApiResponse<object>.Ok(null, "Property updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _propertyService.DeletePropertyAsync(id);
                return Ok(ApiResponse<object>.Ok(null, "Property deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }
    }
}
