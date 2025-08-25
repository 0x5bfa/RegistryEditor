using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using RegistryValley.App.Models;

namespace RegistryValley.App.Converters
{
    public class ValueTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = false;

            if (parameter is string strParam)
            {
                invert = strParam.ToLower() == "invert" ? true : false;
            }

            if (value is REG_VALUE_TYPE type)
            {
                switch (type)
                {
                    case REG_VALUE_TYPE.REG_SZ:
                    case REG_VALUE_TYPE.REG_EXPAND_SZ:
                    case REG_VALUE_TYPE.REG_MULTI_SZ:
                        {
                            return !invert ? true : false;
                        }
                    default:
                    case REG_VALUE_TYPE.REG_BINARY:
                    case REG_VALUE_TYPE.REG_DWORD:
                    case REG_VALUE_TYPE.REG_QWORD:
                        {
                            return !invert ? false : true;
                        }
                }
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
