using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LMS4Carroll.Data;
using LMS4Carroll.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using LMS4Carroll.Services;

namespace LMS4Carroll.Controllers
{
    [Authorize(Roles = "Admin,ChemUser,BiologyUser,Student")]
    public class ChemInventoriesArc2Controller : Controller
    {
        private readonly ApplicationDbContext _context;
        private IConfiguration configuration;

        public ChemInventoriesArc2Controller(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            this.configuration = config;
        }

        // GET: ChemInventoriesArc2
        public async Task<IActionResult> Index(string cheminventoryarc2String, string sortOrder, bool barcodeFlag)
        {
            ViewData["Search"] = cheminventoryarc2String;
            Sp_Logging("1-Info", "View", "Successfully viewed Chemicals Archive list", "Success");
            var archive = from m in _context.ChemInventoryArc2.Include(c => c.Chemical).Include(c => c.Location).Include(c => c.Order)
                          select m;

            // Search Feature
            if (!String.IsNullOrEmpty(cheminventoryarc2String))
            {
                int outID;
                if (Int32.TryParse(cheminventoryarc2String, out outID))
                {
                    archive = archive.Where(s => s.ChemInventoryMIdArc.Equals(outID)
                                        || s.Chemical.ChemID.Equals(outID)
                                        || s.LocationID.Equals(outID));
                }
                else
                {
                    archive = archive.Where(s => s.Chemical.FormulaName.Contains(cheminventoryarc2String));

                    archive = archive.Where(s => s.Department.Contains(cheminventoryarc2String)
                                        || s.CAT.Contains(cheminventoryarc2String)
                                        || s.LOT.Contains(cheminventoryarc2String)
                                        || s.Units.Contains(cheminventoryarc2String)
                                        || s.Chemical.FormulaName.Contains(cheminventoryarc2String)
                                        || s.Chemical.CAS.Contains(cheminventoryarc2String));
                }
            }

            // Sort feature
            ViewData["BarcodeSort"] = String.IsNullOrEmpty(sortOrder) ? "BarcodeSort_desc" : "";
            ViewData["POSort"] = sortOrder == "POSort" ? "POSort_desc" : "POSort";
            ViewData["CASSord"] = sortOrder == "CASSort" ? "CASSort_desc" : "CASSort";
            ViewData["ChemSort"] = sortOrder == "ChemSort" ? "ChemSort_desc" : "ChemSort";
            ViewData["OrderSort"] = sortOrder == "OrderSort" ? "OrderSort_desc" : "OrderSort";
            ViewData["LocationSort"] = sortOrder == "LocationSort" ? "LocationSort_desc" : "LocationSort";
            ViewData["DateSort"] = sortOrder == "DateSort" ? "DateSort_desc" : "DateSort";
            ViewData["QtySort"] = sortOrder == "QtySort" ? "QtySort_desc" : "QtySort";
            ViewData["UnitSort"] = sortOrder == "UnitSort" ? "UnitSort_desc" : "UnitSort";
            ViewData["DeptSort"] = sortOrder == "DeptSort" ? "DeptSort_desc" : "DeptSort";
            ViewData["ManuSort"] = sortOrder == "ManuSort" ? "ManuSort_desc" : "ManuSort";

            switch (sortOrder)
            {
                //Ascending
                case "POSort":
                    archive = archive.OrderBy(x => x.Order.PO);
                    break;
                case "CASSort":
                    archive = archive.OrderBy(x => x.Chemical.CAS);
                    break;
                case "ChemSort":
                    archive = archive.OrderBy(x => x.Chemical.FormulaName);
                    break;
                case "OrderSort":
                    archive = archive.OrderBy(x => x.OrderID);
                    break;
                case "LocationSort":
                    archive = archive.OrderBy(x => x.Location.StorageCode);
                    break;
                case "DateSort":
                    archive = archive.OrderBy(x => x.ExpiryDate);
                    break;
                case "QtySort":
                    archive = archive.OrderBy(x => x.QtyLeft);
                    break;
                case "UnitSort":
                    archive = archive.OrderBy(x => x.Units);
                    break;
                case "DeptSort":
                    archive = archive.OrderBy(x => x.Department);
                    break;

                //Descending
                case "BarcodeSort_desc":
                    archive = archive.OrderByDescending(x => x.Barcode);
                    break;
                case "POSort_desc":
                    archive = archive.OrderByDescending(x => x.Order.PO);
                    break;
                case "CASSort_desc":
                    archive = archive.OrderByDescending(x => x.Chemical.CAS);
                    break;
                case "ChemSort_desc":
                    archive = archive.OrderByDescending(x => x.Chemical.FormulaName);
                    break;
                case "OrderSort_desc":
                    archive = archive.OrderByDescending(x => x.OrderID);
                    break;
                case "LocationSort_desc":
                    archive = archive.OrderByDescending(x => x.Location.StorageCode);
                    break;
                case "DateSort_desc":
                    archive = archive.OrderByDescending(x => x.ExpiryDate);
                    break;
                case "QtySort_desc":
                    archive = archive.OrderByDescending(x => x.QtyLeft);
                    break;
                case "UnitSort_desc":
                    archive = archive.OrderByDescending(x => x.Units);
                    break;
                case "DeptSort_desc":
                    archive = archive.OrderByDescending(x => x.Department);
                    break;

                default:
                    archive = archive.OrderBy(x => x.Barcode);
                    break;
            }

            return View(await archive.ToListAsync());
        }

