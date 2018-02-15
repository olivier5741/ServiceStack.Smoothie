using ServiceStack.FluentValidation;
using ServiceStack.Smoothie.Test.Interfaces;

namespace ServiceStack.Smoothie.Test.Alarms
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