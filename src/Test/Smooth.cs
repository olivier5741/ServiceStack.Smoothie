using System;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;

namespace ServiceStack.Smoothie.Test
{
    [Route("/smooth", "POST")]
    public class Smooth
    {
        [IgnoreDataMember]
        [AutoIncrement]
        public int OrderId { get; set; } // FIFO, autoincrement

        public Guid Id { get; set; }
        
        public Guid TenantId { get; set; }
        public Guid AppId { get; set; }

        public DateTime? Smoothed { get; set; }
        
        public bool Published { get; set; }
        
        public bool Cancelled { get; set; }
    }

    public class SmoothValidator : AbstractValidator<Smooth>
    {
        public SmoothValidator()
        {
            RuleSet(ApplyTo.Post, () =>
            {
                RuleFor(s => s.AppId).NotEmpty();
            });
            
        }
    }
}