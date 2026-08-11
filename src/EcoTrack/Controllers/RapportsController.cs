using EcoTrack.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoTrack.Controllers
{
    [Authorize(Roles = "AdminPrincipal,AdminTemporaire")]
    public partial class RapportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RapportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Rapports
        public IActionResult Index()
        {
            return View();
        }
    }
}