        // GET: ChemInventories/Details/5
        // switched to GET: ChemArchives/Details/5
        // ok I don't know what the '5' means 
        // double check the url
        [Authorize(Roles = "Admin,ChemUser,BiologyUser,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemArchive = await _context.ChemInventoryArc2.SingleOrDefaultAsync(m => m.ChemInventoryMIdArc == id);
            if (chemArchive == null)
            {
                return NotFound(chemArchive);
            }

            return View();
        }

        // GET: ChemInventories/Create
        // changed to GET: ChemArchives/Create
        // double check the url
        [Authorize(Roles = "Admin,ChemUSer,BiologyUser")]
        public IActionResult Create()
        {
            ViewData["ChemID"] = new SelectList(_context.Chemical.OrderBy(x => x.FormulaName), "ChemID", "FormulaName");
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode");
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID");
            return View();
        }

        // POST: ChemInventories/Create
        // changed to POST: ChemArchives/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<IActionResult> Create(int? formulainput, DateTime dateinput, int? storageinput, int? orderinput, string cat, string lot, float qtyinput, string unitstring, string deptstring, string manustring)
        {
            ViewData["Formula"] = formulainput;
            ViewData["ExpiryDate"] = dateinput;
            ViewData["StorageCode"] = storageinput;
            ViewData["Order"] = orderinput;
            ViewData["Qty"] = qtyinput;
            ViewData["Unit"] = unitstring;
            ViewData["Department"] = deptstring;
            ViewData["CAT"] = cat;
            ViewData["LOT"] = lot;
            ViewData["Manufacturer"] = manustring;

            ChemInventoryArc2 chemInventoryArc2 = null;

            if (ModelState.IsValid)
            {
                chemInventoryArc2 = new ChemInventoryArc2();
                chemInventoryArc2.ChemID = formulainput;
                chemInventoryArc2.LocationID = storageinput;
                chemInventoryArc2.ExpiryDate = dateinput;
                chemInventoryArc2.OrderID = orderinput;
                chemInventoryArc2.QtyLeft = qtyinput;
                chemInventoryArc2.Units = unitstring;
                chemInventoryArc2.Department = deptstring;
                chemInventoryArc2.CAT = cat;
                chemInventoryArc2.LOT = lot;
                chemInventoryArc2.Manufacturer = manustring;
                var temp = _context.Locations.First(m => m.LocationID == storageinput);
                chemInventoryArc2.NormalizedLocation = temp.StorageCode;

                _context.Add(chemInventoryArc2);
                await _context.SaveChangesAsync();

                // the three lines below are so that the Barcode matches the Primary Key upon creation, which doesn't exist until the item has been added to the _context
                chemInventoryArc2.Barcode = chemInventoryArc2.ChemInventoryMIdArc;
                _context.Update(chemInventoryArc2);
                await _context.SaveChangesAsync();

                Sp_Logging("2-Change", "Create", "User created a chemical archive item where ChemID=" + formulainput + ", OrderID=" + formulainput, "Success");
                return RedirectToAction("Index", new { barcodeFlag = true });
            }
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventoryArc2.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventoryArc2.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventoryArc2.OrderID);
            return View(chemInventoryArc2);
        }

