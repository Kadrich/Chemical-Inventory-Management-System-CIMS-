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
    public class ChemInventories2Controller : Controller
    {
        private readonly ApplicationDbContext _context;
        private IConfiguration configuration;

        public string exportSearch = "";

        public ChemInventories2Controller(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            this.configuration = config;
        }

        // GET: ChemInventories2
        public async Task<IActionResult> Index(string cheminventory2String, string sortOrder, bool barcodeFlag)
        {
            ViewData["Search"] = cheminventory2String;
            Sp_Logging("1-Info", "View", "Successfuly viewed Chemical Inventory list", "Success");
            var inventory = from m in _context.ChemInventory2.Include(c => c.Chemical).Include(c => c.Location).Include(c => c.Order)
                            select m;

            // display new barcode when new inventory is entered
            if (barcodeFlag)
            {
                ViewData["NewBarcode"] = inventory.Max(s => s.ChemInventoryMId);
            }

            // Search Feature
            if (!String.IsNullOrEmpty(cheminventory2String))
            {
                exportSearch = cheminventory2String;
                int outID;
                if (Int32.TryParse(cheminventory2String, out outID))
                {
                    inventory = inventory.Where(s => s.ChemInventoryMId.Equals(outID)
                                            || s.Chemical.ChemID.Equals(outID)
                                            || s.LocationID.Equals(outID));
                }
                else
                {
                    inventory = inventory.Where(s => s.Chemical.FormulaName.Contains(cheminventory2String));

                    inventory = inventory.Where(s => s.Department.Contains(cheminventory2String)
                                            || s.CAT.Contains(cheminventory2String)
                                            || s.LOT.Contains(cheminventory2String)
                                            || s.Units.Contains(cheminventory2String)
                                            || s.Chemical.FormulaName.Contains(cheminventory2String)
                                            || s.Chemical.CAS.Contains(cheminventory2String));
                }
            }

            // Sort Feature
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
            ViewData["ManuSort"] = sortOrder == "ManuSort" ? "ManySort_desc" : "ManuSort";

            switch (sortOrder)
            {
                //Ascending
                case "BarcodeSort":
                    inventory = inventory.OrderBy(x => x.ChemInventoryMId);
                    break;
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
                    inventory = inventory.OrderByDescending(x => x.ChemInventoryMId);
                    break;
            }
            return View(await inventory.ToListAsync());
        }

        // print out the chemInventory as a pdf
        public FileContentResult ExportCSV()
        {
            var dataTable = from m in _context.ChemInventory2.Include(c => c.Chemical).Include(c => c.Location).Include(c => c.Order)
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
            export["Manufacturer"] = "Manufacturer";

            foreach (var item in dataTable)
            {
                export.AddRow();
                export["Barcode"] = item.Barcode;
                export["CAS"] = item.Chemical.CAS;
                export["CAT"] = item.CAT;
                export["LOT"] = item.LOT;
                export["Chem"] = item.Chemical.FormulaName;
                export["Qty"] = item.QtyLeft;
                export["Units"] = item.Units;
                export["Department"] = item.Department;
                export["Location"] = item.NormalizedLocation;
                export["Manufacturer"] = item.Manufacturer;
            }
            return File(export.ExportToBytes(), "text/csv", "Chemical Inventory.csv");
        }

        // GET: ChemInventories2/Details/5
        [Authorize(Roles = "Admin,ChemUser,BiologyUser,Student")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemInventory2 = await _context.ChemInventory2.SingleOrDefaultAsync(m => m.ChemInventoryMId == id);
            if (chemInventory2 == null)
            {
                return NotFound();
            }

            return View(chemInventory2);
        }

        // GET: ChemInventories2/Create
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public IActionResult Create()
        {
            ViewData["ChemID"] = new SelectList(_context.Chemical.OrderBy(x => x.FormulaName), "ChemID", "FormulaName");
            ViewData["LocationName"] = new SelectList(_context.Locations.OrderBy(x => x.StorageCode), "LocationID", "StorageCode");
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID");
            return View();
        }

        // POST: ChemInventories2/Create
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

            ChemInventory2 chemInventory2 = null;

            if (ModelState.IsValid)
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
                var temp = _context.Locations.First(m => m.LocationID == storageinput);
                chemInventory2.NormalizedLocation = temp.StorageCode;

                _context.Add(chemInventory2);
                await _context.SaveChangesAsync();

                chemInventory2.Barcode = chemInventory2.ChemInventoryMId;
                _context.Update(chemInventory2);
                await _context.SaveChangesAsync();

                Sp_Logging("2-Change", "Create", "User created a chemical inventory item where ChemID=" + formulainput + ", OrderID=" + formulainput, "Success");

                return RedirectToAction("Index", new { barcodeFlag = true });
            }
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventory2.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventory2.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventory2.OrderID);
            return View(chemInventory2);
        }


        // GET: ChemInventories2/Edit/5
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var chemInventory2 = await _context.ChemInventory2.SingleOrDefaultAsync(m => m.ChemInventoryMId == id);
            if (chemInventory2 == null)
            {
                return NotFound();
            }
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventory2.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventory2.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventory2.OrderID);
            return View(chemInventory2);
        }

        // POST: ChemInventories2/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<IActionResult> Edit(int id, int? formulainput, DateTime dateinput, int? storageinput, int? orderinput, string cat, string lot, float qtyinput, string unitstring, string deptstring, string manustring)
        {
            ChemInventory2 chemInventory2 = await _context.ChemInventory2.SingleOrDefaultAsync(p => p.ChemInventoryMId == id);

            if (id != chemInventory2.ChemInventoryMId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if(qtyinput == 0)
                {
                    ChemInventoryArc2 chemInventoryArc2 = null;

                    try
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
                        chemInventoryArc2.Barcode = chemInventory2.Barcode;
                        var temp = _context.Locations.First(m => m.LocationID == storageinput);
                        chemInventoryArc2.NormalizedLocation = temp.StorageCode;
                        _context.Add(chemInventoryArc2);
                        await _context.SaveChangesAsync();

                        await DeleteConfirmed(id);
                        Sp_Logging("2-Archive", "Edit", "User archived a Chemical inventory item where ID= " + id.ToString(), "Success");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ChemInventory2Exists(chemInventoryArc2.ChemInventoryMIdArc))
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
                        var temp = _context.Locations.First(m => m.LocationID == storageinput);
                        chemInventory2.NormalizedLocation = temp.StorageCode;
                        _context.Update(chemInventory2);
                        await _context.SaveChangesAsync();
                        Sp_Logging("2-Change", "Edit", "User edited a Chemical inventory item where ID= " + id.ToString(), "Success");
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ChemInventory2Exists(chemInventory2.ChemInventoryMId))
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
            ViewData["ChemID"] = new SelectList(_context.Chemical, "ChemID", "FormulaName", chemInventory2.ChemID);
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "StorageCode", chemInventory2.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemInventory2.OrderID);
            return View(chemInventory2);
        }

        // GET: ChemInventories2/Archive/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemInventory2 = await _context.ChemInventory2.SingleOrDefaultAsync(m => m.ChemInventoryMId == id);
            if (chemInventory2 == null)
            {
                return NotFound();
            }

            return View(chemInventory2);
        }

        // POST: ChemInventories2/Archive/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            var chemInventory2 = await _context.ChemInventory2.SingleOrDefaultAsync(m => m.ChemInventoryMId == id);
            ChemInventoryArc2 chemInventoryArc2 = new ChemInventoryArc2();

            if (chemInventory2 != null)
            {
                chemInventoryArc2.Barcode = chemInventory2.ChemInventoryMId;
                chemInventoryArc2.ChemID = chemInventory2.ChemID;
                chemInventoryArc2.LocationID = chemInventory2.LocationID;
                chemInventoryArc2.ExpiryDate = chemInventory2.ExpiryDate;
                chemInventoryArc2.OrderID = chemInventory2.OrderID;
                chemInventoryArc2.QtyLeft = chemInventory2.QtyLeft;
                chemInventoryArc2.Units = chemInventory2.Units;
                chemInventoryArc2.Department = chemInventory2.Department;
                chemInventoryArc2.CAT = chemInventory2.CAT;
                chemInventoryArc2.LOT = chemInventory2.LOT;
                chemInventoryArc2.Manufacturer = chemInventory2.Manufacturer;
                _context.ChemInventoryArc2.Add(chemInventoryArc2);
                await _context.SaveChangesAsync();

            }
            _context.ChemInventory2.Remove(chemInventory2);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // GET: ChemInventories2/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemInventory2 = await _context.ChemInventory2.SingleOrDefaultAsync(m => m.ChemInventoryMId == id);
            if (chemInventory2 == null)
            {
                return NotFound();
            }

            return View(chemInventory2);
        }

        // POST: ChemInventories2/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chemInventory2 = await _context.ChemInventory2.SingleOrDefaultAsync(m => m.ChemInventoryMId == id);
            _context.ChemInventory2.Remove(chemInventory2);
            await _context.SaveChangesAsync();
            Sp_Logging("3-Remove", "Delete", "User deleted a Chemical inventory item where ID=" + id.ToString(), "Success");
            return RedirectToAction("Index");
        }

        private bool ChemInventory2Exists(int? id)
        {
            return _context.ChemInventory2.Any(e => e.ChemInventoryMId == id);
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