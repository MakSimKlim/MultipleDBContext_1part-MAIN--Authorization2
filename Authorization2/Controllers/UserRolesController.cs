using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Authorization2.Models;
using Microsoft.AspNetCore.Authorization;

namespace Authorization2.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")] // Attribute restricting access to "UserRoles" for all users except SuperAdmin
    public class UserRolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();
            foreach (ApplicationUser user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.Email = user.Email;
                thisViewModel.FirstName = user.FirstName;
                thisViewModel.LastName = user.LastName;
                thisViewModel.Roles = await GetUserRoles(user);
                userRolesViewModel.Add(thisViewModel);
            }
            return View(userRolesViewModel);
        }
        public async Task<IActionResult> Manage(string userId)
        {
            ViewBag.userId = userId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }
            ViewBag.UserName = user.UserName;
////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Решение: Загружаем роли в память перед циклом
            var roles = await _roleManager.Roles.ToListAsync(); // Материализация запроса
            // Оптимизация: Получаем ВСЕ роли пользователя за один запрос
            var userRoles = await _userManager.GetRolesAsync(user);
////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            var model = new List<ManageUserRolesViewModel>(); // этот код не добавлялся для исправления ошибки

////////////////////////////////////////////////////////////////////////////////////////////////////////
            foreach (var role in roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    //Проверяем роли без дополнительных запросов к БД
                    Selected = userRoles.Contains(role.Name!)
                };
                model.Add(userRolesViewModel);
            }

///////////////////////////////////////////////////////////////////////////////////////////////////////

            //foreach (var role in _roleManager.Roles)
            //{
            //    var userRolesViewModel = new ManageUserRolesViewModel
            //    {
            //        RoleId = role.Id,
            //        RoleName = role.Name
            //    };
            //    if (await _userManager.IsInRoleAsync(user, role.Name!))
            //    {
            //        userRolesViewModel.Selected = true;
            //    }
            //    else
            //    {
            //        userRolesViewModel.Selected = false;
            //    }
            //    model.Add(userRolesViewModel);
            //}

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View();
            }
            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }
            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected == true).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }
            return RedirectToAction("Index");
        }
        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }
    }
}
