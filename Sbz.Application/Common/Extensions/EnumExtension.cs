using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Sbz.Application.Common.Models;

namespace Sbz.Application.Common.Extensions
{
    public static class EnumExtension
    {
        /// <summary>
        /// Example: EnumExtension.ToList<MyEnum>();
        /// </summary>
        public static IList<EnumItem> ToList<T>() where T : struct
        {
            var result = new List<EnumItem>();

            if (!typeof(T).IsEnum) return result;

            var enumType = typeof(T);
            var values = Enum.GetValues(enumType);
            foreach (var value in values)
            {
                var memInfo = enumType.GetMember(enumType.GetEnumName(value));
                var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                string title;

                if (descriptionAttributes.Length > 0)
                    title = ((DescriptionAttribute)descriptionAttributes.First()).Description;
                else
                {
                    descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
                    title = descriptionAttributes.Length > 0
                        ? (((DisplayAttribute)descriptionAttributes.First()).GetName()
                           ?? ((DisplayAttribute)descriptionAttributes.First()).GetDescription())
                        : value.ToString();
                }

                result.Add(new EnumItem((int)value, value.ToString(), title));
            }

            return result;
        }

        public static string Title<T>(this T e) where T : Enum
        {
            if (!e.GetType().IsEnum) return "";

            var enumType = e.GetType();
            var values = (T[])Enum.GetValues(enumType);
            foreach (var value in values)
            {
                if (value.Equals(e))
                {
                    var memInfo = enumType.GetMember(enumType.GetEnumName(value));
                    var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                    string description;

                    if (descriptionAttributes.Length > 0)
                        description = ((DescriptionAttribute)descriptionAttributes.First()).Description;
                    else
                    {
                        descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
                        description = descriptionAttributes.Length > 0
                            ? (((DisplayAttribute)descriptionAttributes.First()).GetName()
                               ?? ((DisplayAttribute)descriptionAttributes.First()).GetDescription())
                            : value.ToString();
                    }

                    return description;
                }

            }

            return e.ToString();
        }

        


    }
}
