using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMS4Carroll.Models
{
    public class Disposable
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Item ID")]
        public int DispoID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Item Name")]
        public string DispoName { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }

        public virtual ICollection<PhyDisposables> PhyDisposables { get; set; }
    }
}
