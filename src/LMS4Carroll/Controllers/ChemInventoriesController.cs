using System;
using System.Collections.Generic;
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
using ZXing;
using LMS4Carroll.Services;

namespace LMS4Carroll.Controllers
{
    [Authorize(Roles = "Admin,ChemUser,BiologyUser,Student")]
    public class ChemInventoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IConfiguration configuration;

        public ChemInventoriesController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            this.configuration = config;
        }

        // GET: ChemInventories
        public async Task<IActionResult> Index(string cheminventoryString, string sortOrder, bool barcodeFlag)
        {
            //var applicationDbContext = _context.ChemInventory.Include(c => c.Chemical).Include(c => c.Location).Include(c => c.Order);
            ViewData["Search"] = cheminventoryString;
            Sp_Logging("1-Info", "View", "Successfuly viewed Chemical Inventory list", "Success");
            var inventory = from m in _context.ChemInventory.Include(c => c.Chemical).Include(c => c.Location).Include(c => c.Order)
                            select m;

            // display new barcode when new inventory is entered
            if (barcodeFlag)
            {
                ViewData["NewBarcode"] = inventory.Max(s => s.ChemInventoryId);
            }
            
            //Search Feature
            if (!String.IsNullOrEmpty(cheminventoryString))
            {
                
                int outID;
                if (Int32.TryParse(cheminventoryString, out outID))
                {
                    inventory = inventory.Where(s => s.ChemInventoryId.Equals(outID)
                                            || s.Chemical.ChemID.Equals(outID)
                                            || s.LocationID.Equals(outID));
                }
                else
                {
                    inventory = inventory.Where(s => s.Chemical.FormulaName.Contains(cheminventoryString));

                    inventory = inventory.Where(s => s.Department.Contains(cheminventoryString)
                                        || s.CAT.Contains(cheminventoryString)  
                                        || s.LOT.Contains(cheminventoryString)    
                                        || s.Units.Contains(cheminventoryString)
                                        || s.Chemical.FormulaName.Contains(cheminventoryString)
                                        || s.Chemical.CAS.Contains(cheminventoryString));
                }
                
                
            }
            
            //Sort feature
            ViewData["BarcodeSort"] = String.IsNullOrEmpty(sortOrder) ? "BarcodeSort_desc" : "";
            ViewData["POSort"] = sortOrder == "POSort" ? "POSort_desc" : "POSort";
            ViewData["CASSort"] = sortOrder == "CASSort" ? "CASSort_desc" : "CASSort";
            ViewData["ChemSort"] = sortOrder == "ChemSort" ? "ChemSort_desc" : "ChemSort";
            ViewData["OrderSort"] = sortOrder == "OrderSort" ? "OrderSort_desc" : "OrderSort";
            ViewData["LocationSort"] = sortOrder == "LocationSort" ? "LocationSort_desc" : "LocationSort";
            ViewData["DateSort"] = sortOrder == "DateSort" ? "DateSort_desc" : "DateSort";
            ViewData["QtySort"] = sortOrder == "QtySort" ? "QtySort_desc" : "QtySort";
            ViewData["UnitSort"] = sortOrder == "UnitSort" ? "UnitSort_desc" : "UnitSort";
            ViewData["DeptSort"] = sortOrder == "DeptSort" ? "DeptSort_desc" : "DeptSort";

            
            switch (sortOrder)
            {
                //Ascending
                case "POSort":
                    inventory = inventory.OrderBy(x => x.Order.PO);
                    break;
                case "CASSort":
                    inventory = inventory.OrderBy(x => x.Chemical.CAS);
                    break;
                case "ChemSort":
                    inventory = inventory.OrderBy(x => x.Chemical.FormulaName);
                    break;
                case "OrderSort":
                    inventory = inventory.OrderBy(x => x.OrderID);
                    break;
                case "LocationSort":
                    inventory = inventory.OrderBy(x => x.Location.StorageCode);
                    break;
                case "DateSort":
                    inventory = inventory.OrderBy(x => x.ExpiryDate);
                    break;
                case "QtySort":
                    inventory = inventory.OrderBy(x => x.QtyLeft);
                    break;
                case "UnitSort":
                    inventory = inventory.OrderBy(x => x.Units);
                    break;
                case "DeptSort":
                    inventory = inventory.OrderBy(x => x.Department);
                    break;

                //Descending
                case "BarcodeSort_desc":
                    inventory = inventory.OrderByDescending(x => x.ChemInventoryId);
                    break;
                case "POSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Order.PO);
                    break;
                case "CASSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Chemical.CAS);
                    break;
                case "ChemSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Chemical.FormulaName);
                    break;
                case "OrderSort_desc":
                    inventory = inventory.OrderByDescending(x => x.OrderID);
                    break;
                case "LocationSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Location.StorageCode);
                    break;
                case "DateSort_desc":
                    inventory = inventory.OrderByDescending(x => x.ExpiryDate);
                    break;
                case "QtySort_desc":
                    inventory = inventory.OrderByDescending(x => x.QtyLeft);
                    break;
                case "UnitSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Units);
                    break;
                case "DeptSort_desc":
                    inventory = inventory.OrderByDescending(x => x.Department);
                    break;

                default:
                    inventory = inventory.OrderBy(x => x.ChemInventoryId);
                    break;
            }

            return View(await inventory.ToListAsync());
        }

        // print out the chemInventory as a pdf
        public FileContentResult ExportCSV()
        {
            var dataTable = from m in _context.ChemInventory.Include(c => c.Chemical).Include(c => c.Location).Include(c => c.Order)
                            select m;

            var export = new CsvExport();
            export.AddRow();
            export["Barcode"] = "Barcode";
            export["CAS"] = "CAS Number";
            export["CAT"] = "CAT Number";
            export["LOT"] = "LOT Number";
            export["Chem"] = "Chemical Name";
            export["Qty"] = "Quantity Left";
            export["Units"] = "Units";
            export["Department"] = "Department";
            export["Location"] = "Location";
            foreach (var item in dataTable)
            {
                export.AddRow();
                export["Barcode"] = item.ChemInventoryId;
                export["CAS"] = item.Chemical.CAS;
                export["CAT"] = item.CAT;
                export["LOT"] = item.LOT;
                export["Chem"] = item.Chemical.FormulaName;
                export["Qty"] = item.QtyLeft;
                export["Units"] = item.Units;
                export["Department"] = item.Department;
                export["Location"] = item.NormalizedLocation;
            }

            return File(export.ExportToBytes(), "text/csv", "Chemical Inventory.csv");
        }

        // GET: ChemInventories/Details/5
        [Authorize(Roles = "Admin,ChemUser,BiologyUser,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemInventory = await _context.ChemInventory.SingleOrDefaultAsync(m => m.ChemInventoryId == id);
            if (chemInventory == null)
            {
                return NotFound();
            }

            return View(chemInventory);
        }

        // GET: ChemInventories/Create
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public IActionResult Create()
        {
            ViewData["ChemID"] = new SelectList(_context.Chemical.OrderBy(x => x.FormulaName), "ChemID", "FormulaName");
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode");
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID");
            return View();
        }

        // POST: ChemInventories/Create
        // Overposting attack vulnerability [Next iteration need to bind]
        //[Bind("ChemInventoryId,OrderID,LocationID,ChemID,Units,QtyLeft,ExpiryDate")] ChemInventory chemInventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<IActionResult> Create(int? formulainput, DateTime dateinput, int? storageinput, int? orderinput, string cat, string lot, float qtyinput, string unitstring, string deptstring)
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
            
            ChemInventory chemInventory = null;

            if (ModelState.IsValid)
            {
                //var chemID = _context.Chemical.Where(p => p.Formula == FormulaString).Select(p => p.ChemID);
                //var Chem = _context.Chemical.Where(p => p.Formula == FormulaString);
                //chemInventory.ChemID = await chemID;
                chemInventory = new ChemInventory();
                chemInventory.ChemID = formulainput;
                chemInventory.LocationID = storageinput;
                chemInventory.ExpiryDate = dateinput;
                chemInventory.OrderID = orderinput;
                chemInventory.QtyLeft = qtyinput;
                chemInventory.Units = unitstring;
                chemInventory.Department = deptstring;
                chemInventory.CAT = cat;
                chemInventory.LOT = lot;
                var temp = _context.Locations.First(m => m.LocationID == storageinput);
                chemInventory.NormalizedLocation = temp.StorageCode;
                
                _context.Add(chemInventory);
                await _context.SaveChangesAsync();
                Sp_Logging("2-Change", "Create", "User created a chemical inventory item where ChemID=" + formulainput + ", OrderID=" + formulainput, "Success");
                return RedirectToAction("Index", new { barcodeFlag = true});
            }
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventory.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventory.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventory.OrderID);
            return View(chemInventory);
        }

        // GET: ChemInventories/Edit/5
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemInventory = await _context.ChemInventory.SingleOrDefaultAsync(m => m.ChemInventoryId == id);
            if (chemInventory == null)
            {
                return NotFound();
            }
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventory.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventory.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventory.OrderID);
            return View(chemInventory);
        }

        // POST: ChemInventories/Edit/5
        // Overposting attack vulnerability [Next iteration need to bind]
        //[Bind("ChemInventoryId,OrderID,LocationID,ChemID,Units,QtyLeft,ExpiryDate")] ChemInventory chemInventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<IActionResult> Edit(int id, int? formulainput, DateTime dateinput, int? storageinput, int? orderinput, string cat, string lot, float qtyinput, string unitstring, string deptstring)
        {
            ChemInventory chemInventory = await _context.ChemInventory.SingleOrDefaultAsync(p => p.ChemInventoryId == id);

            if (id != chemInventory.ChemInventoryId)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                if (qtyinput == 0)
                {
                    ChemInventoryArc chemInventoryArc = await _context.ChemInventoryArc.SingleOrDefaultAsync(p => p.ChemInventoryIdArc == id);
                    chemInventoryArc.ChemID = formulainput;
                    /// continue this
                    /// put it in a try catch like below
                    /// this would presumably add it to the archive page
                    
                    /// maybe call DeleteConfirmed(id) to delete the chemical from the inventory
                }
                try
                {
                    chemInventory.ChemID = formulainput;
                    chemInventory.LocationID = storageinput;
                    chemInventory.ExpiryDate = dateinput;
                    chemInventory.OrderID = orderinput;
                    chemInventory.QtyLeft = qtyinput;
                    chemInventory.Units = unitstring;
                    chemInventory.Department = deptstring;
                    chemInventory.CAT = cat;
                    chemInventory.LOT = lot;
                    var temp = _context.Locations.First(m => m.LocationID == storageinput);
                    chemInventory.NormalizedLocation = temp.StorageCode;
                    _context.Update(chemInventory);
                    await _context.SaveChangesAsync();
                    Sp_Logging("2-Change", "Edit", "User edited a Chemical inventory item where ID= " + id.ToString(), "Success");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChemInventoryExists(chemInventory.ChemInventoryId))
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
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventory.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventory.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventory.OrderID);
            return View(chemInventory);
        }

        // GET: ChemInventories/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemInventory = await _context.ChemInventory.SingleOrDefaultAsync(m => m.ChemInventoryId == id);
            if (chemInventory == null)
            {
                return NotFound();
            }

            return View(chemInventory);
        }

        // POST: ChemInventories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chemInventory = await _context.ChemInventory.SingleOrDefaultAsync(m => m.ChemInventoryId == id);
            _context.ChemInventory.Remove(chemInventory);
            await _context.SaveChangesAsync();
            Sp_Logging("3-Remove", "Delete", "User deleted a Chemical inventory item where ID=" + id.ToString(), "Success");
            return RedirectToAction("Index");
        }

        private bool ChemInventoryExists(int? id)
        {
            return _context.ChemInventory.Any(e => e.ChemInventoryId == id);
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
            string site = "ChemInventory";
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
