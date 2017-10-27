using ServiceStack.FluentValidation;

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