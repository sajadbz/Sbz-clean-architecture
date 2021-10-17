using System.ComponentModel.DataAnnotations;

namespace Sbz.Application.Statics
{
    public enum ActionMessageType
    {
        //[Display(Name = "ActionMessageTypeSuccess", ResourceType = typeof(Resources.Statics.StaticResource))]
        Success,
        //[Display(Name = "ActionMessageTypeError", ResourceType = typeof(Resources.Statics.StaticResource))]
        Error,
        //[Display(Name = "ActionMessageTypeInformation", ResourceType = typeof(Resources.Statics.StaticResource))]
        Information, 
        Null
    }
}
