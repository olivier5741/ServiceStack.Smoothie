using ServiceStack.FluentValidation;

namespace ServiceStack.Smoothie.Test
{
    public class AlarmValidator : AbstractValidator<Alarm>
    {
        public AlarmValidator()
        {
            RuleSet(ApplyTo.Post, () =>
            {
                RuleFor(s => s.AppId).NotEmpty();
                RuleFor(s => s.Time).NotEmpty();
            });
            
        }
    }
}