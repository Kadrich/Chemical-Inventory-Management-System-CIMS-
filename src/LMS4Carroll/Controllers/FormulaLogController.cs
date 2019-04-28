using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LMS4Carroll.Models;
using LMS4Carroll.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace LMS4Carroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FormulaLogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private IConfiguration configuration;

        public FormulaLogController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            this.configuration = config;
        }
        // GET: FormulaLog
        public async Task<ActionResult> Index(string searchString, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            var formulaLogs = from m in _context.FormulaLog select m;

            //seach feature
            if (!String.IsNullOrEmpty(searchString))
            {
                formulaLogs = formulaLogs.Where(s => s.ChemicalList.Contains(searchString)
                                            || s.Name.Contains(searchString));
            }

            ViewData["DateSort"] = String.IsNullOrEmpty(sortOrder) ? "DateSort" : "";
            ViewData["StudentSort"] = sortOrder == "StudentSort" ? "StudentSort_desc" : "StudentSort";
            ViewData["FormulaSort"] = sortOrder == "FormulaSort" ? "FormulaSort_desc" : "FormulaSort";
            
            //sorting
            switch (sortOrder)
            {
                //Ascending
                case "FormulaSort":
                    formulaLogs = formulaLogs.OrderBy(x => x.FormulaID);
                    break;
                case "StudentSort":
                    formulaLogs = formulaLogs.OrderBy(x => x.Name);
                    break;
                case "DateSort":
                    formulaLogs = formulaLogs.OrderBy(x => x.Date);
                    break;

                //Descending
                case "FormulaSort_desc":
                    formulaLogs = formulaLogs.OrderByDescending(x => x.FormulaID);
                    break;
                case "StudentSort_desc":
                    formulaLogs = formulaLogs.OrderByDescending(x => x.Name);
                    break;
                default:
                    formulaLogs = formulaLogs.OrderByDescending(x => x.Date);
                    break;
            }
            return View(await formulaLogs.ToListAsync());
        }

        // GET: FormulaLog/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formulaLog = await _context.FormulaLog.SingleOrDefaultAsync(f => f.LogID == id);
            var formula = await _context.Formula.SingleOrDefaultAsync(f => f.FormulaID == formulaLog.FormulaID);
            if (formulaLog == null || formula == null)
            {
                return NotFound();
            }
            ViewData["FormulaName"] = formula.FormulaName;
            ViewData["FormulaDescription"] = formula.Description;
            ViewData["FormulaID"] = formula.FormulaID;
            ViewData["Date"] = formulaLog.Date;
            ViewData["LogID"] = formulaLog.LogID;

            return View(formulaLog);
        }

        // POST: FormulaLog/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formulaLog = await _context.FormulaLog.SingleOrDefaultAsync(m => m.LogID == id);
            var formula = await _context.Formula.SingleOrDefaultAsync(f => f.FormulaID == formulaLog.FormulaID);
            if (formula == null || formulaLog == null)
            {
                return NotFound();
            }
            ViewData["FormulaName"] = formula.FormulaName;
            ViewData["FormulaDescription"] = formula.Description;

            return View(formulaLog);
        }

        // POST: FormulaLog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind("LogID,FormulaID,Name,Date,ChemicalList")]FormulaLog log)
        {
            var formula = await _context.Formula.SingleOrDefaultAsync(f => f.FormulaID == log.FormulaID);
            if (formula == null)
            {
                return NotFound();
            }
            ViewData["FormulaName"] = formula.FormulaName;
            ViewData["FormulaDescription"] = formula.Description;
            ViewData["FormulaID"] = formula.FormulaID;
            ViewData["Date"] = log.Date;
            ViewData["LogID"] = log.LogID;
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

                try
                {
                    _context.Update(log);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction("Index");
            }

            return View(log);
        }

        // GET: FormulaLog/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formulalog = await _context.FormulaLog.SingleOrDefaultAsync(x => x.LogID == id);
            var formula = await _context.Formula.SingleOrDefaultAsync(f => f.FormulaID == formulalog.FormulaID);
            if (formula == null || formula == null)
            {
                return NotFound();
            }
            ViewData["FormulaName"] = formula.FormulaName;
            ViewData["FormulaDescription"] = formula.Description;
            ViewData["FormulaID"] = formula.FormulaID;
            ViewData["Date"] = formulalog.Date;
            ViewData["LogID"] = formulalog.LogID;
            if (formulalog == null)
            {
                return NotFound();
            }
            return View(formulalog);
        }

        // POST: FormulaLog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var formulalog = await _context.FormulaLog.SingleOrDefaultAsync(x => x.LogID == id);
            _context.FormulaLog.Remove(formulalog);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}