using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LMS4Carroll.Data;
using LMS4Carroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace LMS4Carroll.Controllers
{
    [Authorize(Roles = "Admin,PhysicsUser")]
    public class PhyDisposablesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IConfiguration configuration;

        public PhyDisposablesController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            this.configuration = config;
        }

        // GET: PhyDisposables
        public async Task<IActionResult> Index(string phydisposables2String, string sortOrder, bool barcodeFlag)
        {
            ViewData["Search"] = phydisposables2String;
            Sp_Logging("1-Info", "View", "Successfuly viewed Disposables Inventory list", "Success");
            var inventory = from m in _context.PhyDisposables.Include(c => c.Disposable).Include(c => c.Location)
                            select m;

            // display new barcode when new inventory is entered
            if (barcodeFlag)
            {
                ViewData["NewBarocde"] = inventory.Max(s => s.PhyDisposablesID);
            }

            // Search Feature
            if (!String.IsNullOrEmpty(phydisposables2String))
            {
                int outID;
                if (Int32.TryParse(phydisposables2String, out outID))
                {
                    inventory = inventory.Where(s => s.PhyDisposablesPK.Equals(outID)
                                            || s.Disposable.DispoID.Equals(outID)
                                            || s.LocationID.Equals(outID));
                }
                else
                {
                    inventory = inventory.Where(s => s.ItemName.Contains(phydisposables2String));

                    inventory = inventory.Where(s => s.CAT.Contains(phydisposables2String)
                                            || s.Comments.Contains(phydisposables2String)
                                            || s.Supplier.Contains(phydisposables2String));
                }
            }

            // Sort Feature
            ViewData["BarcodeSort"] = String.IsNullOrEmpty(sortOrder) ? "BarcodeSort_desc" : "";
            ViewData["NameSort"] = sortOrder == "NameSort" ? "NameSort_desc" : "NameSort";
            ViewData["LocationSort"] = sortOrder == "LocationSort" ? "LocationSort_desc" : "LocationSort";
            ViewData["AmtSort"] = sortOrder == "AmtSort" ? "AmtSort_desc" : "AmtSort";
            ViewData["CATSort"] = sortOrder == "CATSort" ? "CATSort_desc" : "CATSort";
            ViewData["CostSort"] = sortOrder == "CostSort" ? "CostSort_desc" : "CostSort";
            ViewData["OrderDateSort"] = sortOrder == "OrderDateSort" ? "OrderDateSort_desc" : "OrderDateSort";
            ViewData["SupplierSort"] = sortOrder == "SupplierSort" ? "SupplierSort_desc" : "SupplierSort";
            ViewData["CommentsSort"] = sortOrder == "CommentsSort" ? "CommentsSort_desc" : "CommentsSort";

            switch (sortOrder)
            {
                // Ascending
                case "BarcodeSort":
                    inventory = inventory.OrderBy(x => x.PhyDisposablesID);
                    break;
                case "NameSort":
                    inventory = inventory.OrderBy(x => x.ItemName);
                    break;
                case "LocationSort":
                    inventory = inventory.OrderBy(x => x.Location.StorageCode);
                    break;
                case "AmtSort":
                    inventory = inventory.OrderBy(x => x.AmtOrdered);
                    break;
                case "CATSort":
                    inventory = inventory.OrderBy(x => x.CAT);
                    break;
                case "CostSort":
                    inventory = inventory.OrderBy(x => x.Cost);
                    break;
                case "OrderDateSort":
                    inventory = inventory.OrderBy(x => x.OrderDate);
                    break;
                case "SupplierSort":
                    inventory = inventory.OrderBy(x => x.Supplier);
                    break;
                case "CommentsSort":
                    inventory = inventory.OrderBy(x => x.Comments);
                    break;

                // Descending
                case "NameSort_desc":
                    inventory = inventory.OrderByDescending(x => x.ItemName);
                    break;
                case "LocationSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Location.StorageCode);
                    break;
                case "AmtSort_desc":
                    inventory = inventory.OrderByDescending(x => x.AmtOrdered);
                    break;
                case "CATSort_desc":
                    inventory = inventory.OrderByDescending(x => x.CAT);
                    break;
                case "CostSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Cost);
                    break;
                case "OrderDateSort_desc":
                    inventory = inventory.OrderByDescending(x => x.OrderDate);
                    break;
                case "SupplierSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Supplier);
                    break;
                case "CommentsSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Comments);
                    break;

                default:
                    inventory = inventory.OrderByDescending(x => x.PhyDisposablesID);
                    break;
            }
            return View(await inventory.ToListAsync());
        }

        // GET: PhyDisposables/Details/5
        [Authorize(Roles = "Admin,PhysicsUser")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phyDisposables = await _context.PhyDisposables.SingleOrDefaultAsync(m => m.PhyDisposablesPK == id);
            if (phyDisposables == null)
            {
                return NotFound();
            }

            return View(phyDisposables);
        }

        // GET: PhyDisposables/Create
        [Authorize(Roles = "Admin,PhysicsUser")]
        public IActionResult Create()
        {
            ViewData["DispoID"] = new SelectList(_context.Disposable.OrderBy(x => x.DispoID), "DispoID", "DispoName");
            ViewData["LocationName"] = new SelectList(_context.Locations.OrderBy(x => x.StorageCode), "LocationID", "StorageCode");
            return View();
        }

        // POST: PhyDisposables/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,PhysicsUser")]
        public async Task<IActionResult> Create(int? nameinput, DateTime dateinput, int? storageinput, string cat, int? costinput, int? amtinput, string supplierinput, string commentsinput)
        {
            ViewData["ItemName"] = nameinput;
            ViewData["OrderDate"] = dateinput;
            ViewData["StorageCode"] = storageinput;
            ViewData["CAT"] = cat;
            ViewData["Cost"] = costinput;
            ViewData["AmtOrdered"] = amtinput;
            ViewData["Supplier"] = supplierinput;
            ViewData["Comments"] = commentsinput;

            PhyDisposables phyDisposables = null;

            if (ModelState.IsValid)
            {
                phyDisposables = new PhyDisposables();
                phyDisposables.DispoID = nameinput;
                phyDisposables.OrderDate = dateinput;
                phyDisposables.LocationID = storageinput;
                phyDisposables.CAT = cat;
                phyDisposables.Cost = costinput;
                phyDisposables.AmtOrdered = amtinput;
                phyDisposables.Supplier = supplierinput;
                phyDisposables.Comments = commentsinput;
                var temp = _context.Locations.First(m => m.LocationID == storageinput);
                phyDisposables.NormalizedLocation = temp.StorageCode;
                var temp2 = _context.Disposable.First(n => n.DispoID == nameinput);
                phyDisposables.ItemName = temp2.DispoName;

                _context.Add(phyDisposables);
                await _context.SaveChangesAsync();

                phyDisposables.PhyDisposablesID = phyDisposables.PhyDisposablesPK;
                _context.Update(phyDisposables);
                await _context.SaveChangesAsync();

                Sp_Logging("2-Change", "Create", "User created a disposable inventory item where ItemName=" + phyDisposables.ItemName, "Success");
                return RedirectToAction("Index", new { barcodeFlag = true });
            }
            ViewData["DispoID"] = new SelectList(_context.Disposable, "DispoID", "DispoName", phyDisposables.DispoID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", phyDisposables.LocationID);
            return View(phyDisposables);
        }

        // GET: PhyDisposables/Edit/5
        [Authorize(Roles = "Admin,PhysicsUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phyDisposables = await _context.PhyDisposables.SingleOrDefaultAsync(m => m.PhyDisposablesPK == id);
            if (phyDisposables == null)
            {
                return NotFound();
            }
            ViewData["DispoID"] = new SelectList(_context.Disposable, "DispoID", "DispoName", phyDisposables.DispoID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", phyDisposables.LocationID);
            return View(phyDisposables);
        }

        // GET: PhyDisposables/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,PhysicsUser")]
        public async Task<IActionResult> Edit(int id, int? nameinput, DateTime dateinput, int? storageinput, string catinput, int? costinput, int? amtinput, string supplierinput, string commentinput)
        {
            PhyDisposables phyDisposables = await _context.PhyDisposables.SingleOrDefaultAsync(p => p.PhyDisposablesPK == id);
            if (id != phyDisposables.PhyDisposablesPK)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    phyDisposables.DispoID = nameinput;
                    phyDisposables.OrderDate = dateinput;
                    phyDisposables.LocationID = storageinput;
                    phyDisposables.CAT = catinput;
                    phyDisposables.Cost = costinput;
                    phyDisposables.AmtOrdered = amtinput;
                    phyDisposables.Supplier = supplierinput;
                    phyDisposables.Comments = commentinput;
                    var temp = _context.Locations.First(m => m.LocationID == storageinput);
                    phyDisposables.NormalizedLocation = temp.StorageCode;
                    var temp2 = _context.Disposable.First(n => n.DispoID == nameinput);
                    phyDisposables.ItemName = temp2.DispoName;

                    _context.Update(phyDisposables);
                    await _context.SaveChangesAsync();
                    Sp_Logging("2-Change", "Edit", "User edited a Disposable inventory item where ID= " + id.ToString(), "Success");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhyDisposablesExists(phyDisposables.PhyDisposablesPK))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            ViewData["DispoID"] = new SelectList(_context.Disposable, "DispoID", "DispoName", phyDisposables.DispoID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", phyDisposables.LocationID);
            return View(phyDisposables);
        }

        // GET: PhyDisposables/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phyDisposables = await _context.PhyDisposables.SingleOrDefaultAsync(m => m.PhyDisposablesPK == id);
            if (phyDisposables == null)
            {
                return NotFound();
            }

            return View(phyDisposables);
        }

        // POST: PhyDisposables/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phyDisposables = await _context.PhyDisposables.SingleOrDefaultAsync(m => m.PhyDisposablesPK == id);
            _context.PhyDisposables.Remove(phyDisposables);
            await _context.SaveChangesAsync();
            Sp_Logging("3-Remove", "Delete", "User deleted a Disposable inventory item where ID=" + id.ToString(), "Success");
            return RedirectToAction("Index");
        }

        private bool PhyDisposablesExists(int? id)
        {
            return _context.PhyDisposables.Any(e => e.PhyDisposablesPK == id);
        }

        //Custom Loggin Solution
        private void Sp_Logging(string level, string logger, string message, string exception)
        {
            //Connection string from AppSettings.JSON
            string CS = configuration.GetConnectionString("DefaultConnection");
            //Using Identity middleware to get email address
            string user = User.Identity.Name;
            string app = "Carroll LMS";
            //Subtract 5 hours as the timestamp is in GMT timezone
            DateTime logged = DateTime.Now.AddHours(-5);
            //logged.AddHours(-5);
            string site = "PhyDisposables";
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