using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LMS4Carroll.Models
{
    public class FormulaLog
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Log ID")]
        public int LogID { get; set; }

        [Required]
        [Display(Name = "Formula ID")]
        public int? FormulaID { get; set; }

        [Required]
        [Display(Name = "Student Name")]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DefaultValue("01/01/1900")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Date")]
        public DateTime Date { get; set; }

        [Display(Name = "Barcode List")]
        [RegularExpression(@"(\d+)(,\s*\d+)*", ErrorMessage = "Format with numbers seperated by commas i.e. \"1234,5689\"")]
        public string ChemicalList { get; set; }
    }
}
