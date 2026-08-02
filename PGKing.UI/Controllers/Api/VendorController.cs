using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGKing.Application.DTOs;
using PGKing.Application.Entities;
using PGKing.Application.Interfaces.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PGKing.UI.Controllers.Api
{
    [ApiController]
    [Route("api/vendor/tenants")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Vendor,SuperAdmin")]
    public class VendorController : ControllerBase
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IMapper _mapper;

        public VendorController(ITenantRepository tenantRepository, IMapper mapper)
        {
            _tenantRepository = tenantRepository;
            _mapper = mapper;
        }

        private int GetVendorId()
        {
            var vendorIdClaim = User.Claims.FirstOrDefault(c => c.Type == "VendorId")?.Value;
            int.TryParse(vendorIdClaim, out int vendorId);
            return vendorId;
        }

        [HttpGet]
        public async Task<IActionResult> GetTenants()
        {
            IEnumerable<Tenant> tenants;
            if (User.IsInRole("SuperAdmin"))
            {
                tenants = await _tenantRepository.GetAllAsync();
            }
            else
            {
                var vendorId = GetVendorId();
                tenants = await _tenantRepository.GetByVendorIdAsync(vendorId);
            }
            var result = _mapper.Map<IEnumerable<TenantResponseDto>>(tenants);
            return Ok(ApiResponse<IEnumerable<TenantResponseDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTenant(int id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                return NotFound(ApiResponse<object>.Fail("Tenant not found"));
            
            if (!User.IsInRole("SuperAdmin"))
            {
                var vendorId = GetVendorId();
                if (tenant.VendorId != vendorId) 
                    return NotFound(ApiResponse<object>.Fail("Tenant not found"));
            }

            var result = _mapper.Map<TenantResponseDto>(tenant);
            return Ok(ApiResponse<TenantResponseDto>.Ok(result));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTenant([FromBody] TenantCreateDto dto)
        {
            if (User.IsInRole("SuperAdmin"))
            {
                return BadRequest(ApiResponse<object>.Fail("SuperAdmins cannot create a tenant here without specifying a VendorId. Use SuperAdmin endpoints."));
            }

            var vendorId = GetVendorId();
            
            var existing = await _tenantRepository.GetByEmailAsync(dto.Email);
            if (existing != null) return BadRequest(ApiResponse<object>.Fail("Email already in use"));

            var tenant = _mapper.Map<Tenant>(dto);
            tenant.VendorId = vendorId;
            tenant.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            
            await _tenantRepository.AddAsync(tenant);

            var result = _mapper.Map<TenantResponseDto>(tenant);
            return CreatedAtAction(nameof(GetTenant), new { id = tenant.TenantId }, ApiResponse<TenantResponseDto>.Ok(result));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(int id, [FromBody] TenantUpdateDto dto)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                return NotFound(ApiResponse<object>.Fail("Tenant not found"));
            
            if (!User.IsInRole("SuperAdmin"))
            {
                var vendorId = GetVendorId();
                if (tenant.VendorId != vendorId) 
                    return NotFound(ApiResponse<object>.Fail("Tenant not found"));
            }

            _mapper.Map(dto, tenant);
            tenant.ModifiedDate = System.DateTime.UtcNow;

            await _tenantRepository.UpdateAsync(tenant);
            var result = _mapper.Map<TenantResponseDto>(tenant);
            return Ok(ApiResponse<TenantResponseDto>.Ok(result));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant(int id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null)
                return NotFound(ApiResponse<object>.Fail("Tenant not found"));
            
            if (!User.IsInRole("SuperAdmin"))
            {
                var vendorId = GetVendorId();
                if (tenant.VendorId != vendorId) 
                    return NotFound(ApiResponse<object>.Fail("Tenant not found"));
            }

            await _tenantRepository.DeleteAsync(tenant);
            return Ok(ApiResponse<object>.Ok(null, "Tenant deleted"));
        }
    }
}
