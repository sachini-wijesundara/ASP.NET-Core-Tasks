using System;
using System.ComponentModel.DataAnnotations;
using ASP.NET_Core_Tasks.Validation;

namespace ASP.NET_Core_Tasks.Models
{
    public class Task
    {
        [Key]
        public int Id { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required and cannot be empty.")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsCompleted { get; set; } = false;

        [Required(ErrorMessage = "DueDate is required.")]
        [FutureOrPresentDate(ErrorMessage = "DueDate must be today or in the future when created.")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Priority is required.")]
        [EnumDataType(typeof(Priority))]
        public Priority Priority { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}
