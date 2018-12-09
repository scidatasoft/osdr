using Newtonsoft.Json;
using Sds.FileParser;
using System;
using System.Text.RegularExpressions;


namespace Sds.WebImporter.ChemicalProcessing.CommandHandlers
{
    public static class PropertyExtensions
    {
        public static PropertyType ToPropertyType(this string property)
        {
            // parse JSON
            try
            {
                dynamic obj = JsonConvert.DeserializeObject(property);

                if (obj != null)
                {
                    Type t = obj.GetType();
                    if (t.Name == "JObject" || t.Name == "JArray")
                    {
                        return PropertyType.Json;
                    }
                }
            }
            catch
            {
            }

            // parse int
            int i;
            if (int.TryParse(property, out i))
            {
                return PropertyType.Int;
            }

            // parse double
            double d;
            if (double.TryParse(property, out d))
            {
                return PropertyType.Double;
            }

            // parse boolean
            bool b;
            if (bool.TryParse(property, out b))
            {
                return PropertyType.Bool;
            }

            // parse email
            System.ComponentModel.DataAnnotations.EmailAddressAttribute emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (emailAttr.IsValid(property))
                return PropertyType.Email;

            // parse uri
            try
            {
                Uri uri;

                if (Uri.TryCreate(property, UriKind.Absolute, out uri))
                {
                    return PropertyType.Url;
                }
            }
            catch
            {
            }

            Regex rgxDateTime = new Regex(@"^(0?[1-9]|1[012])(:[0-5]\d) [APap][mM]$", RegexOptions.IgnoreCase);
            DateTime dt;
            if (rgxDateTime.IsMatch(property))
            {
                if (DateTime.TryParse(property, out dt))
                    return PropertyType.Time;
            }

            TimeSpan ts;
            if (TimeSpan.TryParse(property, out ts))
            {
                return PropertyType.Time;
            }

            rgxDateTime = new Regex(@"^(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})$|^(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))\4(?:(?:1[6-9]|[2-9]\d)?\d{2})$");
            rgxDateTime.IsMatch(property);
            if (property.Length >= 8 && DateTime.TryParse(property, out dt))
            {
                if (dt.TimeOfDay != TimeSpan.Zero)
                {
                    return PropertyType.Datetime;
                }
                return PropertyType.Date;
            }

            // default - string type
            return property.Contains(Environment.NewLine) ? PropertyType.Textarea : PropertyType.String;
        }
    }
}