        // GET: ChemInventories/Edit/5
        // changed to GET:  ChemInventoriesArc/Edit/5
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemInventoryArc2 = await _context.ChemInventoryArc2.SingleOrDefaultAsync(m => m.ChemInventoryMIdArc == id);
            if (chemInventoryArc2 == null)
            {
                return NotFound();
            }
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventoryArc2.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventoryArc2.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventoryArc2.OrderID);
            return View(chemInventoryArc2);
        }

        // POST: ChemInventories/Edit/5
        // changed to POST: ChemInventoriesArc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<IActionResult> Edit(int id, int? formulainput, DateTime dateinput, int? storageinput, int? orderinput, string cat, string lot, float qtyinput, string unitstring, string deptstring, string manustring, int barcode)
        {
            ChemInventoryArc2 chemInventoryArc2 = await _context.ChemInventoryArc2.SingleOrDefaultAsync(p => p.ChemInventoryMIdArc == id);

            if (id != chemInventoryArc2.ChemInventoryMIdArc)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (qtyinput > 0)
                {
                    ChemInventory2 chemInventory2 = null;
                    try
                    {
                        chemInventory2 = new ChemInventory2();
                        chemInventory2.ChemID = formulainput;
                        chemInventory2.LocationID = storageinput;
                        chemInventory2.ExpiryDate = dateinput;
                        chemInventory2.OrderID = orderinput;
                        chemInventory2.QtyLeft = qtyinput;
                        chemInventory2.Units = unitstring;
                        chemInventory2.Department = deptstring;
                        chemInventory2.CAT = cat;
                        chemInventory2.LOT = lot;
                        chemInventory2.Manufacturer = manustring;
                        chemInventory2.Barcode = chemInventoryArc2.Barcode;
                        var temp = _context.Locations.First(m => m.LocationID == storageinput);
                        chemInventory2.NormalizedLocation = temp.StorageCode;
                        _context.Add(chemInventory2);
                        await _context.SaveChangesAsync();

                        await DeleteConfirmed(id);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ChemInventoryArc2Exists(chemInventory2.ChemInventoryMId))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    try
                    {
                        chemInventoryArc2.ChemID = formulainput;
                        chemInventoryArc2.LocationID = storageinput;
                        chemInventoryArc2.ExpiryDate = dateinput;
                        chemInventoryArc2.OrderID = orderinput;
                        chemInventoryArc2.QtyLeft = qtyinput;
                        chemInventoryArc2.Units = unitstring;
                        chemInventoryArc2.Department = deptstring;
                        chemInventoryArc2.CAT = cat;
                        chemInventoryArc2.LOT = lot;
                        chemInventoryArc2.Manufacturer = manustring;
                        chemInventoryArc2.Barcode = barcode;
                        var temp = _context.Locations.First(m => m.LocationID == storageinput);
                        chemInventoryArc2.NormalizedLocation = temp.StorageCode;
                        _context.Update(chemInventoryArc2);
                        await _context.SaveChangesAsync();
                        Sp_Logging("2-Change", "Edit", "User editted a Chemical archive item where ID=" + id.ToString(), "Success");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ChemInventoryArc2Exists(chemInventoryArc2.ChemInventoryMIdArc))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                return RedirectToAction("Index");
            }
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventoryArc2.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventoryArc2.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventoryArc2.OrderID);
            return View(chemInventoryArc2);
        }

        // GET: ChemInventories/Delete/5
        // changed to GET: ChemInventoriesArc/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemInventoryArc2 = await _context.ChemInventoryArc2.SingleOrDefaultAsync(m => m.ChemInventoryMIdArc == id);
            if (chemInventoryArc2 == null)
            {
                return NotFound();
            }

            return View(chemInventoryArc2);
        }

        // POST: ChemInventories/Delete/5
        // changed to POST: ChemInventoriesArc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chemInventoryArc2 = await _context.ChemInventoryArc2.SingleOrDefaultAsync(m => m.ChemInventoryMIdArc == id);
            _context.ChemInventoryArc2.Remove(chemInventoryArc2);
            await _context.SaveChangesAsync();
            Sp_Logging("3-Remove", "Delete", "User deleted a Chemical archive item where ID=" + id.ToString(), "Success");
            return RedirectToAction("Index");
        }

        private bool ChemInventoryArc2Exists(int? id)
        {
            return _context.ChemInventoryArc2.Any(e => e.ChemInventoryMIdArc == id);
        }

        // ----------------   What Does the Below Do? ---------------------

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
            string site = "ChemInventoriesArc2";
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