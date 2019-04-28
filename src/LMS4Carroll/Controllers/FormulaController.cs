using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS4Carroll.Data;
using LMS4Carroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LMS4Carroll.Controllers
{
    [Authorize(Roles = "Admin,ChemUser,BiologyUser,Student")]
    public class FormulaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IConfiguration configuration;

        public FormulaController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            this.configuration = config;
        }

        // GET: Formula
        public async Task<ActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            var formulas = from m in _context.Formula select m;

            // Search Feature
            if (!String.IsNullOrEmpty(searchString))
            {
                int forID;
                if (Int32.TryParse(searchString, out forID)){
                    formulas = formulas.Where(s => s.FormulaID.Equals(forID));
                }
                else
                {
                    formulas = formulas.Where(s => s.FormulaName.Contains(searchString));
                }
            }

            // Sort Feature
            ViewData["NameSort"] = String.IsNullOrEmpty(sortOrder) ? "Name_desc" : "";

            switch (sortOrder)
            {
                case "Name_desc":
                    formulas = formulas.OrderByDescending(x => x.FormulaName);
                    break;

                default:
                    formulas = formulas.OrderBy(x => x.FormulaName);
                    break;
            }

            return View(await formulas.ToListAsync());
        }

        // GET: Formula/Details/5
        [Authorize(Roles = "Admin,ChemUser,BiologyUser,Student")]
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formula = await _context.Formula.SingleOrDefaultAsync(x => x.FormulaID == id);
            if (formula == null)
            {
                return NotFound();
            }
            return View(formula);
        }

        // GET: Formula/Create
        [Authorize(Roles = "Admin,ChemUser,Biologyuser")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Formula/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<ActionResult> Create([Bind("FormulaID,Name,Description")]Formula formula)
        {
            if (ModelState.IsValid)
            {
                _context.Add(formula);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View();
        }

        // GET: Formula/Edit/5
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formula = await _context.Formula.SingleOrDefaultAsync(x => x.FormulaID == id);
            if (formula == null)
            {
                return NotFound();
            }
            return View(formula);
        }

        // POST: Formula/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<ActionResult> Edit(int id, [Bind("FormulaID,Name,Description")]Formula formula)
        {
            if (id != formula.FormulaID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(formula);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction("Index");
            }
            return View(formula);
        }

        // GET: Formula/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formula = await _context.Formula.SingleOrDefaultAsync(x => x.FormulaID == id);
            if (formula == null)
            {
                return NotFound();
            }
            return View(formula);
        }

        // POST: Formula/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var formula = await _context.Formula.SingleOrDefaultAsync(x => x.FormulaID == id);
            _context.Formula.Remove(formula);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "ChemUser,BiologyUser,Admin")]
        public async Task<ActionResult> MakeLog(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formula = await _context.Formula.SingleOrDefaultAsync(x => x.FormulaID == id);
            if (formula == null)
            {
                return NotFound();
            }
            ViewData["FormulaName"] = formula.FormulaName;
            ViewData["FormulaDescription"] = formula.Description;
            ViewData["FormulaID"] = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ChemUser,BiologyUser")]
        public async Task<ActionResult> MakeLog([Bind("LogID,FormulaID,Name,Date,ChemicalList")]FormulaLog log)
        {
            var formula = await _context.Formula.SingleOrDefaultAsync(x => x.FormulaID == log.FormulaID);
            ViewData["FormulaName"] = formula.FormulaName;
            ViewData["FormulaDescription"] = formula.Description;
            ViewData["FormulaID"] = formula.FormulaID;
            if (ModelState.IsValid)
            {
                // make sure chemicallist contains all valid barcodes
                if (log.ChemicalList != null)
                {
                    int[] inputChem = log.ChemicalList.Split(',').Select(x => int.Parse(x)).ToArray();
                    var chemicals = await _context.ChemInventory.Select(m => m.ChemInventoryId).ToArrayAsync();

                    foreach (int c in inputChem)
                    {
                        if (!chemicals.Contains(c))
                        {
                            ModelState.AddModelError("ChemicalList", "One or more chemicals not found");
                            return View(log);
                        }
                    }
                }

                _context.Add(log);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(log);
        }
    }
}