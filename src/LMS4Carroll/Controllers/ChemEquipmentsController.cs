using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LMS4Carroll.Data;
using LMS4Carroll.Models;
using ZXing;
using Microsoft.AspNetCore.Authorization;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using LMS4Carroll.Services;

namespace LMS4Carroll.Controllers
{
    [Authorize(Roles = "Admin,Handler,Student,ChemUser")]
    public class ChemEquipmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IConfiguration configuration;

        public ChemEquipmentsController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            this.configuration = config;
        }

        // GET: ChemEquipments
        public async Task<IActionResult> Index(string equipmentString) { 
            ViewData["CurrentFilter"] = equipmentString;
            sp_Logging("1-Info", "View", "Successfuly viewed Chemical Equipment list", "Success");


            //Search Feature
            if (!String.IsNullOrEmpty(equipmentString))
            {
                var equipments = from m in _context.ChemicalEquipments.Include(c => c.Location).Include(c => c.Order)
                                 select m;
                int forID;
                if (Int32.TryParse(equipmentString, out forID))
                {
                    equipments = equipments.Where(s => s.ChemEquipmentID.Equals(forID)
                                            || s.LocationID.Equals(forID)
                                            || s.OrderID.Equals(forID));
                    return View(await equipments.OrderByDescending(s => s.ChemEquipmentID).ToListAsync());
                }

                equipments = equipments.Where(s => s.EquipmentName.Contains(equipmentString)
                                    || s.EquipmentModel.Contains(equipmentString)
                                    || s.SerialNumber.Contains(equipmentString)
                                 // || s.Location.NormalizedStr.Contains(equipmentString)
                                    || s.LOT.Contains(equipmentString)
                                    || s.CAT.Contains(equipmentString)
                                    || s.Type.Contains(equipmentString)
                                    || s.Comments.Contains(equipmentString));
                return View(await equipments.OrderByDescending(s => s.ChemEquipmentID).ToListAsync());
            }

            else
            {
                var equipments = from m in _context.ChemicalEquipments.Include(c => c.Location).Include(c => c.Order).Take(1000)
                                 select m;
                return View(await equipments.OrderByDescending(s => s.ChemEquipmentID).ToListAsync());
            }
        }



        // print out the chemInventory as a pdf
        public FileContentResult ExportCSV()
        {
            var dataTable = from m in _context.ChemicalEquipments.Include(c => c.Location).Include(c => c.Order)
                            select m;

            var export = new CsvExport();
            export.AddRow();
            export["ChemEqID"] = "Chemical Equipment ID";
            export["ManfctName"] = "Manufacturer Name";
            export["EquipModel"] = "Equipment Model";
            export["S/N"] = "Serial Number";
            export["Location"] = "Location";
            export["InstallDate"] = "Installed Date";
            export["InspectDate"] = "Inspection Date";
            export["OrderID"] = "Order ID";
            export["CAT"] = "CAT Number";
            export["LOT"] = "LOT Number";



            foreach (var item in dataTable)
            {
                export.AddRow();
                export["ChemEqID"] = item.ChemEquipmentID;
                export["ManfctName"] = item.EquipmentName;
                export["EquipModel"] = item.EquipmentModel;
                export["S/N"] = item.SerialNumber;
                export["Location"] = item.Location.NormalizedStr;
                export["InstallDate"] = item.InstalledDate;
                export["InspectDate"] = item.InspectionDate;
                export["OrderID"] = item.Order.OrderID;
                export["CAT"] = item.CAT;
                export["LOT"] = item.LOT;



            }

            return File(export.ExportToBytes(), "text/csv", "Chemical Equipment Inventory.csv");
        }


        // GET: ChemEquipments/Details/5
        [Authorize(Roles = "Admin,ChemUser")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemEquipment = await _context.ChemicalEquipments.SingleOrDefaultAsync(m => m.ChemEquipmentID == id);
            if (chemEquipment == null)
            {
                return NotFound();
            }

            return View(chemEquipment);
        }

        // GET: ChemEquipments/Create
        [Authorize(Roles = "Admin,ChemUser")]
        public IActionResult Create()
        {
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "NormalizedStr");
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID");
            return View();
        }

        // POST: ChemEquipments/Create
        // To protect from overposting attacks, enabled bind properties
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser")]
        public async Task<IActionResult> Create([Bind("ChemEquipmentID,SerialNumber,InstalledDate,InspectionDate,LOT,CAT,EquipmentModel,EquipmentName,LocationID,OrderID,Type,Comments")] ChemEquipment chemEquipment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(chemEquipment);
                await _context.SaveChangesAsync();
                sp_Logging("2-Change", "Create", "User created a chemical equipment", "Success");
                return RedirectToAction("Index");
            }
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "NormalizedStr", chemEquipment.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemEquipment.OrderID);
            return View(chemEquipment);
        }

        // GET: ChemEquipments/Edit/5
        [Authorize(Roles = "Admin,ChemUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemEquipment = await _context.ChemicalEquipments.SingleOrDefaultAsync(m => m.ChemEquipmentID == id);
            if (chemEquipment == null)
            {
                return NotFound();
            }
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "NormalizedStr", chemEquipment.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemEquipment.OrderID);
            return View(chemEquipment);
        }

        // POST: ChemEquipments/Edit/5
        // To protect from overposting attacks, enabled bind properties
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser")]
        public async Task<IActionResult> Edit(int id, [Bind("ChemEquipmentID,SerialNumber,InstalledDate,InspectionDate,CAT,LOT,EquipmentModel,EquipmentName,LocationID,OrderID,Type,Comments")] ChemEquipment chemEquipment)
        {
            if (id != chemEquipment.ChemEquipmentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chemEquipment);
                    await _context.SaveChangesAsync();
                    sp_Logging("2-Change", "Edit", "User edited a Chemical Equipment where ID= " + id.ToString(), "Success");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChemEquipmentExists(chemEquipment.ChemEquipmentID))
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
            ViewData["LocationName"] = new SelectList(_context.Locations, "LocationID", "NormalizedStr", chemEquipment.LocationID);
            ViewData["OrderID"] = new SelectList(_context.Orders, "OrderID", "OrderID", chemEquipment.OrderID);
            return View(chemEquipment);
        }

        // GET: ChemEquipments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemEquipment = await _context.ChemicalEquipments.SingleOrDefaultAsync(m => m.ChemEquipmentID == id);
            if (chemEquipment == null)
            {
                return NotFound();
            }

            return View(chemEquipment);
        }

        // POST: ChemEquipments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chemEquipment = await _context.ChemicalEquipments.SingleOrDefaultAsync(m => m.ChemEquipmentID == id);
            _context.ChemicalEquipments.Remove(chemEquipment);
            await _context.SaveChangesAsync();
            sp_Logging("3-Remove", "Delete", "User deleted a Chemical Equipment where ID=" + id.ToString(), "Success");
            return RedirectToAction("Index");
        }

        // GET: ChemEquipments/Archive/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Archive(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chemEquipment = await _context.ChemicalEquipments.SingleOrDefaultAsync(m => m.ChemEquipmentID == id);
            if (chemEquipment == null)
            {
                return NotFound();
            }

            return View(chemEquipment);
        }

        // POST: ChemEquipments/Archive/5
        [HttpPost, ActionName("Archive")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveConfirm(int id)
        {
            var chemEquipment = await _context.ChemicalEquipments.SingleOrDefaultAsync(m => m.ChemEquipmentID == id);
            ChemArchive chemArchive = new ChemArchive();

            if (chemArchive != null)
            {

                chemArchive.OrderID = chemEquipment.OrderID;
                chemArchive.Type = chemEquipment.Type;
                chemArchive.SerialNumber = chemEquipment.SerialNumber;
                chemArchive.InstalledDate = chemEquipment.InstalledDate;
                chemArchive.ArchiveDate = DateTime.Today;
                chemArchive.EquipmentName = chemEquipment.EquipmentName;
                chemArchive.EquipmentModel = chemEquipment.EquipmentModel;
                chemArchive.Comments = chemEquipment.Comments;
                _context.ChemArchives.Add(chemArchive);
                await _context.SaveChangesAsync();
            }
            _context.ChemicalEquipments.Remove(chemEquipment);
            sp_Logging("3-Remove", "Delete", "User deleted a Chemistry Equipment where ID=" + id.ToString(), "Success");
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool ChemEquipmentExists(int id)
        {
            return _context.ChemicalEquipments.Any(e => e.ChemEquipmentID == id);
        }

        //Custom Loggin Solution
        private void sp_Logging(string level, string logger, string message, string exception)
        {
            //Connection string from AppSettings.JSON
            string CS = configuration.GetConnectionString("DefaultConnection");
            //Using Identity middleware to get email address
            string user = User.Identity.Name;
            string app = "Carroll LMS";
            //Subtract 5 hours as the timestamp is in GMT timezone
            DateTime logged = DateTime.Now.AddHours(-5);
            //logged.AddHours(-5);
            string site = "ChemEquipments";
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
