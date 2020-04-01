using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LMS4Carroll.Data;
using LMS4Carroll.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LMS4Carroll.Controllers
{
    [Authorize(Roles = "Admin,PhysicsUser")]
    public class DisposablesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IConfiguration configuration;

        public DisposablesController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            this.configuration = config;
        }

        // GET: Disposable
        public async Task<IActionResult> Index(string dispostring, string sortOrder)
        {
            ViewData["CurrentFilder"] = dispostring;
            sp_Logging("1-Info", "View", "Sucessfully viewed Disposables list", "Success");
            var disposables = from m in _context.Disposable
                              select m;

            

            // Search Feature
            if (!String.IsNullOrEmpty(dispostring))
            {
                int forID;
                if (Int32.TryParse(dispostring, out forID))
                {
                    disposables = disposables.Where(s => s.DispoID.Equals(forID));
                }
                else
                {
                    disposables = disposables.Where(s => s.DispoName.Contains(dispostring)
                                            || s.Comments.ToLower().Contains(dispostring));
                }
            }

            // Sort Feature
            ViewData["DispoNameSort"] = String.IsNullOrEmpty(sortOrder) ? "DispoNameSort_desc" : "";
            ViewData["DispoIDSort"] = sortOrder == "DispoIDSort" ? "DispoIDSort_desc" : "DispoIDSort";
            ViewData["CommentsSort"] = sortOrder == "CommentsSort" ? "CommentsSort_desc" : "CommentsSort";

            switch (sortOrder)
            {
                // Ascending
                case "DispoIDSort":
                    disposables = disposables.OrderBy(x => x.DispoID);
                    break;
                case "DispoNameSort":
                    disposables = disposables.OrderBy(x => x.DispoName);
                    break;
                case "CommentsSort":
                    disposables = disposables.OrderBy(x => x.Comments);
                    break;

                // Descending
                case "DispoNameSort_desc":
                    disposables = disposables.OrderByDescending(x => x.DispoName);
                    break;
                case "CommentsSort_desc":
                    disposables = disposables.OrderByDescending(x => x.Comments);
                    break;

                default:
                    disposables = disposables.OrderByDescending(x => x.DispoID);
                    break;
            }


            return View(await disposables.ToListAsync());
        }

        // GET: Disposable/Details/5
        [Authorize(Roles = "Admin,PhysicsUser")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var disposable = await _context.Disposable.SingleOrDefaultAsync(m => m.DispoID == id);
            if (disposable == null)
            {
                return NotFound();
            }

            return View(disposable);
        }

        // GET: Disposable/Create
        [Authorize(Roles = "Admin,PhysicsUser")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Disposable/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,PhysicsUser")]
        public async Task<IActionResult> Create([Bind("DispoID,DispoName,Comments")] Disposable disposable)
        {
            if (ModelState.IsValid)
            {
                _context.Add(disposable);
                await _context.SaveChangesAsync();
                sp_Logging("2-Change", "Create", "User created a disposable where name is " + disposable.DispoName, "Success");
                return RedirectToAction("Index");
            }
            return View(disposable);
        }

        // GET: Disposables/Edit/5
        [Authorize(Roles = "Admin,PhysicsUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var disposable = await _context.Disposable.SingleOrDefaultAsync(m => m.DispoID == id);
            if (disposable == null)
            {
                return NotFound();
            }

            return View(disposable);
        }

        // POST: Disposable/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,PhysicsUser")]
        public async Task<IActionResult> Edit(int id, [Bind("DispoID,DispoName,Comments")] Disposable disposable)
        {
            if (id != disposable.DispoID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(disposable);
                    await _context.SaveChangesAsync();
                    sp_Logging("2-Change", "Edit", "User edited a disposable where ID= " + id.ToString(), "Success");
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction("Index");
            }
            return View(disposable);
        }

        // GET: Disposable/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id, bool itemInUse)
        {
            if (id == null)
            {
                return NotFound();
            }

            // display Item In Use if delete doesn't work
            if (itemInUse)
            {
                var temp = _context.Disposable.First(n => n.DispoID == id);
                ViewData["ItemInUse"] = temp.DispoName.ToString() + " ID: " + temp.DispoID.ToString();
            }

            var disposable = await _context.Disposable.SingleOrDefaultAsync(m => m.DispoID == id);
            if (disposable == null)
            {
                return NotFound();
            }

            return View(disposable);
        }

        // POST: Chemicals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var disposable = await _context.Disposable.SingleOrDefaultAsync(m => m.DispoID == id);
            _context.Disposable.Remove(disposable);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return RedirectToAction("Delete", new { itemInUse = true });
            }
            sp_Logging("3-Remove", "Delete", "User deleted a disposable where ID= " + id.ToString(), "Success");
            return RedirectToAction("Index");
        }

        // Custom Logging Solution
        private void sp_Logging(string level, string logger, string message, string exception)
        {
            // Connection string from AppSettings.JSON
            string CS = configuration.GetConnectionString("DefaultConnection");
            // Using Identity middleware to get email adddress
            string user = User.Identity.Name;
            string app = "Carroll LMS";
            // Subtract 5 hours as the timestamp is in GMT
            DateTime logged = DateTime.Now.AddHours(-5);

            string site = "Disposable";
            string query = "insert into dbo.Log([User], [Application], [Logged], [Level], [Message], [Logger], [CallSite]," +
                "[Exception]) values(@User, @Application, @Logged, @Level, @Message,@Logger, @Callsite, @Exception)";
            using (SqlConnection con = new SqlConnection(CS))
            {
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@User", user);
                cmd.Parameters.AddWithValue("@Application", app);
                cmd.Parameters.AddWithValue("@Logged", logged);
                cmd.Parameters.AddWithValue("@Level", level);
                cmd.Parameters.AddWithValue("@Message", message);
                cmd.Parameters.AddWithValue("@Logger", logger);
                cmd.Parameters.AddWithValue("@Callsite", site);
                cmd.Parameters.AddWithValue("@Exception", exception);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
        }
    }
}