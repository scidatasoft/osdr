using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Sds.MetadataStorage.Processing
{
    public class TypeQualifier
    {
        public object MinValue => GetConvertedValue(_minValue);
        public object MaxValue => GetConvertedValue(_maxValue);
        public string DataType => _isNotEmpty ? ( _qualifiers.FirstOrDefault().Key ?? "string") : "string";

        static readonly string[] _booleans = new[] { "true", "false", "1", "0", "y", "n", "yes", "no" };
        IDictionary<string, Func<string, bool>> _qualifiers;

        object _minValue;
        object _maxValue;
        bool _isNotEmpty;

        public TypeQualifier()
        {
            _qualifiers = new Dictionary<string, Func<string, bool>>
            {
                 { "boolean", s => _booleans.Contains(s.ToLower()) },
                 { "integer", s =>
                    {
                        if (!int.TryParse(s, out int n))
                            return false;

                        if (_minValue is null) _minValue = n;
                        else
                            if (n < (int)_minValue) _minValue = n;

                        if (_maxValue is null) _maxValue = n;
                        else
                            if (n > (int)_maxValue) _maxValue = n;

                        return true;
                    }
                 },
                 { "decimal", s =>
                    {
                       if (!decimal.TryParse(s, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out decimal n))
                        {
                            _minValue = _maxValue = null;
                            return false;
                        }
                    
                        if (_minValue is null) _minValue = n;
                        else
                            if (n < Convert.ToDecimal(_minValue)) _minValue = n;

                        if (_maxValue is null) _maxValue = n;
                        else
                            if (n > Convert.ToDecimal(_maxValue)) _maxValue = n;

                        return true;
                    }
                 }
            };
        }

        public void Qualify(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            _isNotEmpty = true;
            foreach (var q in _qualifiers.ToList())
            {
                if (!q.Value(value))
                    _qualifiers.Remove(q.Key);
            }
        }

        private object GetConvertedValue(object value)
        {
            switch (_qualifiers.FirstOrDefault().Key)
            {
                case "decimal":
                    return Convert.ToDecimal(value);

                case "integer":
                    return Convert.ToInt32(value);

                default:
                    return null;
            }
        }
    }
}
