using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LMS4Carroll.Models
{
    public class Formula
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Formula ID")]
        public int FormulaID { get; set; }

        [Required]
        [Display(Name = "Formula Name")]
        public string FormulaName { get; set; }

        [Required]
        [Display(Name = "Formula Description")]
        public string Description { get; set; }
    }
}
