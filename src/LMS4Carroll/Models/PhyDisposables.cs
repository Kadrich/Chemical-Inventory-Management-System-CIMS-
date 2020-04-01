using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS4Carroll.Models
{
    public class PhyDisposables
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Primary Key")]
        public int? PhyDisposablesPK { get; set; }

        [Display(Name = "Entry ID")]
        public int? PhyDisposablesID { get; set; }

        [Required]
        [ForeignKey("Disposable")]
        public int? DispoID { get; set; }
        public virtual Disposable Disposable { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "ItemName")]
        public string ItemName { get; set; }

        [ForeignKey("Location")]
        public int? LocationID { get; set; }
        public virtual Location Location { get; set; }

        [StringLength(50)]
        [Display(Name = "Location")]
        public string NormalizedLocation { get; set; }

        [Display(Name = "Amount Ordered")]
        public int? AmtOrdered { get; set; }

        [StringLength(50)]
        [Display(Name = "CAT")]
        public string CAT { get; set; }

        [Display(Name = "Cost")]
        public int? Cost { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DefaultValue("01/01/1900")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Supplier")]
        public string Supplier { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }
    }
}