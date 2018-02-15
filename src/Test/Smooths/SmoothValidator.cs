using ServiceStack.FluentValidation;
using ServiceStack.Smoothie.Test.Interfaces;

namespace ServiceStack.Smoothie.Test
{
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