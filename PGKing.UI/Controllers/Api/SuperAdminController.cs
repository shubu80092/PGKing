using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PGKing.Application.DTOs;
using PGKing.Application.Entities;
using PGKing.Application.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PGKing.UI.Controllers.Api
{
    [ApiController]
    [Route("api/superadmin/vendors")]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IMapper _mapper;

        public SuperAdminController(IVendorRepository vendorRepository, IMapper mapper)
        {
            _vendorRepository = vendorRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVendors()
        {
            var vendors = await _vendorRepository.GetAllAsync();
            var result = _mapper.Map<IEnumerable<VendorResponseDto>>(vendors);
            return Ok(ApiResponse<IEnumerable<VendorResponseDto>>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVendor(int id)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id);
            if (vendor == null) return NotFound(ApiResponse<object>.Fail("Vendor not found"));

            var result = _mapper.Map<VendorResponseDto>(vendor);
            return Ok(ApiResponse<VendorResponseDto>.Ok(result));
        }

        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] VendorCreateDto dto)
        {
            var existing = await _vendorRepository.GetByEmailAsync(dto.Email);
            if (existing != null) return BadRequest(ApiResponse<object>.Fail("Email already in use"));

            var vendor = _mapper.Map<Vendor>(dto);
            vendor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            
            await _vendorRepository.AddAsync(vendor);

            var result = _mapper.Map<VendorResponseDto>(vendor);
            return CreatedAtAction(nameof(GetVendor), new { id = vendor.VendorId }, ApiResponse<VendorResponseDto>.Ok(result));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] VendorUpdateDto dto)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id);
            if (vendor == null) return NotFound(ApiResponse<object>.Fail("Vendor not found"));

            _mapper.Map(dto, vendor);
            vendor.ModifiedDate = System.DateTime.UtcNow;

            await _vendorRepository.UpdateAsync(vendor);
            var result = _mapper.Map<VendorResponseDto>(vendor);
            return Ok(ApiResponse<VendorResponseDto>.Ok(result));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id);
            if (vendor == null) return NotFound(ApiResponse<object>.Fail("Vendor not found"));

            await _vendorRepository.DeleteAsync(vendor);
            return Ok(ApiResponse<object>.Ok(null, "Vendor deleted"));
        }
    }
}
