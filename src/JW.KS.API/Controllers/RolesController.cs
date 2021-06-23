using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JW.KS.API.Authorization;
using JW.KS.API.Constants;
using JW.KS.API.Data;
using JW.KS.API.Data.Entities;
using JW.KS.ViewModels;
using JW.KS.ViewModels.Systems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JW.KS.API.Controllers
{
    public class RolesController : BaseController
    {
        private readonly RoleManager<IdentityRole> _manager;
        private readonly ApplicationDbContext _context;
        public RolesController(RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _manager = roleManager;
            _context = context;
        }

        [HttpPost]
        [ClaimRequirement(FunctionCode.SYSTEM_ROLE, CommandCode.CREATE)]
        public async Task<IActionResult> PostRole(RoleCreateRequest request)
        {
            var role = new IdentityRole()
            {
                Id = request.Id,
                Name = request.Name,
                NormalizedName = request.Name.ToUpper()
            };
            var result = await _manager.CreateAsync(role);
            if (result.Succeeded)
            {
                return CreatedAtAction(nameof(GetById), new {id = role.Id}, request);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpGet]
        [ClaimRequirement(FunctionCode.SYSTEM_ROLE, CommandCode.VIEW)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _manager.Roles.Select(r => new RoleVm
            {
                Id = r.Id,
                Name = r.Name
            }).ToListAsync();

            return Ok(roles);
        }
        
        [HttpGet("filter")]
        [ClaimRequirement(FunctionCode.SYSTEM_ROLE, CommandCode.VIEW)]
        public async Task<IActionResult> GetRolesPaging(string filter, int page, int size)
        {
            var query = _manager.Roles;
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(x => x.Id.Contains(filter) || x.Name.Contains(filter));
            }

            var totalRecords = await query.CountAsync();
            var items = await query.Skip((page - 1) * size)
                .Take(size)
                .Select(x => new RoleVm()
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync();

            var pagination = new Pagination<RoleVm>
            {
                Items = items,
                TotalRecords = totalRecords
            };
            
            return Ok(pagination);
        }

        [HttpGet("{id}")]
        [ClaimRequirement(FunctionCode.SYSTEM_ROLE, CommandCode.VIEW)]
        public async Task<IActionResult> GetById(string id)
        {
            var role = await _manager.FindByIdAsync(id);
            if (role == null)
                return NotFound();
            var roleVm = new RoleVm()
            {
                Id = role.Id,
                Name = role.Name
            };
            return Ok(roleVm);
        }

        [HttpPut("{id}")]
        [ClaimRequirement(FunctionCode.SYSTEM_ROLE, CommandCode.UPDATE)]
        public async Task<IActionResult> PutRole(string id, [FromBody]RoleCreateRequest request)
        {
            if (id != request.Id)
                return BadRequest();
            var role = await _manager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            role.Name = request.Name;
            role.NormalizedName = request.Name.ToUpper();

            var result = await _manager.UpdateAsync(role);
            if (result.Succeeded)
            {
                return NoContent();
            }

            return BadRequest(result.Errors);
        }

        [HttpDelete("{id}")]
        [ClaimRequirement(FunctionCode.SYSTEM_ROLE, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _manager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var result = await _manager.DeleteAsync(role);
            if (result.Succeeded)
            {
                var roleVm = new RoleVm()
                {
                    Id = role.Id,
                    Name = role.Name
                };
                return Ok(roleVm);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("{roleId}/permissions")]
        [ClaimRequirement(FunctionCode.SYSTEM_PERMISSION, CommandCode.VIEW)]
        public async Task<IActionResult> GetPermissionByRoleId(string roleId)
        {
            var permissions = from p in _context.Permissions

                              join a in _context.Commands
                              on p.CommandId equals a.Id
                              where p.RoleId == roleId
                              select new PermissionVm()
                              {
                                  FunctionId = p.FunctionId,
                                  CommandId = p.CommandId,
                                  RoleId = p.RoleId
                              };

            return Ok(await permissions.ToListAsync());
        }

        [HttpPut("{roleId}/permissions")]
        [ClaimRequirement(FunctionCode.SYSTEM_PERMISSION, CommandCode.UPDATE)]
        public async Task<IActionResult> PutPermissionByRoleId(string roleId, [FromBody] UpdatePermissionRequest request)
        {
            //create new permission list from user changed
            var newPermissions = new List<Permission>();
            foreach (var p in request.Permissions)
            {
                newPermissions.Add(new Permission(p.FunctionId, roleId, p.CommandId));
            }

            var existingPermissions = _context.Permissions.Where(x => x.RoleId == roleId);
            
            _context.Permissions.RemoveRange(existingPermissions);
            _context.Permissions.AddRange(newPermissions.Distinct(new MyPermissionComparer()));
            
            var result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest();
        }
    }
    
    internal class MyPermissionComparer : IEqualityComparer<Permission>
    {
        // Items are equal if their ids are equal.
        public bool Equals(Permission x, Permission y)
        {
            // Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            // Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            //Check whether the items properties are equal.
            return x.CommandId == y.CommandId && x.FunctionId == x.FunctionId && x.RoleId == x.RoleId;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Permission permission)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(permission, null)) return 0;

            //Get hash code for the ID field.
            int hashProductId = (permission.CommandId + permission.FunctionId + permission.RoleId).GetHashCode();

            return hashProductId;
        }
    }
}